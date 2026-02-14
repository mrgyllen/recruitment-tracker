namespace api.Application.Features.Recruitments.Commands.CloseRecruitment;

public class CloseRecruitmentCommand : IRequest
{
    public Guid RecruitmentId { get; init; }
}
