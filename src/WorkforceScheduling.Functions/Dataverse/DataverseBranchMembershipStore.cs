using System.Globalization;
using System.Text.Json;
using WorkforceScheduling.Domain.Authorization;
using WorkforceScheduling.Domain.Branches;
using WorkforceScheduling.Domain.Identifiers;

namespace WorkforceScheduling.Functions.Dataverse;

/// <summary>Loads branch and role-scope records from the approved Dataverse contract.</summary>
public sealed class DataverseBranchMembershipStore(IDataverseClient dataverse) : IBranchMembershipStore
{
    public async Task<BranchResourceMapping?> GetBranchAsync(
        BranchId branchId,
        CancellationToken cancellationToken)
    {
        var filter = Uri.EscapeDataString($"ws_branchid eq '{EscapeODataLiteral(branchId.Value)}'");
        var path = $"ws_branchconfigurations?$select=ws_branchconfigurationid,ws_branchid,ws_franchiseid,ws_intrialscope,ws_lifecyclestate,ws_m365groupid,ws_teamid,ws_timezone,ws_sharepointsiteurl&$filter={filter}&$top=2";
        using var response = await dataverse.GetAsync(path, cancellationToken);
        var records = GetRecords(response);
        if (records.Count == 0)
        {
            return null;
        }

        if (records.Count != 1)
        {
            throw new InvalidOperationException("Dataverse branch configuration must contain one row per stable BranchId.");
        }

        var record = records[0];
        var branch = new BranchConfiguration(
            BranchId.Create(GetRequiredString(record, "ws_branchid")),
            FranchiseId.Create(GetRequiredString(record, "ws_franchiseid")),
            GetRequiredBoolean(record, "ws_intrialscope"),
            ParseLifecycle(GetRequiredString(record, "ws_lifecyclestate")));
        return new BranchResourceMapping(
            branch,
            GetRequiredGuid(record, "ws_branchconfigurationid"),
            GetRequiredGuid(record, "ws_m365groupid"),
            GetOptionalGuid(record, "ws_teamid"),
            GetRequiredString(record, "ws_timezone"),
            GetRequiredHttpsUrl(record, "ws_sharepointsiteurl"));
    }

    public async Task<IReadOnlyCollection<RoleScopeAssignment>> GetRoleAssignmentsAsync(
        PrincipalId principalId,
        IReadOnlyCollection<string> securityGroupIds,
        CancellationToken cancellationToken)
    {
        if (securityGroupIds.Count == 0)
        {
            return [];
        }

        var groupIds = securityGroupIds.Where(id => Guid.TryParse(id, out _))
            .Select(id => Guid.Parse(id))
            .Distinct()
            .ToArray();
        if (groupIds.Length == 0)
        {
            return [];
        }

        var groupFilter = string.Join(" or ", groupIds.Select(id => $"ws_entraagroupid eq {id:D}"));
        var filter = Uri.EscapeDataString($"({groupFilter})");
        var path = $"ws_accessassignments?$select=ws_entraagroupid,ws_role,ws_scopetype,ws_scopeid,ws_effectivefrom,ws_effectiveto&$filter={filter}";
        using var response = await dataverse.GetAsync(path, cancellationToken);
        return GetRecords(response)
            .Where(record => IsEffective(record, DateTimeOffset.UtcNow))
            .Select(MapAssignment)
            .Distinct()
            .ToArray();
    }

    private static RoleScopeAssignment MapAssignment(JsonElement record)
    {
        var role = GetRequiredString(record, "ws_role") switch
        {
            "BranchManager" => OperationalRole.BranchManager,
            "FranchiseManager" => OperationalRole.FranchiseManager,
            "PlatformSupportAdmin" => OperationalRole.PlatformSupportAdmin,
            _ => throw new InvalidOperationException("Dataverse contains an unrecognized access-assignment role.")
        };
        var scopeType = GetRequiredString(record, "ws_scopetype") switch
        {
            "Branch" => ScopeType.Branch,
            "Franchise" => ScopeType.Franchise,
            "Trial" => ScopeType.Trial,
            _ => throw new InvalidOperationException("Dataverse contains an unrecognized access-assignment scope type.")
        };
        return new RoleScopeAssignment(role, scopeType, GetRequiredString(record, "ws_scopeid"));
    }

    private static bool IsEffective(JsonElement record, DateTimeOffset now)
    {
        var from = GetOptionalDate(record, "ws_effectivefrom");
        var to = GetOptionalDate(record, "ws_effectiveto");
        return (from is null || from <= now) && (to is null || to >= now);
    }

    private static BranchLifecycleState ParseLifecycle(string value) =>
        Enum.TryParse<BranchLifecycleState>(value, ignoreCase: false, out var state)
            ? state
            : throw new InvalidOperationException("Dataverse contains an unknown branch lifecycle state.");

    private static string EscapeODataLiteral(string value) => value.Replace("'", "''", StringComparison.Ordinal);

    private static IReadOnlyList<JsonElement> GetRecords(JsonDocument? response)
    {
        if (response is null || !response.RootElement.TryGetProperty("value", out var values))
        {
            return [];
        }

        return values.EnumerateArray().Select(element => element.Clone()).ToArray();
    }

    private static string GetRequiredString(JsonElement record, string field) =>
        record.TryGetProperty(field, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()!
            : throw new InvalidOperationException($"Dataverse branch or access configuration is missing {field}.");

    private static string? GetOptionalGuid(JsonElement record, string field) =>
        record.TryGetProperty(field, out var value) && value.ValueKind == JsonValueKind.String
            ? Guid.TryParse(value.GetString(), out var id) ? id.ToString("D") : null
            : null;

    private static string GetRequiredGuid(JsonElement record, string field) =>
        GetOptionalGuid(record, field)
        ?? throw new InvalidOperationException($"Dataverse configuration contains an invalid {field}.");

    private static string GetRequiredHttpsUrl(JsonElement record, string field)
    {
        var value = GetRequiredString(record, field);
        return Uri.TryCreate(value, UriKind.Absolute, out var uri)
            && uri.Scheme == Uri.UriSchemeHttps
            && string.IsNullOrEmpty(uri.UserInfo)
                ? uri.AbsoluteUri
                : throw new InvalidOperationException($"Dataverse configuration contains an invalid {field}.");
    }

    private static bool GetRequiredBoolean(JsonElement record, string field) =>
        record.TryGetProperty(field, out var value) && value.ValueKind is JsonValueKind.True or JsonValueKind.False
            ? value.GetBoolean()
            : throw new InvalidOperationException($"Dataverse branch configuration is missing {field}.");

    private static DateTimeOffset? GetOptionalDate(JsonElement record, string field) =>
        record.TryGetProperty(field, out var value) && value.ValueKind == JsonValueKind.String
            ? DateTimeOffset.Parse(value.GetString()!, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal)
            : null;
}
