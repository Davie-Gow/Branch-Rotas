using System.Net.Http;
using System.Text.Json;

namespace WorkforceScheduling.Functions.Graph;

/// <summary>Calls only Microsoft Graph v1.0 paths through a workload identity.</summary>
public interface IGraphClient
{
    Task<JsonDocument?> SendAsync(
        HttpMethod method,
        string relativePath,
        object? payload,
        CancellationToken cancellationToken);
}
