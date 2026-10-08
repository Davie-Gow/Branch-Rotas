using System.Text.Json;
using WorkforceScheduling.Domain.Branches;
using WorkforceScheduling.Domain.Identifiers;

namespace WorkforceScheduling.Functions.Dataverse;

/// <summary>Persists branch operation progress without deleting resources or history.</summary>
public sealed class DataverseBranchLifecycleStore(IDataverseClient dataverse) : IBranchLifecycleStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public Task MarkProvisioningAsync(
        BranchId branchId,
        string operationId,
        CancellationToken cancellationToken) =>
        UpdateAsync(branchId, new
        {
            ws_lifecyclestate = nameof(BranchLifecycleState.Provisioning),
            ws_lastoperationid = operationId
        }, cancellationToken);

    public async Task MarkActiveAsync(
        BranchId branchId,
        string teamId,
        string operationId,
        CancellationToken cancellationToken)
    {
        await UpdateAsync(branchId, new
        {
            ws_lifecyclestate = nameof(BranchLifecycleState.Active),
            ws_teamid = teamId,
            ws_lastoperationid = operationId
        }, cancellationToken);
    }

    public Task MarkFailedAsync(
        BranchId branchId,
        string operationId,
        BranchLifecycleState previousState,
        string errorCategory,
        CancellationToken cancellationToken) =>
        UpdateAsync(branchId, new
        {
            ws_lifecyclestate = nameof(previousState),
            ws_lastoperationid = operationId,
            ws_lasterrorcategory = errorCategory
        }, cancellationToken);

    private async Task UpdateAsync(BranchId branchId, object payload, CancellationToken cancellationToken)
    {
        var filter = Uri.EscapeDataString($"ws_branchid eq '{branchId.Value.Replace("'", "''", StringComparison.Ordinal)}'");
        var path = $"ws_branchconfigurations?$select=ws_branchconfigurationid&$filter={filter}&$top=2";
        using var response = await dataverse.GetAsync(path, cancellationToken);
        var records = response is not null
            && response.RootElement.TryGetProperty("value", out var values)
            ? values.EnumerateArray().ToArray()
            : [];
        if (records.Length != 1
            || !records[0].TryGetProperty("ws_branchconfigurationid", out var rowIdElement)
            || !Guid.TryParse(rowIdElement.GetString(), out var rowId))
        {
            throw new InvalidOperationException("Branch lifecycle update requires exactly one configured branch row.");
        }

        await dataverse.PatchAsync($"ws_branchconfigurations({rowId:D})", payload, cancellationToken);
    }
}
