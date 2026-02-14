using api.Application.Common.Interfaces;
using api.Application.Common.Models;

namespace api.Infrastructure.Identity;

public class EntraIdDirectoryService : IDirectoryService
{
    public Task<IReadOnlyList<DirectoryUser>> SearchUsersAsync(
        string searchTerm, CancellationToken cancellationToken)
    {
        // TODO: Implement Microsoft Graph API integration when Entra ID tenant is configured.
        // Requires User.Read.All or People.Read.All application permission.
        throw new NotImplementedException(
            "EntraIdDirectoryService requires Entra ID configuration. Use DevDirectoryService in development.");
    }
}
