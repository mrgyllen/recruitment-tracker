using api.Application.Common.Models;

namespace api.Application.Features.Team.Queries.SearchDirectory;

public record DirectoryUserDto
{
    public Guid Id { get; init; }
    public string DisplayName { get; init; } = null!;
    public string Email { get; init; } = null!;

    public static DirectoryUserDto From(DirectoryUser user) =>
        new()
        {
            Id = user.Id,
            DisplayName = user.DisplayName,
            Email = user.Email,
        };
}
