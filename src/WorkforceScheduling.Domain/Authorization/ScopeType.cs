namespace WorkforceScheduling.Domain.Authorization;

/// <summary>The stable organizational boundary attached to an approved role assignment.</summary>
public enum ScopeType
{
    /// <summary>A single stable branch identifier.</summary>
    Branch,
    /// <summary>A single stable franchise identifier.</summary>
    Franchise,
    /// <summary>The explicitly assigned trial scope.</summary>
    Trial
}
