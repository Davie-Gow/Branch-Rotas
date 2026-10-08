using System.Text.Json;
using System.Web;
using WorkforceScheduling.Domain.Authorization;

namespace WorkforceScheduling.Functions.Graph;

/// <summary>Searches only enabled internal Entra users and returns the minimum picker profile.</summary>
public sealed class GraphUserDirectory(IGraphClient graph) : IUserDirectory
{
    public async Task<DirectoryUser?> GetEnabledInternalUserAsync(
        string userObjectId,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(userObjectId, out var objectId))
        {
            return null;
        }

        using var response = await graph.SendAsync(
            HttpMethod.Get,
            $"users/{objectId:D}?$select=id,displayName,mail,userPrincipalName,accountEnabled,userType",
            null,
            cancellationToken);
        if (response is null)
        {
            return null;
        }

        try
        {
            return MapUser(response.RootElement);
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    public async Task<IReadOnlyCollection<DirectoryUser>> SearchEnabledUsersAsync(
        string query,
        int limit,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);
        if (query.Trim().Length < 2 || query.Length > 80)
        {
            throw new ArgumentException("User search must contain 2 to 80 characters.", nameof(query));
        }

        if (limit is < 1 or > 20)
        {
            throw new ArgumentOutOfRangeException(nameof(limit), "Search results are limited to 20 users.");
        }

        var prefix = query.Trim().Replace("'", "''", StringComparison.Ordinal);
        var filter = $"accountEnabled eq true and userType eq 'Member' and startswith(displayName,'{prefix}')";
        var path = $"users?$select=id,displayName,mail,userPrincipalName,accountEnabled,userType&$filter={HttpUtility.UrlEncode(filter)}&$top={limit}&$count=true";
        using var response = await graph.SendAsync(HttpMethod.Get, path, null, cancellationToken);
        if (response is null
            || !response.RootElement.TryGetProperty("value", out var values)
            || values.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException("Microsoft Graph returned an invalid user search response.");
        }

        return values.EnumerateArray()
            .Select(MapUser)
            .ToArray();
    }

    private static DirectoryUser MapUser(JsonElement item)
    {
        var id = ReadRequired(item, "id");
        if (!Guid.TryParse(id, out var objectId))
        {
            throw new InvalidOperationException("Microsoft Graph returned a user without a valid object ID.");
        }

        if (!item.TryGetProperty("accountEnabled", out var enabled)
            || enabled.ValueKind != JsonValueKind.True
            || !string.Equals(ReadOptional(item, "userType"), "Member", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Microsoft Graph returned a user outside the enabled internal-account filter.");
        }

        return new DirectoryUser(
            objectId.ToString("D"),
            ReadRequired(item, "displayName"),
            ReadOptional(item, "mail"),
            ReadOptional(item, "userPrincipalName"));
    }

    private static string ReadRequired(JsonElement item, string property) =>
        ReadOptional(item, property)
        ?? throw new InvalidOperationException($"Microsoft Graph user response is missing {property}.");

    private static string? ReadOptional(JsonElement item, string property) =>
        item.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
}
