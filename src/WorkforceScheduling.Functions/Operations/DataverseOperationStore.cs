using WorkforceScheduling.Functions.Dataverse;

namespace WorkforceScheduling.Functions.Operations;

/// <summary>Persists operations using the configured unique Dataverse operation-ID alternate key.</summary>
public sealed class DataverseOperationStore(IDataverseClient dataverse) : IOperationStore
{
    public Task CreateAsync(OperationAuditRecord operation, CancellationToken cancellationToken) =>
        dataverse.PostAsync("ws_provisioningoperations", new
        {
            ws_operationid = operation.OperationId,
            ws_branchid = operation.BranchId,
            ws_operationtype = operation.OperationType,
            ws_state = operation.State,
            ws_step = "Accepted",
            ws_correlationid = operation.CorrelationId,
            ws_requestedby = operation.RequestedBy,
            ws_targetuserobjectid = operation.TargetUserObjectId,
            ws_action = operation.Action,
            ws_errorcategory = operation.ErrorCategory,
            ws_startedon = DateTimeOffset.UtcNow
        }, cancellationToken);

    public async Task<OperationAuditRecord?> GetAsync(string operationId, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(operationId, out var id))
        {
            return null;
        }

        var key = Uri.EscapeDataString(id.ToString("D"));
        using var response = await dataverse.GetAsync(
            $"ws_provisioningoperations(ws_operationid='{key}')?$select=ws_operationid,ws_branchid,ws_operationtype,ws_state,ws_correlationid,ws_requestedby,ws_targetuserobjectid,ws_action,ws_errorcategory",
            cancellationToken);
        if (response is null)
        {
            return null;
        }

        var record = response.RootElement;
        return new OperationAuditRecord(
            ReadString(record, "ws_operationid"),
            ReadString(record, "ws_branchid"),
            ReadString(record, "ws_operationtype"),
            ReadString(record, "ws_state"),
            ReadString(record, "ws_correlationid"),
            ReadString(record, "ws_requestedby"),
            ReadNullableString(record, "ws_targetuserobjectid"),
            ReadNullableString(record, "ws_action"),
            ReadNullableString(record, "ws_errorcategory"));
    }

    public Task UpdateStateAsync(
        string operationId,
        string state,
        string step,
        string? errorCategory,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(operationId, out var id))
        {
            throw new ArgumentException("Operation IDs must be GUIDs.", nameof(operationId));
        }

        var key = Uri.EscapeDataString(id.ToString("D"));
        return dataverse.PatchAsync(
            $"ws_provisioningoperations(ws_operationid='{key}')",
            new
            {
                ws_state = state,
                ws_step = step,
                ws_errorcategory = errorCategory,
                ws_completedon = state is "Succeeded" or "Failed" ? DateTimeOffset.UtcNow : (DateTimeOffset?)null
            },
            cancellationToken);
    }

    private static string ReadString(JsonElement record, string name) =>
        record.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()!
            : throw new InvalidOperationException($"Dataverse operation record is missing {name}.");

    private static string? ReadNullableString(JsonElement record, string name) =>
        record.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
}
