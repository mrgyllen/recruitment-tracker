namespace api.Application.Features.Team.Queries.SearchDirectory;

public record SearchDirectoryQuery : IRequest<IReadOnlyList<DirectoryUserDto>>
{
    public string SearchTerm { get; init; } = null!;
}
