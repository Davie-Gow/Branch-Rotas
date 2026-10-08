using WorkforceScheduling.Domain.Authorization;
using WorkforceScheduling.Domain.Identifiers;

namespace WorkforceScheduling.Functions.Authentication;

/// <summary>Maps only validated token group IDs to configured Dataverse role scopes.</summary>
public sealed class TokenRoleScopeResolver(IBranchMembershipStore store) : IRoleScopeResolver
{
    public Task<IReadOnlyCollection<RoleScopeAssignment>> GetAssignmentsAsync(
        PrincipalId principalId,
        IReadOnlyCollection<string> securityGroupIds,
        CancellationToken cancellationToken) =>
        store.GetRoleAssignmentsAsync(principalId, securityGroupIds, cancellationToken);
}
