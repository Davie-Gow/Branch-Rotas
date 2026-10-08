namespace WorkforceScheduling.Domain.Errors;

/// <summary>Stable error categories shared by the API, orchestration, and user interface.</summary>
public enum ErrorCategory
{
    /// <summary>The request or business rule is invalid.</summary>
    Validation,
    /// <summary>The authenticated principal lacks the required scope.</summary>
    Authorisation,
    /// <summary>Required authoritative branch or identity data is missing or inconsistent.</summary>
    MasterData,
    /// <summary>Environment or solution configuration is invalid.</summary>
    Configuration,
    /// <summary>An external dependency is unavailable.</summary>
    Dependency,
    /// <summary>The external service rate-limited the request.</summary>
    Throttling,
    /// <summary>A temporary service or network failure occurred.</summary>
    Transient,
    /// <summary>A concurrent or competing operation prevents the requested change.</summary>
    Conflict,
    /// <summary>An external rejection is not expected to recover through retry.</summary>
    Permanent,
    /// <summary>The failure is unclassified and must be triaged before retry.</summary>
    Unknown
}
