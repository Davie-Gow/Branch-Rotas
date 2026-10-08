using WorkforceScheduling.Domain.Authorization;
using WorkforceScheduling.Domain.Branches;
using WorkforceScheduling.Domain.Identifiers;
using WorkforceScheduling.Functions.Authentication;
using Xunit;

namespace WorkforceScheduling.Functions.Tests.Authentication;

public sealed class BranchAdministrationAuthorizationTests
{
    private static readonly BranchConfiguration Branch = new(
        BranchId.Create("branch-001"),
        FranchiseId.Create("franchise-001"),
        IsInTrialScope: true,
        BranchLifecycleState.Active);

    [Fact]
    public async Task Franchise_manager_cannot_reconcile_teams_or_shifts()
    {
        var authorization = CreateAuthorization(new RoleScopeAssignment(
            OperationalRole.FranchiseManager,
            ScopeType.Franchise,
            Branch.FranchiseId.Value));

        Assert.False(await authorization.CanAdministerBranchAsync(
            PrincipalId.Create("principal-001"), [], Branch, CancellationToken.None));
    }

    [Fact]
    public async Task Branch_manager_can_reconcile_only_assigned_branch()
    {
        var authorization = CreateAuthorization(new RoleScopeAssignment(
            OperationalRole.BranchManager,
            ScopeType.Branch,
            Branch.BranchId.Value));

        Assert.True(await authorization.CanAdministerBranchAsync(
            PrincipalId.Create("principal-001"), [], Branch, CancellationToken.None));

        var otherBranch = Branch with { BranchId = BranchId.Create("branch-002") };
        Assert.False(await authorization.CanAdministerBranchAsync(
            PrincipalId.Create("principal-001"), [], otherBranch, CancellationToken.None));
    }

    private static BranchAdministrationAuthorization CreateAuthorization(
        params RoleScopeAssignment[] assignments) =>
        new(new StubRoleScopeResolver(assignments));

    private sealed class StubRoleScopeResolver(
        IReadOnlyCollection<RoleScopeAssignment> assignments) : IRoleScopeResolver
    {
        public Task<IReadOnlyCollection<RoleScopeAssignment>> GetAssignmentsAsync(
            PrincipalId principalId,
            IReadOnlyCollection<string> securityGroupIds,
            CancellationToken cancellationToken) => Task.FromResult(assignments);
    }
}
