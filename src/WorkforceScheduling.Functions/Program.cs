using Azure.Core;
using Azure.Identity;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using WorkforceScheduling.Domain.Authorization;
using WorkforceScheduling.Domain.Branches;
using WorkforceScheduling.Functions.Authentication;
using WorkforceScheduling.Functions.Configuration;
using WorkforceScheduling.Functions.Dataverse;
using WorkforceScheduling.Functions.Graph;
using WorkforceScheduling.Functions.Locks;
using WorkforceScheduling.Functions.Operations;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((context, services) =>
    {
        services.AddOptions<WorkforceOptions>()
            .Bind(context.Configuration.GetSection(WorkforceOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddOptions<EntraAuthenticationOptions>()
            .Bind(context.Configuration.GetSection(EntraAuthenticationOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<WorkforceOptions>, WorkforceOptionsValidation>();
        services.AddSingleton<IValidateOptions<EntraAuthenticationOptions>, EntraAuthenticationOptionsValidation>();
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();
        services.AddSingleton<TokenCredential>(provider =>
        {
            var clientId = provider.GetRequiredService<IOptions<WorkforceOptions>>()
                .Value.ManagedIdentityClientId;
            return string.IsNullOrWhiteSpace(clientId)
                ? new DefaultAzureCredential()
                : new ManagedIdentityCredential(clientId);
        });

        services.AddHttpClient("Dataverse");
        services.AddHttpClient("MicrosoftGraph", client =>
        {
            client.BaseAddress = new Uri("https://graph.microsoft.com/v1.0/");
        });

        services.AddSingleton<IEntraTokenValidator, EntraTokenValidator>();
        services.AddSingleton<IDataverseClient, DataverseClient>();
        services.AddSingleton<IBranchMembershipStore, DataverseBranchMembershipStore>();
        services.AddSingleton<IBranchLifecycleStore, DataverseBranchLifecycleStore>();
        services.AddSingleton<IOperationStore, DataverseOperationStore>();
        services.AddSingleton<IRoleScopeResolver, TokenRoleScopeResolver>();
        services.AddSingleton<IMembershipAuthorization, MembershipAuthorization>();
        services.AddSingleton<IBranchAdministrationAuthorization, BranchAdministrationAuthorization>();
        services.AddSingleton<IGraphClient, GraphClient>();
        services.AddSingleton<IBranchGroupMembership, GraphBranchGroupMembership>();
        services.AddSingleton<IUserDirectory, GraphUserDirectory>();
        services.AddSingleton<IBranchProvisioner, GraphBranchProvisioner>();
        services.AddSingleton<IBranchOperationLock, BlobBranchOperationLock>();
    })
    .Build();
host.Run();
