using WorkforceScheduling.Domain.Identifiers;

namespace WorkforceScheduling.Domain.Branches;

/// <summary>Minimum branch data required for authorization and lifecycle policy.</summary>
/// <param name="BranchId">The stable branch integration key.</param>
/// <param name="FranchiseId">The stable franchise scope key.</param>
/// <param name="IsInTrialScope">Whether the branch is explicitly allow-listed for the trial.</param>
/// <param name="LifecycleState">The approved branch lifecycle state.</param>
public sealed record BranchConfiguration(
    BranchId BranchId,
    FranchiseId FranchiseId,
    bool IsInTrialScope,
    BranchLifecycleState LifecycleState);
