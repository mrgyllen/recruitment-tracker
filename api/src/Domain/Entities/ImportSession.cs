using api.Domain.Common;
using api.Domain.Enums;
using api.Domain.Exceptions;

namespace api.Domain.Entities;

public class ImportSession : GuidEntity
{
    public Guid RecruitmentId { get; private set; }
    public ImportSessionStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public int TotalRows { get; private set; }
    public int SuccessfulRows { get; private set; }
    public int FailedRows { get; private set; }
    public string? FailureReason { get; private set; }
    public Guid CreatedByUserId { get; private set; }

    private ImportSession() { } // EF Core

    public static ImportSession Create(Guid recruitmentId, Guid createdByUserId)
    {
        return new ImportSession
        {
            RecruitmentId = recruitmentId,
            Status = ImportSessionStatus.Processing,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedByUserId = createdByUserId,
        };
    }

    public void MarkCompleted(int successCount, int failCount)
    {
        EnsureProcessing();

        Status = ImportSessionStatus.Completed;
        SuccessfulRows = successCount;
        FailedRows = failCount;
        TotalRows = successCount + failCount;
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
