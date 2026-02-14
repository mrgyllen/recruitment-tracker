using api.Domain.Entities;
using api.Domain.ValueObjects;

namespace api.Application.Features.Import.Queries.GetImportSession;

public record ImportSessionDto
{
    public Guid Id { get; init; }
    public Guid RecruitmentId { get; init; }
    public string Status { get; init; } = string.Empty;
    public string SourceFileName { get; init; } = string.Empty;
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? CompletedAt { get; init; }
    public int TotalRows { get; init; }
    public int CreatedCount { get; init; }
    public int UpdatedCount { get; init; }
    public int ErroredCount { get; init; }
    public int FlaggedCount { get; init; }
    public string? FailureReason { get; init; }
    public int? PdfTotalCandidates { get; init; }
    public int? PdfSplitCandidates { get; init; }
    public int PdfSplitErrors { get; init; }
    public string? OriginalBundleBlobUrl { get; init; }
    public List<ImportRowResultDto> RowResults { get; init; } = new();
    public List<ImportDocumentDto> ImportDocuments { get; init; } = new();

    public static ImportSessionDto From(ImportSession entity) => new()
    {
        Id = entity.Id,
        RecruitmentId = entity.RecruitmentId,
        Status = entity.Status.ToString(),
        SourceFileName = entity.SourceFileName,
        CreatedAt = entity.CreatedAt,
        CompletedAt = entity.CompletedAt,
        TotalRows = entity.TotalRows,
        CreatedCount = entity.CreatedCount,
        UpdatedCount = entity.UpdatedCount,
        ErroredCount = entity.ErroredCount,
        FlaggedCount = entity.FlaggedCount,
        FailureReason = entity.FailureReason,
        PdfTotalCandidates = entity.PdfTotalCandidates,
        PdfSplitCandidates = entity.PdfSplitCandidates,
        PdfSplitErrors = entity.PdfSplitErrors,
        OriginalBundleBlobUrl = entity.OriginalBundleBlobUrl,
        RowResults = entity.RowResults.Select(ImportRowResultDto.From).ToList(),
        ImportDocuments = entity.ImportDocuments.Select(ImportDocumentDto.From).ToList(),
    };
}

public record ImportRowResultDto
{
    public int RowNumber { get; init; }
    public string? CandidateEmail { get; init; }
    public string Action { get; init; } = string.Empty;
    public string? ErrorMessage { get; init; }
    public string? Resolution { get; init; }

    public static ImportRowResultDto From(ImportRowResult row) => new()
    {
        RowNumber = row.RowNumber,
        CandidateEmail = row.CandidateEmail,
        Action = row.Action.ToString(),
        ErrorMessage = row.ErrorMessage,
        Resolution = row.Resolution,
    };
}

public record ImportDocumentDto
{
    public Guid Id { get; init; }
    public string CandidateName { get; init; } = string.Empty;
    public string BlobStorageUrl { get; init; } = string.Empty;
    public string? WorkdayCandidateId { get; init; }
    public string MatchStatus { get; init; } = string.Empty;
    public Guid? MatchedCandidateId { get; init; }

    public static ImportDocumentDto From(Domain.Entities.ImportDocument entity) => new()
    {
        Id = entity.Id,
        CandidateName = entity.CandidateName,
        BlobStorageUrl = entity.BlobStorageUrl,
        WorkdayCandidateId = entity.WorkdayCandidateId,
        MatchStatus = entity.MatchStatus.ToString(),
        MatchedCandidateId = entity.MatchedCandidateId,
    };
}
