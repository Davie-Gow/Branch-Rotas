using WorkforceScheduling.Domain.Branches;
using WorkforceScheduling.Domain.Identifiers;

namespace WorkforceScheduling.Domain.Authorization;

/// <summary>Loads trusted branch mappings and server-side authorization scope assignments.</summary>
public interface IBranchMembershipStore
{
    Task<BranchResourceMapping?> GetBranchAsync(BranchId branchId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<RoleScopeAssignment>> GetRoleAssignmentsAsync(
        PrincipalId principalId,
        IReadOnlyCollection<string> securityGroupIds,
        CancellationToken cancellationToken);
}
