using api.Application.Common.Interfaces;
using api.Domain.Entities;
using api.Domain.Enums;
using api.Domain.Exceptions;
using ForbiddenAccessException = api.Application.Common.Exceptions.ForbiddenAccessException;
using NotFoundException = api.Application.Common.Exceptions.NotFoundException;

namespace api.Application.Features.Candidates.Commands.AssignDocument;

public class AssignDocumentCommandHandler(
    IApplicationDbContext dbContext,
    ITenantContext tenantContext,
    IBlobStorageService blobStorage)
    : IRequestHandler<AssignDocumentCommand, DocumentDto>
{
    private const string ContainerName = "documents";

    public async Task<DocumentDto> Handle(
        AssignDocumentCommand request,
        CancellationToken cancellationToken)
    {
        var recruitment = await dbContext.Recruitments
            .Include(r => r.Members)
            .FirstOrDefaultAsync(r => r.Id == request.RecruitmentId, cancellationToken)
            ?? throw new NotFoundException(nameof(Recruitment), request.RecruitmentId);

        var userId = tenantContext.UserGuid;
        if (userId is null || !recruitment.Members.Any(m => m.UserId == userId))
            throw new ForbiddenAccessException();

        if (recruitment.Status == RecruitmentStatus.Closed)
            throw new RecruitmentClosedException(recruitment.Id);

        if (!blobStorage.VerifyBlobOwnership(ContainerName, request.DocumentBlobUrl, request.RecruitmentId))
            throw new ForbiddenAccessException();

        var candidate = await dbContext.Candidates
            .Include(c => c.Documents)
            .FirstOrDefaultAsync(c => c.Id == request.CandidateId
                && c.RecruitmentId == request.RecruitmentId, cancellationToken)
            ?? throw new NotFoundException(nameof(Candidate), request.CandidateId);

        var oldBlobUrl = candidate.ReplaceDocument("CV", request.DocumentBlobUrl);

        if (oldBlobUrl is not null)
        {
            await blobStorage.DeleteAsync(ContainerName, oldBlobUrl, cancellationToken);
        }

        // Update ImportDocument status if from an import session
        if (request.ImportSessionId.HasValue)
        {
            var session = await dbContext.ImportSessions
                .Include(s => s.ImportDocuments)
                .FirstOrDefaultAsync(s => s.Id == request.ImportSessionId.Value, cancellationToken);

            var importDoc = session?.ImportDocuments
                .FirstOrDefault(d => d.BlobStorageUrl == request.DocumentBlobUrl);

            if (importDoc is not null)
            {
                session!.UpdateImportDocumentMatch(
                    importDoc.Id, request.CandidateId, ImportDocumentMatchStatus.ManuallyAssigned);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var newDoc = candidate.Documents.First(d =>
            d.BlobStorageUrl == request.DocumentBlobUrl);
        return DocumentDto.From(newDoc);
    }
}
