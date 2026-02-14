using api.Application.Common.Interfaces;
using api.Application.Common.Models;

namespace api.Infrastructure.Identity;

public class DevDirectoryService : IDirectoryService
{
    private static readonly List<DirectoryUser> DevUsers =
    [
        new(Guid.Parse("11111111-1111-1111-1111-111111111111"), "Dev User A", "usera@dev.local"),
        new(Guid.Parse("22222222-2222-2222-2222-222222222222"), "Dev User B", "userb@dev.local"),
        new(Guid.Parse("33333333-3333-3333-3333-333333333333"), "Dev Admin", "admin@dev.local"),
        new(Guid.Parse("44444444-4444-4444-4444-444444444444"), "Erik Leader", "erik@dev.local"),
        new(Guid.Parse("55555555-5555-5555-5555-555555555555"), "Sara Specialist", "sara@dev.local"),
    ];

    public Task<IReadOnlyList<DirectoryUser>> SearchUsersAsync(
        string searchTerm, CancellationToken cancellationToken)
    {
        var results = DevUsers
            .Where(u => u.DisplayName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
                     || u.Email.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
            .ToList();

        return Task.FromResult<IReadOnlyList<DirectoryUser>>(results);
    }
}
