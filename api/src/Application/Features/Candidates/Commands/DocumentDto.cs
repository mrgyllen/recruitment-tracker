using api.Domain.Entities;

namespace api.Application.Features.Candidates.Commands;

public record DocumentDto
{
    public Guid Id { get; init; }
    public Guid CandidateId { get; init; }
    public string DocumentType { get; init; } = string.Empty;
    public string BlobStorageUrl { get; init; } = string.Empty;
    public DateTimeOffset UploadedAt { get; init; }

    public static DocumentDto From(CandidateDocument entity) => new()
    {
        Id = entity.Id,
        CandidateId = entity.CandidateId,
        DocumentType = entity.DocumentType,
        BlobStorageUrl = entity.BlobStorageUrl,
        UploadedAt = entity.UploadedAt,
    };
}
