using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Azure.Core;
using Microsoft.Extensions.Options;
using WorkforceScheduling.Functions.Configuration;

namespace WorkforceScheduling.Functions.Dataverse;

/// <summary>Calls Dataverse using the Functions workload identity and a configured environment URL.</summary>
public sealed class DataverseClient(
    IHttpClientFactory httpClientFactory,
    TokenCredential credential,
    IOptions<WorkforceOptions> options) : IDataverseClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly WorkforceOptions _options = options.Value;

    public async Task<JsonDocument?> GetAsync(string relativePath, CancellationToken cancellationToken)
    {
        using var request = await CreateRequestAsync(HttpMethod.Get, relativePath, cancellationToken);
        using var response = await httpClientFactory.CreateClient("Dataverse")
            .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureSuccessAsync(response, cancellationToken);
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
    }

    public async Task PostAsync(
        string relativePath,
        object payload,
        CancellationToken cancellationToken)
    {
        using var request = await CreateRequestAsync(HttpMethod.Post, relativePath, cancellationToken);
        request.Headers.TryAddWithoutValidation("Prefer", "return=minimal");
        request.Content = JsonContent.Create(payload, options: JsonOptions);
        using var response = await httpClientFactory.CreateClient("Dataverse")
            .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    public async Task PatchAsync(
        string relativePath,
        object payload,
        CancellationToken cancellationToken)
    {
        using var request = await CreateRequestAsync(HttpMethod.Patch, relativePath, cancellationToken);
        request.Headers.TryAddWithoutValidation("If-Match", "*");
        request.Content = JsonContent.Create(payload, options: JsonOptions);
        using var response = await httpClientFactory.CreateClient("Dataverse")
            .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    private async Task<HttpRequestMessage> CreateRequestAsync(
        HttpMethod method,
        string relativePath,
        CancellationToken cancellationToken)
    {
        if (relativePath.StartsWith('/') || relativePath.Contains("..", StringComparison.Ordinal))
        {
            throw new ArgumentException("Dataverse paths must be relative to the configured API root.", nameof(relativePath));
        }

        var resource = $"{_options.DataverseUrl.TrimEnd('/')}/api/data/{_options.DataverseApiVersion}/";
        var token = await credential.GetTokenAsync(
            new TokenRequestContext([$"{_options.DataverseUrl.TrimEnd('/')}/.default"]),
            cancellationToken);
        var request = new HttpRequestMessage(method, new Uri(new Uri(resource), relativePath));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);
        request.Headers.TryAddWithoutValidation("OData-Version", "4.0");
        request.Headers.TryAddWithoutValidation("OData-MaxVersion", "4.0");
        request.Headers.TryAddWithoutValidation("Accept", "application/json");
        return request;
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var code = "Unknown";
        if (response.Content.Headers.ContentLength is > 0 and < 32_768)
        {
            using var body = await JsonDocument.ParseAsync(
                await response.Content.ReadAsStreamAsync(cancellationToken),
                cancellationToken: cancellationToken);
            if (body.RootElement.TryGetProperty("error", out var error)
                && error.TryGetProperty("code", out var codeElement)
                && codeElement.ValueKind == JsonValueKind.String)
            {
                code = codeElement.GetString() ?? code;
            }
        }

        throw new DataverseApiException((int)response.StatusCode, code);
    }
}
