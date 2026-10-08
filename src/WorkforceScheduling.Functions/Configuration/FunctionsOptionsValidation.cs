using Microsoft.Extensions.Options;

namespace WorkforceScheduling.Functions.Configuration;

/// <summary>Rejects placeholder tenant values and unsafe service endpoints at startup.</summary>
public sealed class EntraAuthenticationOptionsValidation : IValidateOptions<EntraAuthenticationOptions>
{
    public ValidateOptionsResult Validate(string? name, EntraAuthenticationOptions options)
    {
        if (!Guid.TryParse(options.TenantId, out _))
        {
            return ValidateOptionsResult.Fail("Entra:TenantId must be the configured tenant GUID.");
        }
        if (!Uri.TryCreate(options.AuthorityHost, UriKind.Absolute, out var authority)
            || authority.Scheme != Uri.UriSchemeHttps)
        {
            return ValidateOptionsResult.Fail("Entra:AuthorityHost must be an HTTPS URL.");
        }

        return ValidateOptionsResult.Success;
    }

}

/// <summary>Validates configured external API endpoints before the host accepts requests.</summary>
public sealed class WorkforceOptionsValidation : IValidateOptions<WorkforceOptions>
{
    public ValidateOptionsResult Validate(string? name, WorkforceOptions options)
    {
        if (!Uri.TryCreate(options.DataverseUrl, UriKind.Absolute, out var dataverse)
            || dataverse.Scheme != Uri.UriSchemeHttps)
        {
            return ValidateOptionsResult.Fail("Workforce:DataverseUrl must be an HTTPS URL.");
        }

        if (!Uri.TryCreate(options.LockStorageServiceUri, UriKind.Absolute, out var storage)
            || storage.Scheme != Uri.UriSchemeHttps)
        {
            return ValidateOptionsResult.Fail("Workforce:LockStorageServiceUri must be an HTTPS Blob endpoint.");
        }

        if (!options.GraphScope.EndsWith("/.default", StringComparison.Ordinal))
        {
            return ValidateOptionsResult.Fail("Workforce:GraphScope must use the approved .default scope.");
        }

        if (!string.IsNullOrWhiteSpace(options.ManagedIdentityClientId)
            && !Guid.TryParse(options.ManagedIdentityClientId, out _))
        {
            return ValidateOptionsResult.Fail("Workforce:ManagedIdentityClientId must be a client ID GUID.");
        }

        return ValidateOptionsResult.Success;
    }
}
