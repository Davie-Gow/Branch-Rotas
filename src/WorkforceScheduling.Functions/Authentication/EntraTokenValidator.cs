using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using WorkforceScheduling.Domain.Identifiers;
using WorkforceScheduling.Functions.Configuration;

namespace WorkforceScheduling.Functions.Authentication;

/// <summary>Performs signature, issuer, audience, and lifetime checks for each caller token.</summary>
public sealed class EntraTokenValidator : IEntraTokenValidator
{
    private readonly EntraAuthenticationOptions _options;
    private readonly ConfigurationManager<OpenIdConnectConfiguration> _configurationManager;
    private readonly JwtSecurityTokenHandler _tokenHandler = new() { MapInboundClaims = false };

    public EntraTokenValidator(IOptions<EntraAuthenticationOptions> options)
    {
        _options = options.Value;
        var metadataAddress = $"{_options.AuthorityHost.TrimEnd('/')}/{_options.TenantId}/v2.0/.well-known/openid-configuration";
        _configurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
            metadataAddress,
            new OpenIdConnectConfigurationRetriever(),
            new HttpDocumentRetriever { RequireHttps = true });
    }

    public async Task<AuthenticatedRequester?> ValidateAsync(
        HttpRequestData request,
        CancellationToken cancellationToken)
    {
        if (!request.Headers.TryGetValues("Authorization", out var values))
        {
            return null;
        }

        var authorization = values.FirstOrDefault();
        if (authorization is null
            || !authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var token = authorization["Bearer ".Length..].Trim();
        if (token.Length == 0)
        {
            return null;
        }

        try
        {
            var configuration = await _configurationManager.GetConfigurationAsync(cancellationToken);
            var validIssuer = $"{_options.AuthorityHost.TrimEnd('/')}/{_options.TenantId}/v2.0";
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = validIssuer,
                ValidateAudience = true,
                ValidAudience = _options.Audience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKeys = configuration.SigningKeys,
                ValidateLifetime = true,
                RequireExpirationTime = true,
                ClockSkew = TimeSpan.FromMinutes(2),
                NameClaimType = "name"
            };

            var claimsPrincipal = _tokenHandler.ValidateToken(token, validationParameters, out _);
            var tenantId = claimsPrincipal.FindFirstValue("tid");
            var objectId = claimsPrincipal.FindFirstValue("oid");
            var scopes = claimsPrincipal.FindFirstValue("scp")?
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                ?? [];
            if (!string.Equals(tenantId, _options.TenantId, StringComparison.OrdinalIgnoreCase)
                || !Guid.TryParse(objectId, out _)
                || !scopes.Contains("access_as_user", StringComparer.Ordinal))
            {
                return null;
            }

            // Group-overage tokens omit the group IDs needed for the fail-closed scope check.
            if (claimsPrincipal.HasClaim(claim => claim.Type is "hasgroups" or "_claim_names"))
            {
                return null;
            }

            var groups = claimsPrincipal.FindAll("groups")
                .Select(claim => claim.Value)
                .Where(value => Guid.TryParse(value, out _))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            return new AuthenticatedRequester(PrincipalId.Create(objectId!), groups);
        }
        catch (SecurityTokenException)
        {
            return null;
        }
    }
}
