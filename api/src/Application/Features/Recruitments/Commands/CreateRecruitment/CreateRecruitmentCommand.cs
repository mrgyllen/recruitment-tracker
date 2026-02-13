namespace api.Application.Features.Recruitments.Commands.CreateRecruitment;

public record CreateRecruitmentCommand : IRequest<Guid>
{
    public string Title { get; init; } = null!;
    public string? Description { get; init; }
    public string? JobRequisitionId { get; init; }
    public List<WorkflowStepDto> Steps { get; init; } = [];
}

public record WorkflowStepDto
{
    public string Name { get; init; } = null!;
    public int Order { get; init; }
}
