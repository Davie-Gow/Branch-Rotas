namespace WorkforceScheduling.Functions.Locks;

/// <summary>Runs one state-changing branch operation under a renewable distributed lease.</summary>
public interface IBranchOperationLock
{
    Task<T> ExecuteAsync<T>(
        string branchId,
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken);
}
