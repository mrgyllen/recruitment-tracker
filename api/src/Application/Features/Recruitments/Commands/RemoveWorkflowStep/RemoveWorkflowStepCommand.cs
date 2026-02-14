namespace api.Application.Features.Recruitments.Commands.RemoveWorkflowStep;

public record RemoveWorkflowStepCommand : IRequest
{
    public Guid RecruitmentId { get; init; }
    public Guid StepId { get; init; }
}
