namespace WorkforceScheduling.Domain.Authorization;

/// <summary>Changes ordinary user membership in a preconfigured Microsoft 365 group.</summary>
public interface IBranchGroupMembership
{
    Task ApplyAsync(
        string groupId,
        string userObjectId,
        MembershipChangeAction action,
        CancellationToken cancellationToken);
}
