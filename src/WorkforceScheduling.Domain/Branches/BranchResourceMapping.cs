using WorkforceScheduling.Domain.Identifiers;

namespace WorkforceScheduling.Domain.Branches;

/// <summary>Trusted mapping from a branch to its existing Microsoft 365 resources.</summary>
/// <param name="Branch">The branch's lifecycle and authorization configuration.</param>
/// <param name="DataverseRowId">The Dataverse primary key used for safe updates.</param>
/// <param name="Microsoft365GroupId">The existing SharePoint-connected group object ID.</param>
/// <param name="TeamId">The reconciled team ID, if one has been recorded.</param>
/// <param name="TimeZone">The approved IANA timezone required to create a Shifts schedule.</param>
/// <param name="SharePointSiteUrl">The existing branch group's SharePoint site URL.</param>
public sealed record BranchResourceMapping(
    BranchConfiguration Branch,
    string DataverseRowId,
    string Microsoft365GroupId,
    string? TeamId,
    string TimeZone,
    string SharePointSiteUrl);
