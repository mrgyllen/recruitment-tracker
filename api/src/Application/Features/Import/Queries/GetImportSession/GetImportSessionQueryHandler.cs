using api.Application.Common.Interfaces;
using api.Domain.Entities;
using NotFoundException = api.Application.Common.Exceptions.NotFoundException;

namespace api.Application.Features.Import.Queries.GetImportSession;

public class GetImportSessionQueryHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetImportSessionQuery, ImportSessionDto>
{
    public async Task<ImportSessionDto> Handle(
        GetImportSessionQuery request,
        CancellationToken cancellationToken)
    {
        var session = await dbContext.ImportSessions
            .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(ImportSession), request.Id);

        return ImportSessionDto.From(session);
    }
}
