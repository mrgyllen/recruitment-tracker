namespace api.Application.Features.Recruitments.Commands.AddWorkflowStep;

public class AddWorkflowStepCommandValidator : AbstractValidator<AddWorkflowStepCommand>
{
    public AddWorkflowStepCommandValidator()
    {
        RuleFor(x => x.RecruitmentId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().WithMessage("Step name is required.").MaximumLength(100);
        RuleFor(x => x.Order).GreaterThan(0);
    }
}
