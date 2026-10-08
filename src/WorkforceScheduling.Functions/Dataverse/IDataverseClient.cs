using System.Text.Json;

namespace WorkforceScheduling.Functions.Dataverse;

/// <summary>Small Dataverse Web API surface; callers provide trusted logical schema names.</summary>
public interface IDataverseClient
{
    Task<JsonDocument?> GetAsync(string relativePath, CancellationToken cancellationToken);

    Task PostAsync(
        string relativePath,
        object payload,
        CancellationToken cancellationToken);

    Task PatchAsync(
        string relativePath,
        object payload,
        CancellationToken cancellationToken);
}
