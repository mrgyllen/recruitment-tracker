namespace api.Application.Common.Models;

public record PdfSplitResult(
    bool Success,
    IReadOnlyList<PdfSplitEntry> Entries,
    string? ErrorMessage);

public record PdfSplitEntry(
    string CandidateName,
    string? WorkdayCandidateId,
    int StartPage,
    int EndPage,
    byte[]? PdfBytes,
    string? ErrorMessage);

public record PdfSplitProgress(
    int TotalCandidates,
    int CompletedCandidates,
    string? CurrentCandidateName);
