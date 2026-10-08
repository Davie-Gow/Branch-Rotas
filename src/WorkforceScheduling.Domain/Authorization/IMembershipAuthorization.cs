using WorkforceScheduling.Domain.Branches;
using WorkforceScheduling.Domain.Identifiers;

namespace WorkforceScheduling.Domain.Authorization;

/// <summary>Authorizes commands independently of Power Apps client visibility.</summary>
public interface IMembershipAuthorization
{
    /// <summary>Checks whether a principal may change ordinary membership in a branch group.</summary>
    /// <param name="principalId">The authenticated requester, not a client-supplied identity.</param>
    /// <param name="securityGroupIds">Validated Entra group object IDs from the requester's access token.</param>
    /// <param name="branch">Trusted branch configuration loaded by the server.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>True only when an explicit role assignment covers the active trial branch.</returns>
    Task<bool> CanManageMembershipAsync(
        PrincipalId principalId,
        IReadOnlyCollection<string> securityGroupIds,
        BranchConfiguration branch,
        CancellationToken cancellationToken);
}

/// <summary>Resolves approved, stable-ID role scopes for an authenticated principal.</summary>
public interface IRoleScopeResolver
{
    /// <summary>Resolves the requester's approved Entra-backed role and scope assignments.</summary>
    /// <param name="principalId">The authenticated Entra object identifier.</param>
    /// <param name="securityGroupIds">Validated Entra group object IDs from the requester's access token.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>Only explicitly assigned role scopes.</returns>
    Task<IReadOnlyCollection<RoleScopeAssignment>> GetAssignmentsAsync(
        PrincipalId principalId,
        IReadOnlyCollection<string> securityGroupIds,
        CancellationToken cancellationToken);
}
