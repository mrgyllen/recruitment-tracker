using api.Domain.Entities;

namespace api.Application.Features.Recruitments.Queries.GetRecruitmentById;

public record RecruitmentDetailDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = null!;
    public string? Description { get; init; }
    public string? JobRequisitionId { get; init; }
    public string Status { get; init; } = null!;
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? ClosedAt { get; init; }
    public Guid CreatedByUserId { get; init; }
    public List<WorkflowStepDetailDto> Steps { get; init; } = [];
    public List<MemberDetailDto> Members { get; init; } = [];

    public static RecruitmentDetailDto From(Recruitment entity) => new()
    {
        Id = entity.Id,
        Title = entity.Title,
        Description = entity.Description,
        JobRequisitionId = entity.JobRequisitionId,
        Status = entity.Status.ToString(),
        CreatedAt = entity.CreatedAt,
        ClosedAt = entity.ClosedAt,
        CreatedByUserId = entity.CreatedByUserId,
        Steps = entity.Steps
            .OrderBy(s => s.Order)
            .Select(s => new WorkflowStepDetailDto
            {
                Id = s.Id,
                Name = s.Name,
                Order = s.Order,
            })
            .ToList(),
        Members = entity.Members
            .Select(m => new MemberDetailDto
            {
                Id = m.Id,
                UserId = m.UserId,
                Role = m.Role,
            })
            .ToList(),
    };
}

public record WorkflowStepDetailDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = null!;
    public int Order { get; init; }
}

public record MemberDetailDto
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public string Role { get; init; } = null!;
}
