using WorkforceScheduling.Domain.Branches;
using WorkforceScheduling.Domain.Identifiers;

namespace WorkforceScheduling.Domain.Authorization;

/// <summary>
/// Applies the server-side policy for ordinary branch-group membership changes.
/// The caller must supply branch configuration loaded from trusted storage.
/// </summary>
public sealed class MembershipAuthorization : IMembershipAuthorization
{
    private readonly IRoleScopeResolver _roleScopeResolver;

    /// <summary>The reserved scope key used for explicitly approved trial-wide support assignments.</summary>
    public const string TrialScopeId = "trial";

    /// <summary>Creates the policy with a resolver backed by approved Entra role assignments.</summary>
    /// <param name="roleScopeResolver">The trusted role/scope assignment resolver.</param>
    public MembershipAuthorization(IRoleScopeResolver roleScopeResolver)
    {
        ArgumentNullException.ThrowIfNull(roleScopeResolver);
        _roleScopeResolver = roleScopeResolver;
    }

    /// <inheritdoc />
    public async Task<bool> CanManageMembershipAsync(
        PrincipalId principalId,
        IReadOnlyCollection<string> securityGroupIds,
        BranchConfiguration branch,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(principalId);
        ArgumentNullException.ThrowIfNull(securityGroupIds);
        ArgumentNullException.ThrowIfNull(branch);

        // Membership changes are limited to explicitly enabled trial branches.
        if (!branch.IsInTrialScope || branch.LifecycleState != BranchLifecycleState.Active)
        {
            return false;
        }

        var assignments = await _roleScopeResolver.GetAssignmentsAsync(
            principalId, securityGroupIds, cancellationToken);

        // Trial-wide support scope is explicit; an unscoped role never grants access.
        return assignments.Any(assignment =>
            assignment.Role == OperationalRole.PlatformSupportAdmin
            && assignment.ScopeType == ScopeType.Trial
            && string.Equals(assignment.ScopeId, TrialScopeId, StringComparison.Ordinal))
            || assignments.Any(assignment =>
                assignment.Role == OperationalRole.BranchManager
                && assignment.ScopeType == ScopeType.Branch
                && string.Equals(assignment.ScopeId, branch.BranchId.Value, StringComparison.Ordinal))
            || assignments.Any(assignment =>
                assignment.Role == OperationalRole.FranchiseManager
                && assignment.ScopeType == ScopeType.Franchise
                && string.Equals(assignment.ScopeId, branch.FranchiseId.Value, StringComparison.Ordinal));
    }
}
