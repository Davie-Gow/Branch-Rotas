namespace WorkforceScheduling.Functions.Operations;

/// <summary>Serializable input for an idempotent durable membership command.</summary>
public sealed record MembershipOperationInput(
    string OperationId,
    string BranchId,
    string UserObjectId,
    string Action,
    string RequestedBy,
    string[] SecurityGroupIds);

/// <summary>Serializable input for an idempotent durable branch reconciliation.</summary>
public sealed record ProvisionOperationInput(
    string OperationId,
    string BranchId,
    string RequestedBy,
    string PreviousLifecycleState,
    int ScheduleProvisioningTimeoutMinutes,
    int SchedulePollingIntervalSeconds,
    string[] SecurityGroupIds);
