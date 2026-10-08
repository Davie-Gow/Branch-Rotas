using WorkforceScheduling.Domain.Authorization;
using WorkforceScheduling.Domain.Branches;
using WorkforceScheduling.Domain.Identifiers;
using Xunit;

namespace WorkforceScheduling.Domain.Tests.Authorization;

public sealed class MembershipAuthorizationTests
{
    private static readonly BranchConfiguration Branch = new(
        BranchId.Create("branch-001"),
        FranchiseId.Create("franchise-001"),
        IsInTrialScope: true,
        BranchLifecycleState.Active);

    [Fact]
    public async Task BranchManager_can_manage_only_assigned_branch()
    {
        var policy = CreatePolicy(new RoleScopeAssignment(
            OperationalRole.BranchManager,
            ScopeType.Branch,
            Branch.BranchId.Value));

        Assert.True(await policy.CanManageMembershipAsync(
            PrincipalId.Create("principal-001"), [], Branch, CancellationToken.None));

        var otherBranch = Branch with { BranchId = BranchId.Create("branch-002") };
        Assert.False(await policy.CanManageMembershipAsync(
            PrincipalId.Create("principal-001"), [], otherBranch, CancellationToken.None));
    }

    [Fact]
    public async Task FranchiseManager_can_manage_branch_in_assigned_franchise()
    {
        var policy = CreatePolicy(new RoleScopeAssignment(
            OperationalRole.FranchiseManager,
            ScopeType.Franchise,
            Branch.FranchiseId.Value));

        Assert.True(await policy.CanManageMembershipAsync(
            PrincipalId.Create("principal-001"), [], Branch, CancellationToken.None));
    }

    [Fact]
    public async Task Support_admin_requires_explicit_trial_scope()
    {
        var policy = CreatePolicy(new RoleScopeAssignment(
            OperationalRole.PlatformSupportAdmin,
            ScopeType.Trial,
            MembershipAuthorization.TrialScopeId));

        Assert.True(await policy.CanManageMembershipAsync(
            PrincipalId.Create("principal-001"), [], Branch, CancellationToken.None));

        var incorrectlyScoped = CreatePolicy(new RoleScopeAssignment(
            OperationalRole.PlatformSupportAdmin,
            ScopeType.Trial,
            "all"));
        Assert.False(await incorrectlyScoped.CanManageMembershipAsync(
            PrincipalId.Create("principal-001"), [], Branch, CancellationToken.None));
    }

    [Theory]
    [InlineData(false, BranchLifecycleState.Active)]
    [InlineData(true, BranchLifecycleState.NotEnabled)]
    [InlineData(true, BranchLifecycleState.Provisioning)]
    [InlineData(true, BranchLifecycleState.Suspended)]
    [InlineData(true, BranchLifecycleState.DecommissionPending)]
    [InlineData(true, BranchLifecycleState.Archived)]
    public async Task Ineligible_branch_is_denied_even_with_trial_admin(
        bool inTrialScope,
        BranchLifecycleState lifecycleState)
    {
        var branch = Branch with
        {
            IsInTrialScope = inTrialScope,
            LifecycleState = lifecycleState
        };
        var policy = CreatePolicy(new RoleScopeAssignment(
            OperationalRole.PlatformSupportAdmin,
            ScopeType.Trial,
            MembershipAuthorization.TrialScopeId));

        Assert.False(await policy.CanManageMembershipAsync(
            PrincipalId.Create("principal-001"), [], branch, CancellationToken.None));
    }

    [Fact]
    public async Task Ordinary_branch_membership_does_not_grant_management_role()
    {
        // Only the role-scope resolver can grant a role; group membership is not consulted here.
        var policy = CreatePolicy();

        Assert.False(await policy.CanManageMembershipAsync(
            PrincipalId.Create("ordinary-staff"), [], Branch, CancellationToken.None));
    }

    private static MembershipAuthorization CreatePolicy(params RoleScopeAssignment[] assignments) =>
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
