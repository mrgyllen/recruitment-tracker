namespace api.Application.Features.Recruitments.Commands.CreateRecruitment;

public class CreateRecruitmentCommandValidator : AbstractValidator<CreateRecruitmentCommand>
{
    public CreateRecruitmentCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(200);

        RuleFor(x => x.Description)
            .MaximumLength(2000);

        RuleFor(x => x.JobRequisitionId)
            .MaximumLength(100);

        RuleFor(x => x.Steps)
            .Must(steps => steps
                .Select(s => s.Name.ToLowerInvariant())
                .Distinct()
                .Count() == steps.Count)
            .When(x => x.Steps.Count > 0)
            .WithMessage("Workflow step names must be unique.");

        RuleFor(x => x.Steps)
            .Must(steps =>
            {
                var orders = steps.Select(s => s.Order).OrderBy(o => o).ToList();
                return orders.SequenceEqual(Enumerable.Range(1, steps.Count));
            })
            .When(x => x.Steps.Count > 0)
            .WithMessage("Workflow step orders must be contiguous starting from 1.");

        RuleForEach(x => x.Steps).ChildRules(step =>
        {
            step.RuleFor(s => s.Name)
                .NotEmpty().WithMessage("Step name is required.")
                .MaximumLength(100);

            step.RuleFor(s => s.Order)
                .GreaterThan(0);
        });
    }
}
