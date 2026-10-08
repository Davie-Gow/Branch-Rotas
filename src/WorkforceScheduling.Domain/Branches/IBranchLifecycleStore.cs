using WorkforceScheduling.Domain.Identifiers;

namespace WorkforceScheduling.Domain.Branches;

/// <summary>Persists allowed lifecycle transitions and external resource mappings.</summary>
public interface IBranchLifecycleStore
{
    Task MarkProvisioningAsync(BranchId branchId, string operationId, CancellationToken cancellationToken);

    Task MarkActiveAsync(
        BranchId branchId,
        string teamId,
        string operationId,
        CancellationToken cancellationToken);

    Task MarkFailedAsync(
        BranchId branchId,
        string operationId,
        BranchLifecycleState previousState,
        string errorCategory,
        CancellationToken cancellationToken);
}
