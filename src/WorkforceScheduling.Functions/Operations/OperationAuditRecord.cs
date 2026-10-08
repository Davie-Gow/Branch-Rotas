namespace WorkforceScheduling.Functions.Operations;

/// <summary>Audit fields retained for one asynchronous control-plane command.</summary>
public sealed record OperationAuditRecord(
    string OperationId,
    string BranchId,
    string OperationType,
    string State,
    string CorrelationId,
    string RequestedBy,
    string? TargetUserObjectId,
    string? Action,
    string? ErrorCategory);
