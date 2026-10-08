using WorkforceScheduling.Domain.Branches;
using WorkforceScheduling.Domain.Identifiers;

namespace WorkforceScheduling.Domain.Authorization;

/// <summary>Authorizes Team and Shifts reconciliation independently of the app client.</summary>
public interface IBranchAdministrationAuthorization
{
    /// <summary>
    /// Checks whether a caller can reconcile Teams/Shifts for a branch. FranchiseManager scope alone
    /// does not grant scheduling-resource management.
    /// </summary>
    Task<bool> CanAdministerBranchAsync(
        PrincipalId principalId,
        IReadOnlyCollection<string> securityGroupIds,
        BranchConfiguration branch,
        CancellationToken cancellationToken);
}
