namespace api.Application.Features.Recruitments.Commands.UpdateRecruitment;

public record UpdateRecruitmentCommand : IRequest
{
    public Guid Id { get; init; }
    public string Title { get; init; } = null!;
    public string? Description { get; init; }
    public string? JobRequisitionId { get; init; }
}
