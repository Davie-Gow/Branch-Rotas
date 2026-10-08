namespace WorkforceScheduling.Functions.Operations;

/// <summary>Persists operation metadata and audit references without storing raw errors.</summary>
public interface IOperationStore
{
    Task CreateAsync(OperationAuditRecord operation, CancellationToken cancellationToken);

    Task<OperationAuditRecord?> GetAsync(string operationId, CancellationToken cancellationToken);

    Task UpdateStateAsync(
        string operationId,
        string state,
        string step,
        string? errorCategory,
        CancellationToken cancellationToken);
}
