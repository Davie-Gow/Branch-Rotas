using System.ComponentModel.DataAnnotations;

namespace WorkforceScheduling.Functions.Configuration;

/// <summary>Environment-specific integration endpoints; values are never checked in.</summary>
public sealed class WorkforceOptions
{
    public const string SectionName = "Workforce";

    [Required]
    [Url]
    public string DataverseUrl { get; init; } = string.Empty;

    [Required]
    [RegularExpression(@"^[A-Za-z0-9.-]+$")]
    public string DataverseApiVersion { get; init; } = "v9.2";

    [Required]
    public string GraphScope { get; init; } = "https://graph.microsoft.com/.default";

    [Required]
    [Url]
    public string LockStorageServiceUri { get; init; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int ScheduleProvisioningTimeoutMinutes { get; init; }

    [Range(1, int.MaxValue)]
    public int SchedulePollingIntervalSeconds { get; init; }

    /// <summary>Client ID of the user-assigned managed identity; empty selects the host identity.</summary>
    public string? ManagedIdentityClientId { get; init; }
}
