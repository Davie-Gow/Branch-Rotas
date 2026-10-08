using System.Net.Http;
using System.Collections.Generic;
using WorkforceScheduling.Domain.Authorization;

namespace WorkforceScheduling.Functions.Graph;

/// <summary>Idempotently adds/removes users from an existing Microsoft 365 branch group.</summary>
public sealed class GraphBranchGroupMembership(IGraphClient graph) : IBranchGroupMembership
{
    private const string GraphRoot = "https://graph.microsoft.com/v1.0";

    public async Task ApplyAsync(
        string groupId,
        string userObjectId,
        MembershipChangeAction action,
        CancellationToken cancellationToken)
    {
        var safeGroupId = RequireGuid(groupId, nameof(groupId));
        var safeUserId = RequireGuid(userObjectId, nameof(userObjectId));
        var memberPath = $"groups/{safeGroupId}/members/{safeUserId}?$select=id";
        using var currentMembership = await graph.SendAsync(
            HttpMethod.Get, memberPath, null, cancellationToken);

        if (action == MembershipChangeAction.Add)
        {
            if (currentMembership is not null)
            {
                return;
            }

            await graph.SendAsync(
                HttpMethod.Post,
                $"groups/{safeGroupId}/members/$ref",
                new Dictionary<string, string>
                {
                    ["@odata.id"] = $"{GraphRoot}/directoryObjects/{safeUserId}"
                },
                cancellationToken);
            return;
        }

        if (currentMembership is not null)
        {
            await graph.SendAsync(
                HttpMethod.Delete,
                $"groups/{safeGroupId}/members/{safeUserId}/$ref",
                null,
                cancellationToken);
        }
    }

    private static string RequireGuid(string value, string parameterName) =>
        Guid.TryParse(value, out var parsed)
            ? parsed.ToString("D")
            : throw new ArgumentException("Graph object identifiers must be GUIDs.", parameterName);
}
