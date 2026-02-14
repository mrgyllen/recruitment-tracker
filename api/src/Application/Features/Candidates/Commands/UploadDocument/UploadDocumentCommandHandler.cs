using api.Application.Common.Interfaces;
using api.Domain.Entities;
using api.Domain.Enums;
using api.Domain.Exceptions;
using ForbiddenAccessException = api.Application.Common.Exceptions.ForbiddenAccessException;
using NotFoundException = api.Application.Common.Exceptions.NotFoundException;

namespace api.Application.Features.Candidates.Commands.UploadDocument;

public class UploadDocumentCommandHandler(
    IApplicationDbContext dbContext,
    ITenantContext tenantContext,
    IBlobStorageService blobStorage)
    : IRequestHandler<UploadDocumentCommand, DocumentDto>
{
    private const string ContainerName = "documents";

    public async Task<DocumentDto> Handle(
        UploadDocumentCommand request,
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

        var candidate = await dbContext.Candidates
            .Include(c => c.Documents)
            .FirstOrDefaultAsync(c => c.Id == request.CandidateId
                && c.RecruitmentId == request.RecruitmentId, cancellationToken)
            ?? throw new NotFoundException(nameof(Candidate), request.CandidateId);

        // Upload to blob storage
        var docId = Guid.NewGuid();
        var blobName = $"{request.RecruitmentId}/cvs/{docId}.pdf";
        await blobStorage.UploadAsync(ContainerName, blobName,
            request.FileStream, "application/pdf", cancellationToken);

        var oldBlobUrl = candidate.ReplaceDocument("CV", blobName);

        if (oldBlobUrl is not null)
        {
            await blobStorage.DeleteAsync(ContainerName, oldBlobUrl, cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var newDoc = candidate.Documents.First(d => d.BlobStorageUrl == blobName);
        return DocumentDto.From(newDoc);
    }
}
