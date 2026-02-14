using api.Application.Common.Interfaces;
using api.Application.Features.Recruitments.Queries.GetRecruitmentById;
using api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using ForbiddenAccessException = api.Application.Common.Exceptions.ForbiddenAccessException;
using NotFoundException = api.Application.Common.Exceptions.NotFoundException;

namespace api.Application.Features.Recruitments.Commands.AddWorkflowStep;

public class AddWorkflowStepCommandHandler(
    IApplicationDbContext dbContext,
    ITenantContext tenantContext)
    : IRequestHandler<AddWorkflowStepCommand, WorkflowStepDetailDto>
{
    public async Task<WorkflowStepDetailDto> Handle(
        AddWorkflowStepCommand request, CancellationToken cancellationToken)
    {
        var recruitment = await dbContext.Recruitments
            .Include(r => r.Steps)
            .Include(r => r.Members)
            .FirstOrDefaultAsync(r => r.Id == request.RecruitmentId, cancellationToken)
            ?? throw new NotFoundException(nameof(Recruitment), request.RecruitmentId);

        var userId = tenantContext.UserGuid;
        if (userId is null || !recruitment.Members.Any(m => m.UserId == userId))
        {
            throw new ForbiddenAccessException();
        }

        recruitment.AddStep(request.Name, request.Order);

        await dbContext.SaveChangesAsync(cancellationToken);

        var newStep = recruitment.Steps.First(s => s.Name == request.Name);
        return new WorkflowStepDetailDto
        {
            Id = newStep.Id,
            Name = newStep.Name,
            Order = newStep.Order,
        };
    }
}
