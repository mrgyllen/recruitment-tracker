using api.Domain.Common;

namespace api.Domain.Entities;

public class WorkflowStep : GuidEntity
{
    public Guid RecruitmentId { get; private set; }
    public string Name { get; private set; } = null!;
    public int Order { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private WorkflowStep() { } // EF Core

    internal static WorkflowStep Create(Guid recruitmentId, string name, int order)
    {
        return new WorkflowStep
        {
            RecruitmentId = recruitmentId,
            Name = name,
            Order = order,
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }

    internal void UpdateOrder(int newOrder)
    {
        Order = newOrder;
    }
}
