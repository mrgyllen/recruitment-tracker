namespace api.Application.Features.Screening.Queries.GetCandidateOutcomeHistory;

public record GetCandidateOutcomeHistoryQuery(
    Guid RecruitmentId,
    Guid CandidateId
) : IRequest<List<OutcomeHistoryDto>>;
