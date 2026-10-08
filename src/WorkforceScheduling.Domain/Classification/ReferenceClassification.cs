namespace WorkforceScheduling.Domain.Classification;

/// <summary>A controlled Shifts/reporting classification; free text is not a reporting key.</summary>
/// <param name="Code">The stable controlled code.</param>
/// <param name="Type">The classification category.</param>
/// <param name="DisplayName">The user-facing label; never a reporting key.</param>
/// <param name="WorkingTime">Whether the classification represents working time.</param>
/// <param name="SalesFloor">Whether the classification counts as sales-floor time.</param>
/// <param name="IsApproved">Whether this version has governance approval for promotion.</param>
/// <param name="Version">The approved taxonomy version.</param>
public sealed record ReferenceClassification(
    string Code,
    string Type,
    string DisplayName,
    bool WorkingTime,
    bool SalesFloor,
    bool IsApproved,
    int Version);
