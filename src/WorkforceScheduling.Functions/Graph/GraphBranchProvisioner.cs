using System.Net.Http;
using System.Text.Json;
using WorkforceScheduling.Domain.Branches;

namespace WorkforceScheduling.Functions.Graph;

/// <summary>Creates missing Teams/Shifts resources but never replaces an existing schedule.</summary>
public sealed class GraphBranchProvisioner(IGraphClient graph) : IBranchProvisioner
{
    public async Task<BranchProvisioningResult> ProvisionAsync(
        BranchResourceMapping mapping,
        CancellationToken cancellationToken)
    {
        var groupId = RequireGuid(mapping.Microsoft365GroupId, "Microsoft 365 group");
        if (!string.Equals(mapping.Microsoft365GroupId, groupId, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("The configured branch group ID is not a canonical GUID.");
        }

        var (teamId, teamReady) = await EnsureTeamAsync(groupId, cancellationToken);
        if (!teamReady)
        {
            return new BranchProvisioningResult(teamId, false);
        }

        var scheduleReady = await EnsureScheduleAsync(teamId, mapping.TimeZone, cancellationToken);
        return new BranchProvisioningResult(teamId, scheduleReady);
    }

    private async Task<(string TeamId, bool Ready)> EnsureTeamAsync(
        string groupId,
        CancellationToken cancellationToken)
    {
        using var existing = await graph.SendAsync(HttpMethod.Get, $"teams/{groupId}?$select=id", null, cancellationToken);
        if (existing is not null)
        {
            return (ReadId(existing.RootElement), true);
        }

        try
        {
            using var creationResponse = await graph.SendAsync(
                HttpMethod.Put,
                $"groups/{groupId}/team",
                new { },
                cancellationToken);
        }
        catch (GraphApiException exception) when (exception.StatusCode == 409)
        {
            // A concurrent provisioner may already be creating the Team.
        }

        // Team creation may be asynchronous; the durable orchestrator will poll again.
        using var reconciled = await graph.SendAsync(
            HttpMethod.Get,
            $"teams/{groupId}?$select=id",
            null,
            cancellationToken);
        return reconciled is null
            ? (groupId, false)
            : (ReadId(reconciled.RootElement), true);
    }

    private async Task<bool> EnsureScheduleAsync(
        string teamId,
        string timeZone,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(timeZone))
        {
            throw new InvalidOperationException("An approved IANA timezone is required before creating a Shifts schedule.");
        }

        using var existing = await graph.SendAsync(
            HttpMethod.Get,
            $"teams/{teamId}/schedule",
            null,
            cancellationToken);
        if (existing is not null)
        {
            return true;
        }

        try
        {
            using var creationResponse = await graph.SendAsync(
                HttpMethod.Put,
                $"teams/{teamId}/schedule",
                new { enabled = true, timeZone },
                cancellationToken);
        }
        catch (GraphApiException exception) when (exception.StatusCode == 409)
        {
            // A prior accepted schedule creation may still be converging.
        }

        // Only report success if the asynchronous schedule creation became observable.
        using var reconciled = await graph.SendAsync(
            HttpMethod.Get,
            $"teams/{teamId}/schedule",
            null,
            cancellationToken);
        return reconciled is not null;
    }

    private static string ReadId(JsonElement response) =>
        response.TryGetProperty("id", out var id)
        && id.ValueKind == JsonValueKind.String
        && Guid.TryParse(id.GetString(), out var parsed)
            ? parsed.ToString("D")
            : throw new InvalidOperationException("Microsoft Graph returned a resource without a valid ID.");

    private static string RequireGuid(string value, string name) =>
        Guid.TryParse(value, out var parsed)
            ? parsed.ToString("D")
            : throw new InvalidOperationException($"Configured {name} ID must be a GUID.");
}
