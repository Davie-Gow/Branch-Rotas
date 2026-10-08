using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Options;
using WorkforceScheduling.Domain.Authorization;
using WorkforceScheduling.Domain.Branches;
using WorkforceScheduling.Domain.Identifiers;
using WorkforceScheduling.Functions.Authentication;
using WorkforceScheduling.Functions.Dataverse;
using WorkforceScheduling.Functions.Graph;
using WorkforceScheduling.Functions.Operations;
using WorkforceScheduling.Functions.Configuration;

namespace WorkforceScheduling.Functions.Api;

/// <summary>Authenticated HTTP control plane; every command is authorized against trusted server-side scope.</summary>
public sealed class BranchCommandFunctions(
    IEntraTokenValidator tokenValidator,
    IBranchMembershipStore branchStore,
    IMembershipAuthorization membershipAuthorization,
    IBranchAdministrationAuthorization administrationAuthorization,
    IUserDirectory userDirectory,
    IOperationStore operationStore,
    IOptions<WorkforceOptions> options)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Function(nameof(SearchBranchUsers))]
    public async Task<HttpResponseData> SearchBranchUsers(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "branches/{branchId}/users")]
        HttpRequestData request,
        string branchId,
        FunctionContext context)
    {
        var requester = await tokenValidator.ValidateAsync(request, context.CancellationToken);
        if (requester is null)
        {
            return await WriteErrorAsync(request, HttpStatusCode.Unauthorized, "Unauthenticated");
        }

        if (!TryCreateBranchId(branchId, out var stableBranchId))
        {
            return await WriteErrorAsync(request, HttpStatusCode.BadRequest, "InvalidBranchId");
        }

        var branch = await branchStore.GetBranchAsync(stableBranchId, context.CancellationToken);
        if (branch is null
            || !await membershipAuthorization.CanManageMembershipAsync(
                requester.PrincipalId, requester.SecurityGroupIds, branch.Branch, context.CancellationToken))
        {
            return await WriteErrorAsync(request, HttpStatusCode.Forbidden, "NotAuthorized");
        }

        var query = GetQueryValue(request, "q");
        if (string.IsNullOrWhiteSpace(query) || query.Trim().Length is < 2 or > 80)
        {
            return await WriteErrorAsync(request, HttpStatusCode.BadRequest, "InvalidSearchQuery");
        }

        var users = await userDirectory.SearchEnabledUsersAsync(query, 20, context.CancellationToken);
        return await WriteJsonAsync(request, HttpStatusCode.OK, users);
    }

    [Function(nameof(RequestMembershipChange))]
    public async Task<HttpResponseData> RequestMembershipChange(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "branches/{branchId}/membership-operations")]
        HttpRequestData request,
        string branchId,
        [DurableClient] DurableTaskClient durableClient,
        FunctionContext context)
    {
        var requester = await tokenValidator.ValidateAsync(request, context.CancellationToken);
        if (requester is null)
        {
            return await WriteErrorAsync(request, HttpStatusCode.Unauthorized, "Unauthenticated");
        }

        if (!TryCreateBranchId(branchId, out var stableBranchId))
        {
            return await WriteErrorAsync(request, HttpStatusCode.BadRequest, "InvalidBranchId");
        }

        var body = await ReadBodyAsync<MembershipChangeBody>(request, context.CancellationToken);
        if (body is null
            || !Guid.TryParse(body.UserObjectId, out var targetObjectId)
            || !Enum.TryParse<MembershipChangeAction>(body.Action, ignoreCase: true, out var action)
            || !Enum.IsDefined(action)
            || !TryGetIdempotencyKey(request, out var operationId))
        {
            return await WriteErrorAsync(request, HttpStatusCode.BadRequest, "InvalidMembershipRequest");
        }

        var branch = await branchStore.GetBranchAsync(stableBranchId, context.CancellationToken);
        if (branch is null
            || !await membershipAuthorization.CanManageMembershipAsync(
                requester.PrincipalId, requester.SecurityGroupIds, branch.Branch, context.CancellationToken))
        {
            return await WriteErrorAsync(request, HttpStatusCode.Forbidden, "NotAuthorized");
        }

        if (action == MembershipChangeAction.Add
            && await userDirectory.GetEnabledInternalUserAsync(
                targetObjectId.ToString("D"), context.CancellationToken) is null)
        {
            return await WriteErrorAsync(request, HttpStatusCode.BadRequest, "TargetUserUnavailable");
        }

        var operation = new OperationAuditRecord(
            operationId,
            stableBranchId.Value,
            "Membership",
            "Accepted",
            context.InvocationId,
            requester.PrincipalId.Value,
            targetObjectId.ToString("D"),
            action.ToString(),
            null);
        var existing = await operationStore.GetAsync(operationId, context.CancellationToken);
        if (existing is not null)
        {
            if (!IsSameRequest(existing, operation))
            {
                return await WriteErrorAsync(request, HttpStatusCode.Conflict, "IdempotencyKeyConflict");
            }

            if (string.Equals(existing.State, "Failed", StringComparison.Ordinal))
            {
                return await WriteErrorAsync(request, HttpStatusCode.Conflict, "OperationPreviouslyFailed");
            }

            return await WriteAcceptedAsync(request, operationId);
        }

        await operationStore.CreateAsync(operation, context.CancellationToken);
        var input = new MembershipOperationInput(
            operationId,
            stableBranchId.Value,
            targetObjectId.ToString("D"),
            action.ToString(),
            requester.PrincipalId.Value,
            requester.SecurityGroupIds.ToArray());
        try
        {
            await durableClient.ScheduleNewOrchestrationInstanceAsync(
                nameof(MembershipOperationOrchestrator),
                input,
                new StartOrchestrationOptions { InstanceId = operationId });
        }
        catch (Exception)
        {
            await operationStore.UpdateStateAsync(
                operationId, "Failed", "Failed", "OrchestrationStartFailed", CancellationToken.None);
            throw;
        }

        return await WriteAcceptedAsync(request, operationId);
    }

    [Function(nameof(RequestBranchReconciliation))]
    public async Task<HttpResponseData> RequestBranchReconciliation(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "branches/{branchId}/provisioning-operations")]
        HttpRequestData request,
        string branchId,
        [DurableClient] DurableTaskClient durableClient,
        FunctionContext context)
    {
        var requester = await tokenValidator.ValidateAsync(request, context.CancellationToken);
        if (requester is null)
        {
            return await WriteErrorAsync(request, HttpStatusCode.Unauthorized, "Unauthenticated");
        }

        if (!TryCreateBranchId(branchId, out var stableBranchId)
            || !TryGetIdempotencyKey(request, out var operationId))
        {
            return await WriteErrorAsync(request, HttpStatusCode.BadRequest, "InvalidReconciliationRequest");
        }

        var branch = await branchStore.GetBranchAsync(stableBranchId, context.CancellationToken);
        if (branch is null
            || !await administrationAuthorization.CanAdministerBranchAsync(
                requester.PrincipalId, requester.SecurityGroupIds, branch.Branch, context.CancellationToken))
        {
            return await WriteErrorAsync(request, HttpStatusCode.Forbidden, "NotAuthorized");
        }

        var operation = new OperationAuditRecord(
            operationId,
            stableBranchId.Value,
            "BranchReconciliation",
            "Accepted",
            context.InvocationId,
            requester.PrincipalId.Value,
            null,
            null,
            null);
        var existing = await operationStore.GetAsync(operationId, context.CancellationToken);
        if (existing is not null)
        {
            if (!IsSameRequest(existing, operation))
            {
                return await WriteErrorAsync(request, HttpStatusCode.Conflict, "IdempotencyKeyConflict");
            }

            if (string.Equals(existing.State, "Failed", StringComparison.Ordinal))
            {
                return await WriteErrorAsync(request, HttpStatusCode.Conflict, "OperationPreviouslyFailed");
            }

            return await WriteAcceptedAsync(request, operationId);
        }

        await operationStore.CreateAsync(operation, context.CancellationToken);
        try
        {
            await durableClient.ScheduleNewOrchestrationInstanceAsync(
                nameof(ProvisionOperationOrchestrator),
                new ProvisionOperationInput(
                    operationId,
                    stableBranchId.Value,
                    requester.PrincipalId.Value,
                    branch.Branch.LifecycleState.ToString(),
                    options.Value.ScheduleProvisioningTimeoutMinutes,
                    options.Value.SchedulePollingIntervalSeconds,
                    requester.SecurityGroupIds.ToArray()),
                new StartOrchestrationOptions { InstanceId = operationId });
        }
        catch (Exception)
        {
            await operationStore.UpdateStateAsync(
                operationId, "Failed", "Failed", "OrchestrationStartFailed", CancellationToken.None);
            throw;
        }

        return await WriteAcceptedAsync(request, operationId);
    }

    [Function(nameof(GetOperationStatus))]
    public async Task<HttpResponseData> GetOperationStatus(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "operations/{operationId}")]
        HttpRequestData request,
        string operationId,
        FunctionContext context)
    {
        var requester = await tokenValidator.ValidateAsync(request, context.CancellationToken);
        if (requester is null)
        {
            return await WriteErrorAsync(request, HttpStatusCode.Unauthorized, "Unauthenticated");
        }

        if (!Guid.TryParse(operationId, out var parsedId))
        {
            return await WriteErrorAsync(request, HttpStatusCode.BadRequest, "InvalidOperationId");
        }

        var operation = await operationStore.GetAsync(parsedId.ToString("D"), context.CancellationToken);
        if (operation is null || !string.Equals(operation.RequestedBy, requester.PrincipalId.Value, StringComparison.OrdinalIgnoreCase))
        {
            return await WriteErrorAsync(request, HttpStatusCode.NotFound, "OperationNotFound");
        }

        return await WriteJsonAsync(request, HttpStatusCode.OK, new
        {
            operation.OperationId,
            operation.BranchId,
            operation.OperationType,
            operation.State,
            operation.CorrelationId,
            operation.ErrorCategory
        });
    }

    private static bool TryCreateBranchId(string value, out BranchId branchId)
    {
        try
        {
            branchId = BranchId.Create(value);
            return true;
        }
        catch (ArgumentException)
        {
            branchId = default!;
            return false;
        }
    }

    private static bool TryGetIdempotencyKey(HttpRequestData request, out string operationId)
    {
        operationId = string.Empty;
        if (!request.Headers.TryGetValues("Idempotency-Key", out var values)
            || !Guid.TryParse(values.FirstOrDefault(), out var id))
        {
            return false;
        }

        operationId = id.ToString("D");
        return true;
    }

    private static string? GetQueryValue(HttpRequestData request, string key)
    {
        var query = System.Web.HttpUtility.ParseQueryString(request.Url.Query);
        return query[key];
    }

    private static async Task<T?> ReadBodyAsync<T>(HttpRequestData request, CancellationToken cancellationToken)
    {
        if (!request.Headers.TryGetValues("Content-Type", out var contentTypes)
            || !string.Equals(contentTypes.FirstOrDefault()?.Split(';')[0].Trim(), "application/json", StringComparison.OrdinalIgnoreCase))
        {
            return default;
        }

        try
        {
            using var buffer = new MemoryStream();
            var chunk = new byte[1024];
            while (true)
            {
                var read = await request.Body.ReadAsync(chunk, cancellationToken);
                if (read == 0)
                {
                    break;
                }

                if (buffer.Length + read > 4096)
                {
                    return default;
                }

                await buffer.WriteAsync(chunk.AsMemory(0, read), cancellationToken);
            }

            return buffer.Length == 0
                ? default
                : JsonSerializer.Deserialize<T>(buffer.ToArray(), JsonOptions);
        }
        catch (JsonException)
        {
            return default;
        }
    }

    private static bool IsSameRequest(OperationAuditRecord existing, OperationAuditRecord requested) =>
        string.Equals(existing.BranchId, requested.BranchId, StringComparison.Ordinal)
        && string.Equals(existing.OperationType, requested.OperationType, StringComparison.Ordinal)
        && string.Equals(existing.RequestedBy, requested.RequestedBy, StringComparison.OrdinalIgnoreCase)
        && string.Equals(existing.TargetUserObjectId, requested.TargetUserObjectId, StringComparison.OrdinalIgnoreCase)
        && string.Equals(existing.Action, requested.Action, StringComparison.Ordinal);

    private static async Task<HttpResponseData> WriteAcceptedAsync(HttpRequestData request, string operationId)
    {
        var response = request.CreateResponse(HttpStatusCode.Accepted);
        response.Headers.Add("Location", $"/api/operations/{operationId}");
        await response.WriteAsJsonAsync(new { operationId, status = "Accepted" });
        return response;
    }

    private static async Task<HttpResponseData> WriteErrorAsync(
        HttpRequestData request,
        HttpStatusCode status,
        string code) =>
        await WriteJsonAsync(request, status, new { error = code });

    private static async Task<HttpResponseData> WriteJsonAsync(
        HttpRequestData request,
        HttpStatusCode status,
        object body)
    {
        var response = request.CreateResponse(status);
        await response.WriteAsJsonAsync(body);
        return response;
    }

    private sealed record MembershipChangeBody(string? UserObjectId, string? Action);
}
