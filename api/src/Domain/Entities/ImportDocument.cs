using api.Domain.Common;
using api.Domain.Enums;

namespace api.Domain.Entities;

public class ImportDocument : GuidEntity
{
    public Guid ImportSessionId { get; private set; }
    public string CandidateName { get; private set; } = null!;
    public string BlobStorageUrl { get; private set; } = null!;
    public string? WorkdayCandidateId { get; private set; }
    public ImportDocumentMatchStatus MatchStatus { get; private set; }
    public Guid? MatchedCandidateId { get; private set; }

    private ImportDocument() { } // EF Core

    internal static ImportDocument Create(
        Guid importSessionId,
        string candidateName,
        string blobStorageUrl,
        string? workdayCandidateId)
    {
        return new ImportDocument
        {
            ImportSessionId = importSessionId,
            CandidateName = candidateName,
            BlobStorageUrl = blobStorageUrl,
            WorkdayCandidateId = workdayCandidateId,
            MatchStatus = ImportDocumentMatchStatus.Pending,
        };
    }
}
