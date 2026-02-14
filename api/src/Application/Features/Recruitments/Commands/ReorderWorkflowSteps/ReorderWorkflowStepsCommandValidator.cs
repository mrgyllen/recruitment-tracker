namespace api.Application.Features.Recruitments.Commands.ReorderWorkflowSteps;

public class ReorderWorkflowStepsCommandValidator : AbstractValidator<ReorderWorkflowStepsCommand>
{
    public ReorderWorkflowStepsCommandValidator()
    {
        RuleFor(x => x.RecruitmentId).NotEmpty();
        RuleFor(x => x.Steps).NotEmpty().WithMessage("Steps list cannot be empty.");
        RuleFor(x => x.Steps)
            .Must(steps =>
            {
                var orders = steps.Select(s => s.Order).OrderBy(o => o).ToList();
                return orders.SequenceEqual(Enumerable.Range(1, steps.Count));
            })
            .When(x => x.Steps.Count > 0)
            .WithMessage("Step orders must be contiguous starting from 1.");
    }
}
