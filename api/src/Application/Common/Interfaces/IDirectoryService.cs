using api.Application.Common.Models;

namespace api.Application.Common.Interfaces;

public interface IDirectoryService
{
    Task<IReadOnlyList<DirectoryUser>> SearchUsersAsync(
        string searchTerm, CancellationToken cancellationToken);
}
