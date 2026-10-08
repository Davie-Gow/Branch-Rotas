namespace WorkforceScheduling.Domain.Authorization;

/// <summary>Searches enabled internal Entra user accounts using minimal profile data.</summary>
public interface IUserDirectory
{
    Task<DirectoryUser?> GetEnabledInternalUserAsync(
        string userObjectId,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<DirectoryUser>> SearchEnabledUsersAsync(
        string query,
        int limit,
        CancellationToken cancellationToken);
}

/// <summary>Minimal profile returned to the authorized branch membership picker.</summary>
public sealed record DirectoryUser(string ObjectId, string DisplayName, string? Mail, string? UserPrincipalName);
