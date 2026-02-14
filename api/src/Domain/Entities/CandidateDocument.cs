using api.Domain.Common;
using api.Domain.Enums;

namespace api.Domain.Entities;

public class CandidateDocument : GuidEntity
{
    public Guid CandidateId { get; private set; }
    public string DocumentType { get; private set; } = null!;
    public string BlobStorageUrl { get; private set; } = null!;
    public DateTimeOffset UploadedAt { get; private set; }
    public string? WorkdayCandidateId { get; private set; }
    public DocumentSource DocumentSource { get; private set; }

    private CandidateDocument() { } // EF Core

    internal static CandidateDocument Create(
        Guid candidateId, string documentType, string blobStorageUrl,
        string? workdayCandidateId = null,
        DocumentSource documentSource = DocumentSource.IndividualUpload)
    {
        return new CandidateDocument
        {
            CandidateId = candidateId,
            DocumentType = documentType,
            BlobStorageUrl = blobStorageUrl,
            UploadedAt = DateTimeOffset.UtcNow,
            WorkdayCandidateId = workdayCandidateId,
            DocumentSource = documentSource,
        };
    }
}
