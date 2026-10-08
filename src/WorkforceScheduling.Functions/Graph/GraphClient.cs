using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Azure.Core;
using Microsoft.Extensions.Options;
using WorkforceScheduling.Functions.Configuration;

namespace WorkforceScheduling.Functions.Graph;

/// <summary>Uses managed identity, bounded throttling retries, and sanitized errors for Graph calls.</summary>
public sealed class GraphClient(
    IHttpClientFactory httpClientFactory,
    TokenCredential credential,
    IOptions<WorkforceOptions> options) : IGraphClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly WorkforceOptions _options = options.Value;

    public async Task<JsonDocument?> SendAsync(
        HttpMethod method,
        string relativePath,
        object? payload,
        CancellationToken cancellationToken)
    {
        if (relativePath.StartsWith('/') || relativePath.Contains("..", StringComparison.Ordinal))
        {
            throw new ArgumentException("Graph paths must be relative v1.0 paths.", nameof(relativePath));
        }

        var client = httpClientFactory.CreateClient("MicrosoftGraph");
        for (var attempt = 0; attempt < 3; attempt++)
        {
            var token = await credential.GetTokenAsync(
                new TokenRequestContext([_options.GraphScope]),
                cancellationToken);
            using var request = new HttpRequestMessage(method, relativePath);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);
            request.Headers.TryAddWithoutValidation("ConsistencyLevel", "eventual");
            if (payload is not null)
            {
                request.Content = JsonContent.Create(payload, options: JsonOptions);
            }

            using var response = await client.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            if ((int)response.StatusCode is 429 or 503 or 504 && attempt < 2)
            {
                await Task.Delay(GetRetryDelay(response, attempt), cancellationToken);
                continue;
            }

            if (!response.IsSuccessStatusCode)
            {
                throw new GraphApiException((int)response.StatusCode, await GetErrorCodeAsync(response, cancellationToken));
            }

            if (response.StatusCode == HttpStatusCode.NoContent
                || response.Content.Headers.ContentLength == 0)
            {
                return null;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            return await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        }

        throw new InvalidOperationException("Graph retry loop ended without a response.");
    }

    private static TimeSpan GetRetryDelay(HttpResponseMessage response, int attempt)
    {
        var retryAfter = response.Headers.RetryAfter?.Delta;
        if (retryAfter is null && response.Headers.RetryAfter?.Date is { } retryDate)
        {
            retryAfter = retryDate - DateTimeOffset.UtcNow;
        }

        var delay = retryAfter ?? TimeSpan.FromSeconds(Math.Pow(2, attempt + 1));
        return delay < TimeSpan.Zero ? TimeSpan.Zero : TimeSpan.FromSeconds(Math.Min(delay.TotalSeconds, 30));
    }

    private static async Task<string> GetErrorCodeAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (response.Content.Headers.ContentLength is not (> 0 and < 32_768))
        {
            return "Unknown";
        }

        using var body = await JsonDocument.ParseAsync(
            await response.Content.ReadAsStreamAsync(cancellationToken),
            cancellationToken: cancellationToken);
        return body.RootElement.TryGetProperty("error", out var error)
            && error.TryGetProperty("code", out var code)
            && code.ValueKind == JsonValueKind.String
                ? code.GetString() ?? "Unknown"
                : "Unknown";
    }
}
