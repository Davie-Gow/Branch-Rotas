namespace WorkforceScheduling.Domain.Branches;

/// <summary>Approved logical lifecycle states for a trial branch.</summary>
public enum BranchLifecycleState
{
    /// <summary>The branch is not enabled for scheduling.</summary>
    NotEnabled,
    /// <summary>Provisioning is in progress.</summary>
    Provisioning,
    /// <summary>The branch is enabled for normal operation.</summary>
    Active,
    /// <summary>New operational changes are blocked while resources and history are retained.</summary>
    Suspended,
    /// <summary>An approved decommission workflow is pending.</summary>
    DecommissionPending,
    /// <summary>The branch has been archived according to the approved retention policy.</summary>
    Archived
}
