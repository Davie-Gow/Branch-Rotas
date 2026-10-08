using System.Security.Cryptography;
using System.Text;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using Microsoft.Extensions.Options;
using WorkforceScheduling.Functions.Configuration;

namespace WorkforceScheduling.Functions.Locks;

/// <summary>Serializes branch writes with renewable Azure Blob leases.</summary>
public sealed class BlobBranchOperationLock : IBranchOperationLock
{
    private const string ContainerName = "branch-operation-locks";
    private static readonly TimeSpan LeaseDuration = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan RenewalInterval = TimeSpan.FromSeconds(20);
    private readonly BlobContainerClient _container;

    public BlobBranchOperationLock(
        Azure.Core.TokenCredential credential,
        IOptions<WorkforceOptions> options)
    {
        var service = new BlobServiceClient(new Uri(options.Value.LockStorageServiceUri), credential);
        _container = service.GetBlobContainerClient(ContainerName);
    }

    public async Task<T> ExecuteAsync<T>(
        string branchId,
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken)
    {
        await _container.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
        var blob = _container.GetBlobClient(GetBlobName(branchId));
        try
        {
            await blob.UploadAsync(
                BinaryData.FromString("branch-operation-lock"),
                overwrite: false,
                cancellationToken: cancellationToken);
        }
        catch (RequestFailedException exception) when (exception.Status == 409)
        {
            // Another caller may have created the permanent empty lock blob first.
        }

        var leaseClient = blob.GetBlobLeaseClient();
        try
        {
            await leaseClient.AcquireAsync(LeaseDuration, cancellationToken: cancellationToken);
        }
        catch (RequestFailedException exception) when (exception.Status == 409)
        {
            throw new BranchOperationConflictException();
        }

        using var operationCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var renewal = RenewUntilCancelledAsync(leaseClient, operationCancellation.Token);
        Task<T>? operation = null;
        try
        {
            operation = action(operationCancellation.Token);
            var completed = await Task.WhenAny(operation, renewal);
            if (completed == renewal)
            {
                await renewal;
                throw new InvalidOperationException("Branch lease renewal stopped before the operation completed.");
            }

            var result = await operation;
            operationCancellation.Cancel();
            await renewal;
            return result;
        }
        finally
        {
            operationCancellation.Cancel();
            try
            {
                if (operation is not null)
                {
                    try
                    {
                        await operation;
                    }
                    catch (OperationCanceledException) when (operationCancellation.IsCancellationRequested)
                    {
                    }
                }

                await renewal;
            }
            finally
            {
                await leaseClient.ReleaseAsync(cancellationToken: CancellationToken.None);
            }
        }
    }

    private static async Task RenewUntilCancelledAsync(
        BlobLeaseClient leaseClient,
        CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(RenewalInterval);
        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                await leaseClient.RenewAsync(cancellationToken: cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
    }

    private static string GetBlobName(string branchId)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(branchId));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
