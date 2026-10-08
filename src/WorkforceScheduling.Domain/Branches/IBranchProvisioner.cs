namespace WorkforceScheduling.Domain.Branches;

/// <summary>Creates or reconciles a Team and Shifts schedule for an existing branch group.</summary>
public interface IBranchProvisioner
{
    Task<BranchProvisioningResult> ProvisionAsync(
        BranchResourceMapping mapping,
        CancellationToken cancellationToken);
}

/// <summary>Result of reconciling the branch's collaboration resources.</summary>
public sealed record BranchProvisioningResult(string TeamId, bool ScheduleReady);
