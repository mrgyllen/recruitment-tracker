using System.Threading.Channels;
using api.Application.Common.Interfaces;
using api.Application.Common.Models;
using api.Domain.Entities;
using api.Domain.Enums;
using ForbiddenAccessException = api.Application.Common.Exceptions.ForbiddenAccessException;
using NotFoundException = api.Application.Common.Exceptions.NotFoundException;

namespace api.Application.Features.Import.Commands.StartImport;

public class StartImportCommandHandler(
    IApplicationDbContext dbContext,
    ITenantContext tenantContext,
    ChannelWriter<ImportRequest> channelWriter)
    : IRequestHandler<StartImportCommand, StartImportResponse>
{
    public async Task<StartImportResponse> Handle(
        StartImportCommand request,
        CancellationToken cancellationToken)
    {
        var recruitment = await dbContext.Recruitments
            .Include(r => r.Members)
            .FirstOrDefaultAsync(r => r.Id == request.RecruitmentId, cancellationToken)
            ?? throw new NotFoundException(nameof(Recruitment), request.RecruitmentId);

        var userId = tenantContext.UserGuid;
        if (userId is null || !recruitment.Members.Any(m => m.UserId == userId))
        {
            throw new ForbiddenAccessException();
        }

        if (recruitment.Status == RecruitmentStatus.Closed)
        {
            throw new Domain.Exceptions.RecruitmentClosedException(recruitment.Id);
        }

        var session = ImportSession.Create(request.RecruitmentId, userId.Value, request.FileName);
        dbContext.ImportSessions.Add(session);
        await dbContext.SaveChangesAsync(cancellationToken);

        var isPdf = request.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase);

        await channelWriter.WriteAsync(
            new ImportRequest(
                session.Id,
                request.RecruitmentId,
                isPdf ? [] : request.FileContent,
                userId.Value,
                isPdf ? request.FileContent : null),
            cancellationToken);

        return new StartImportResponse(
            session.Id,
            $"/api/import-sessions/{session.Id}");
    }
}
