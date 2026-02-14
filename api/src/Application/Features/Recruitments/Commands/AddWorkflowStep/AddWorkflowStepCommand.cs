using api.Application.Features.Recruitments.Queries.GetRecruitmentById;

namespace api.Application.Features.Recruitments.Commands.AddWorkflowStep;

public record AddWorkflowStepCommand : IRequest<WorkflowStepDetailDto>
{
    public Guid RecruitmentId { get; init; }
    public string Name { get; init; } = null!;
    public int Order { get; init; }
}
