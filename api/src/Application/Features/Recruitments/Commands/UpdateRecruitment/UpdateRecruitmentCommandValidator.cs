namespace api.Application.Features.Recruitments.Commands.UpdateRecruitment;

public class UpdateRecruitmentCommandValidator : AbstractValidator<UpdateRecruitmentCommand>
{
    public UpdateRecruitmentCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().WithMessage("Title is required.").MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.JobRequisitionId).MaximumLength(100);
    }
}
