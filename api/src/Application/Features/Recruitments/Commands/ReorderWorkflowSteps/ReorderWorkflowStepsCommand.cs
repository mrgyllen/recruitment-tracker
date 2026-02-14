namespace api.Application.Features.Recruitments.Commands.ReorderWorkflowSteps;

public record ReorderWorkflowStepsCommand : IRequest
{
    public Guid RecruitmentId { get; init; }
    public List<StepOrderDto> Steps { get; init; } = [];
}

public record StepOrderDto
{
    public Guid StepId { get; init; }
    public int Order { get; init; }
}
