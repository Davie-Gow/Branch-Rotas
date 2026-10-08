using Microsoft.Azure.Functions.Worker.Http;

namespace WorkforceScheduling.Functions.Authentication;

/// <summary>Validates inbound Entra access tokens against the configured API audience and tenant.</summary>
public interface IEntraTokenValidator
{
    Task<AuthenticatedRequester?> ValidateAsync(HttpRequestData request, CancellationToken cancellationToken);
}
