namespace api.Application.Features.Recruitments.Commands.CloseRecruitment;

public class CloseRecruitmentCommandValidator : AbstractValidator<CloseRecruitmentCommand>
{
    public CloseRecruitmentCommandValidator()
    {
        RuleFor(x => x.RecruitmentId).NotEmpty();
    }
}
