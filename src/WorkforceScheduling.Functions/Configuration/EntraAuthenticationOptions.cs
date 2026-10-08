using System.ComponentModel.DataAnnotations;

namespace WorkforceScheduling.Functions.Configuration;

/// <summary>Expected tenant and API audience for inbound Entra access tokens.</summary>
public sealed class EntraAuthenticationOptions
{
    public const string SectionName = "Entra";

    [Required]
    public string TenantId { get; init; } = string.Empty;

    [Required]
    public string Audience { get; init; } = string.Empty;

    public string AuthorityHost { get; init; } = "https://login.microsoftonline.com";
}
