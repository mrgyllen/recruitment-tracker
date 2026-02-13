using api.Domain.Common;

namespace api.Domain.Entities;

public class AuditEntry : GuidEntity
{
    public Guid RecruitmentId { get; private set; }
    public Guid? EntityId { get; private set; }
    public string EntityType { get; private set; } = null!;
    public string ActionType { get; private set; } = null!;
    public Guid PerformedBy { get; private set; }
    public DateTimeOffset PerformedAt { get; private set; }
    public string? Context { get; private set; }

    private AuditEntry() { } // EF Core

    public static AuditEntry Create(
        Guid recruitmentId,
        Guid? entityId,
        string entityType,
        string actionType,
        Guid performedBy,
        string? context)
    {
        return new AuditEntry
        {
            RecruitmentId = recruitmentId,
            EntityId = entityId,
            EntityType = entityType,
            ActionType = actionType,
            PerformedBy = performedBy,
            PerformedAt = DateTimeOffset.UtcNow,
            Context = context,
        };
    }
}
