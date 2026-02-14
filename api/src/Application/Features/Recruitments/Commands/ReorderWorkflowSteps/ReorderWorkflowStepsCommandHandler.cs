using api.Application.Common.Interfaces;
using api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using NotFoundException = api.Application.Common.Exceptions.NotFoundException;

namespace api.Application.Features.Recruitments.Commands.ReorderWorkflowSteps;

public class ReorderWorkflowStepsCommandHandler(
    IApplicationDbContext dbContext)
    : IRequestHandler<ReorderWorkflowStepsCommand>
{
    public async Task Handle(
        ReorderWorkflowStepsCommand request, CancellationToken cancellationToken)
    {
        var recruitment = await dbContext.Recruitments
            .Include(r => r.Steps)
            .FirstOrDefaultAsync(r => r.Id == request.RecruitmentId, cancellationToken)
            ?? throw new NotFoundException(nameof(Recruitment), request.RecruitmentId);

        var reordering = request.Steps
            .Select(s => (s.StepId, s.Order))
            .ToList();

        recruitment.ReorderSteps(reordering);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
