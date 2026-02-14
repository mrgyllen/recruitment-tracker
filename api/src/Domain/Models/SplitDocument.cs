namespace api.Domain.Models;

public record SplitDocument(
    string CandidateName,
    string BlobStorageUrl,
    string? WorkdayCandidateId);
