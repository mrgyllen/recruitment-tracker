namespace api.Application.Features.Recruitments.Queries.GetRecruitmentById;

public record GetRecruitmentByIdQuery : IRequest<RecruitmentDetailDto>
{
    public Guid Id { get; init; }
}
