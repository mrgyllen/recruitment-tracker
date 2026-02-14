using api.Application.Common.Interfaces;

namespace api.Application.Features.Team.Queries.SearchDirectory;

public class SearchDirectoryQueryHandler(IDirectoryService directoryService)
    : IRequestHandler<SearchDirectoryQuery, IReadOnlyList<DirectoryUserDto>>
{
    public async Task<IReadOnlyList<DirectoryUserDto>> Handle(
        SearchDirectoryQuery request, CancellationToken cancellationToken)
    {
        var users = await directoryService.SearchUsersAsync(request.SearchTerm, cancellationToken);
        return users.Select(DirectoryUserDto.From).ToList();
    }
}
