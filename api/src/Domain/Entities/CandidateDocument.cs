using api.Domain.Common;

namespace api.Domain.Entities;

public class CandidateDocument : GuidEntity
{
    public Guid CandidateId { get; private set; }
    public string DocumentType { get; private set; } = null!;
    public string BlobStorageUrl { get; private set; } = null!;
    public DateTimeOffset UploadedAt { get; private set; }

    private CandidateDocument() { } // EF Core

    internal static CandidateDocument Create(Guid candidateId, string documentType, string blobStorageUrl)
    {
        return new CandidateDocument
        {
            CandidateId = candidateId,
            DocumentType = documentType,
            BlobStorageUrl = blobStorageUrl,
            UploadedAt = DateTimeOffset.UtcNow,
        };
    }
}
