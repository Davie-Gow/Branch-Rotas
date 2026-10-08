using WorkforceScheduling.Domain.Authorization;
using WorkforceScheduling.Domain.Branches;
using WorkforceScheduling.Domain.Identifiers;

namespace WorkforceScheduling.Functions.Authentication;

/// <summary>Allows branch managers and trial support to reconcile approved live branch resources.</summary>
public sealed class BranchAdministrationAuthorization(IRoleScopeResolver roleScopeResolver)
    : IBranchAdministrationAuthorization
{
    public async Task<bool> CanAdministerBranchAsync(
        PrincipalId principalId,
        IReadOnlyCollection<string> securityGroupIds,
        BranchConfiguration branch,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(principalId);
        ArgumentNullException.ThrowIfNull(securityGroupIds);
        ArgumentNullException.ThrowIfNull(branch);

        if (!branch.IsInTrialScope
            || branch.LifecycleState is BranchLifecycleState.Suspended
                or BranchLifecycleState.DecommissionPending
                or BranchLifecycleState.Archived)
        {
            return false;
        }

        var assignments = await roleScopeResolver.GetAssignmentsAsync(
            principalId, securityGroupIds, cancellationToken);
        return assignments.Any(assignment =>
            assignment.Role == OperationalRole.PlatformSupportAdmin
            && assignment.ScopeType == ScopeType.Trial
            && string.Equals(assignment.ScopeId, MembershipAuthorization.TrialScopeId, StringComparison.Ordinal))
            || assignments.Any(assignment =>
                assignment.Role == OperationalRole.BranchManager
                && assignment.ScopeType == ScopeType.Branch
                && string.Equals(assignment.ScopeId, branch.BranchId.Value, StringComparison.Ordinal));
    }
}
