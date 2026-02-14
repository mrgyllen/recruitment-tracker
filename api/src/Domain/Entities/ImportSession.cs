using api.Domain.Common;
using api.Domain.Enums;
using api.Domain.Exceptions;
using api.Domain.ValueObjects;

namespace api.Domain.Entities;

public class ImportSession : GuidEntity
{
    public Guid RecruitmentId { get; private set; }
    public ImportSessionStatus Status { get; private set; }
    public string SourceFileName { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public int TotalRows { get; private set; }
    public int CreatedCount { get; private set; }
    public int UpdatedCount { get; private set; }
    public int ErroredCount { get; private set; }
    public int FlaggedCount { get; private set; }
    public string? FailureReason { get; private set; }
    public Guid CreatedByUserId { get; private set; }

    private readonly List<ImportRowResult> _rowResults = new();
    public IReadOnlyCollection<ImportRowResult> RowResults => _rowResults.AsReadOnly();

    public int? PdfTotalCandidates { get; private set; }
    public int? PdfSplitCandidates { get; private set; }
    public int PdfSplitErrors { get; private set; }
    public string? OriginalBundleBlobUrl { get; private set; }

    private readonly List<ImportDocument> _importDocuments = new();
    public IReadOnlyCollection<ImportDocument> ImportDocuments => _importDocuments.AsReadOnly();

    private ImportSession() { } // EF Core

    public static ImportSession Create(Guid recruitmentId, Guid createdByUserId, string sourceFileName = "")
    {
        return new ImportSession
        {
            RecruitmentId = recruitmentId,
            Status = ImportSessionStatus.Processing,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedByUserId = createdByUserId,
            SourceFileName = sourceFileName,
        };
    }

    public void AddRowResult(ImportRowResult rowResult)
    {
        EnsureProcessing();
        _rowResults.Add(rowResult);
    }

    public void MarkCompleted(int created, int updated, int errored, int flagged)
    {
        EnsureProcessing();

        Status = ImportSessionStatus.Completed;
        CreatedCount = created;
        UpdatedCount = updated;
        ErroredCount = errored;
        FlaggedCount = flagged;
        TotalRows = created + updated + errored + flagged;
        CompletedAt = DateTimeOffset.UtcNow;
    }

    public void MarkFailed(string reason)
    {
        EnsureProcessing();

        Status = ImportSessionStatus.Failed;
        FailureReason = reason?.Length > 2000 ? reason[..2000] : reason;
        CompletedAt = DateTimeOffset.UtcNow;
    }

    public ImportRowResult ConfirmMatch(int rowIndex)
    {
        EnsureCompleted();
        ValidateRowIndex(rowIndex);

        var row = _rowResults[rowIndex];
        row.Confirm();
        return row;
    }

    public ImportRowResult RejectMatch(int rowIndex)
    {
        EnsureCompleted();
        ValidateRowIndex(rowIndex);

        var row = _rowResults[rowIndex];
        row.Reject();
        return row;
    }

    public void SetPdfSplitProgress(int total, int completed, int errors)
    {
        EnsureProcessing();
        PdfTotalCandidates = total;
        PdfSplitCandidates = completed;
        PdfSplitErrors = errors;
    }

    public void SetOriginalBundleUrl(string url)
    {
        OriginalBundleBlobUrl = url;
    }

    public void AddImportDocument(string candidateName, string blobStorageUrl, string? workdayCandidateId)
    {
        EnsureProcessing();
        _importDocuments.Add(ImportDocument.Create(Id, candidateName, blobStorageUrl, workdayCandidateId));
    }

    public void ClearImportDocuments()
    {
        _importDocuments.Clear();
    }

    public void UpdateImportDocumentMatch(Guid importDocumentId, Guid? candidateId, ImportDocumentMatchStatus status)
    {
        var doc = _importDocuments.FirstOrDefault(d => d.Id == importDocumentId)
            ?? throw new ArgumentException($"ImportDocument {importDocumentId} not found in session");

        switch (status)
        {
            case ImportDocumentMatchStatus.AutoMatched:
                doc.MarkAutoMatched(candidateId!.Value);
                break;
            case ImportDocumentMatchStatus.Unmatched:
                doc.MarkUnmatched();
                break;
            case ImportDocumentMatchStatus.ManuallyAssigned:
                doc.MarkManuallyAssigned(candidateId!.Value);
                break;
        }
    }

    private void EnsureProcessing()
    {
        if (Status != ImportSessionStatus.Processing)
        {
            throw new InvalidWorkflowTransitionException(
                Status.ToString(), "target status");
        }
    }

    private void EnsureCompleted()
    {
        if (Status != ImportSessionStatus.Completed)
        {
            throw new InvalidWorkflowTransitionException(
                Status.ToString(), "match resolution requires Completed status");
        }
    }

    private void ValidateRowIndex(int rowIndex)
    {
        if (rowIndex < 0 || rowIndex >= _rowResults.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(rowIndex));
        }
    }
}
