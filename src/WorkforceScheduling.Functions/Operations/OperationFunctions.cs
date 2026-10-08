using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using WorkforceScheduling.Domain.Authorization;
using WorkforceScheduling.Domain.Branches;
using WorkforceScheduling.Domain.Identifiers;
using WorkforceScheduling.Functions.Authentication;
using WorkforceScheduling.Functions.Dataverse;
using WorkforceScheduling.Functions.Graph;
using WorkforceScheduling.Functions.Locks;

namespace WorkforceScheduling.Functions.Operations;

public static class MembershipOperationOrchestrator
{
    [Function(nameof(MembershipOperationOrchestrator))]
    public static Task Run(
        [OrchestrationTrigger] TaskOrchestrationContext context)
    {
        var input = context.GetInput<MembershipOperationInput>()
            ?? throw new InvalidOperationException("Membership orchestration input is required.");
        return context.CallActivityAsync(nameof(ApplyMembershipActivity), input);
    }

    [Function(nameof(ApplyMembershipActivity))]
    public static async Task ApplyMembershipActivity(
        [ActivityTrigger] MembershipOperationInput input,
        IBranchMembershipStore branchStore,
        IMembershipAuthorization authorization,
        IBranchGroupMembership membership,
        IUserDirectory userDirectory,
        IBranchOperationLock operationLock,
        IOperationStore operationStore,
        ILogger<ApplyMembershipActivity> logger,
        CancellationToken cancellationToken)
    {
        try
        {
            await operationStore.UpdateStateAsync(input.OperationId, "Running", "Authorizing", null, cancellationToken);
            var branchId = BranchId.Create(input.BranchId);
            var principalId = PrincipalId.Create(input.RequestedBy);
            var branch = await branchStore.GetBranchAsync(branchId, cancellationToken)
                ?? throw new OperationRejectedException("BranchUnavailable");
            if (!await authorization.CanManageMembershipAsync(
                    principalId, input.SecurityGroupIds, branch.Branch, cancellationToken))
            {
                throw new OperationRejectedException("NotAuthorized");
            }

            await operationLock.ExecuteAsync(input.BranchId, async lockToken =>
            {
                var current = await branchStore.GetBranchAsync(branchId, lockToken)
                    ?? throw new OperationRejectedException("BranchUnavailable");
                if (!await authorization.CanManageMembershipAsync(
                        principalId, input.SecurityGroupIds, current.Branch, lockToken))
                {
                    throw new OperationRejectedException("NotAuthorized");
                }

                await operationStore.UpdateStateAsync(input.OperationId, "Running", "ApplyingMembership", null, lockToken);
                if (!Enum.TryParse<MembershipChangeAction>(input.Action, false, out var action)
                    || !Enum.IsDefined(action))
                {
                    throw new OperationRejectedException("InvalidMembershipAction");
                }

                if (action == MembershipChangeAction.Add
                    && await userDirectory.GetEnabledInternalUserAsync(input.UserObjectId, lockToken) is null)
                {
                    throw new OperationRejectedException("TargetUserUnavailable");
                }

                await membership.ApplyAsync(
                    current.Microsoft365GroupId, input.UserObjectId, action, lockToken);
                await operationStore.UpdateStateAsync(input.OperationId, "Succeeded", "Completed", null, lockToken);
                return true;
            }, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            var category = exception is OperationRejectedException rejected
                ? rejected.Category
                : "MembershipOperationFailed";
            logger.LogError(exception, "Membership operation {OperationId} failed in category {Category}.", input.OperationId, category);
            await operationStore.UpdateStateAsync(
                input.OperationId, "Failed", "Failed", category, CancellationToken.None);
            throw;
        }
    }
}

public static class ProvisionOperationOrchestrator
{
    [Function(nameof(ProvisionOperationOrchestrator))]
    public static async Task Run(
        [OrchestrationTrigger] TaskOrchestrationContext context)
    {
        var input = context.GetInput<ProvisionOperationInput>()
            ?? throw new InvalidOperationException("Provision orchestration input is required.");
        var deadline = context.CurrentUtcDateTime.AddMinutes(input.ScheduleProvisioningTimeoutMinutes);
        while (!await context.CallActivityAsync<bool>(nameof(ReconcileBranchActivity), input))
        {
            if (context.CurrentUtcDateTime >= deadline)
            {
                await context.CallActivityAsync(nameof(FailBranchReconciliationActivity), input);
                return;
            }

            await context.CreateTimer(
                context.CurrentUtcDateTime.AddSeconds(input.SchedulePollingIntervalSeconds),
                CancellationToken.None);
        }
    }

    [Function(nameof(ReconcileBranchActivity))]
    public static async Task<bool> ReconcileBranchActivity(
        [ActivityTrigger] ProvisionOperationInput input,
        IBranchMembershipStore branchStore,
        IBranchAdministrationAuthorization authorization,
        IBranchProvisioner provisioner,
        IBranchLifecycleStore lifecycleStore,
        IBranchOperationLock operationLock,
        IOperationStore operationStore,
        ILogger<ReconcileBranchActivity> logger,
        CancellationToken cancellationToken)
    {
        var previousState = Enum.TryParse<BranchLifecycleState>(
            input.PreviousLifecycleState, false, out var parsedState)
                ? parsedState
                : throw new InvalidOperationException("The branch lifecycle state in the operation input is invalid.");
        var lifecycleStarted = false;
        var branchId = BranchId.Create(input.BranchId);
        try
        {
            await operationStore.UpdateStateAsync(input.OperationId, "Running", "Authorizing", null, cancellationToken);
            var principalId = PrincipalId.Create(input.RequestedBy);
            var branch = await branchStore.GetBranchAsync(branchId, cancellationToken)
                ?? throw new OperationRejectedException("BranchUnavailable");
            if (!await authorization.CanAdministerBranchAsync(
                    principalId, input.SecurityGroupIds, branch.Branch, cancellationToken))
            {
                throw new OperationRejectedException("NotAuthorized");
            }

            await operationLock.ExecuteAsync(input.BranchId, async lockToken =>
            {
                var current = await branchStore.GetBranchAsync(branchId, lockToken)
                    ?? throw new OperationRejectedException("BranchUnavailable");
                if (!await authorization.CanAdministerBranchAsync(
                        principalId, input.SecurityGroupIds, current.Branch, lockToken))
                {
                    throw new OperationRejectedException("NotAuthorized");
                }

                await operationStore.UpdateStateAsync(input.OperationId, "Running", "ReconcilingResources", null, lockToken);
                await lifecycleStore.MarkProvisioningAsync(branchId, input.OperationId, lockToken);
                lifecycleStarted = true;
                var result = await provisioner.ProvisionAsync(current, lockToken);
                if (!result.ScheduleReady)
                {
                    await operationStore.UpdateStateAsync(
                        input.OperationId, "Running", "WaitingForGraphResourceProvisioning", null, lockToken);
                    return false;
                }

                await lifecycleStore.MarkActiveAsync(branchId, result.TeamId, input.OperationId, lockToken);
                await operationStore.UpdateStateAsync(input.OperationId, "Succeeded", "Completed", null, lockToken);
                return true;
            }, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            var category = exception is OperationRejectedException rejected
                ? rejected.Category
                : "BranchReconciliationFailed";
            logger.LogError(exception, "Branch reconciliation {OperationId} failed in category {Category}.", input.OperationId, category);
            if (lifecycleStarted)
            {
                await lifecycleStore.MarkFailedAsync(
                    branchId, input.OperationId, previousState, category, CancellationToken.None);
            }
            await operationStore.UpdateStateAsync(
                input.OperationId, "Failed", "Failed", category, CancellationToken.None);
            throw;
        }
    }

    [Function(nameof(FailBranchReconciliationActivity))]
    public static async Task FailBranchReconciliationActivity(
        [ActivityTrigger] ProvisionOperationInput input,
        IBranchLifecycleStore lifecycleStore,
        IOperationStore operationStore,
        ILogger<FailBranchReconciliationActivity> logger,
        CancellationToken cancellationToken)
    {
        var branchId = BranchId.Create(input.BranchId);
        var previousState = Enum.TryParse<BranchLifecycleState>(
            input.PreviousLifecycleState, false, out var parsedState)
                ? parsedState
                : throw new InvalidOperationException("The branch lifecycle state in the operation input is invalid.");
        const string category = "GraphResourceProvisioningTimeout";
        logger.LogError(
            "Branch reconciliation {OperationId} did not observe ready Graph resources within the configured timeout.",
            input.OperationId);
        await lifecycleStore.MarkFailedAsync(
            branchId, input.OperationId, previousState, category, cancellationToken);
        await operationStore.UpdateStateAsync(
            input.OperationId, "Failed", "Failed", category, cancellationToken);
    }
}

internal sealed class OperationRejectedException(string category) : Exception(category)
{
    public string Category { get; } = category;
}
