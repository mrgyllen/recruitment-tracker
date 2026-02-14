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

    private void EnsureProcessing()
    {
        if (Status != ImportSessionStatus.Processing)
        {
            throw new InvalidWorkflowTransitionException(
                Status.ToString(), "target status");
        }
    }
}
