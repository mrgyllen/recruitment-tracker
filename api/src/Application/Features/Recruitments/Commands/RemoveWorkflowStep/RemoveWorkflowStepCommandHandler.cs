using api.Application.Common.Interfaces;
using api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using NotFoundException = api.Application.Common.Exceptions.NotFoundException;

namespace api.Application.Features.Recruitments.Commands.RemoveWorkflowStep;

public class RemoveWorkflowStepCommandHandler(
    IApplicationDbContext dbContext)
    : IRequestHandler<RemoveWorkflowStepCommand>
{
    public async Task Handle(
        RemoveWorkflowStepCommand request, CancellationToken cancellationToken)
    {
        var recruitment = await dbContext.Recruitments
            .Include(r => r.Steps)
            .FirstOrDefaultAsync(r => r.Id == request.RecruitmentId, cancellationToken)
            ?? throw new NotFoundException(nameof(Recruitment), request.RecruitmentId);

        var hasOutcomes = await dbContext.Candidates
            .AnyAsync(c => c.Outcomes.Any(o => o.WorkflowStepId == request.StepId), cancellationToken);

        if (hasOutcomes)
            recruitment.MarkStepHasOutcomes(request.StepId);

        recruitment.RemoveStep(request.StepId);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
