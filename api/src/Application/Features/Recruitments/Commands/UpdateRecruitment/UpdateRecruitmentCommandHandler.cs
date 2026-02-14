using api.Application.Common.Interfaces;
using api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using NotFoundException = api.Application.Common.Exceptions.NotFoundException;

namespace api.Application.Features.Recruitments.Commands.UpdateRecruitment;

public class UpdateRecruitmentCommandHandler(
    IApplicationDbContext dbContext)
    : IRequestHandler<UpdateRecruitmentCommand>
{
    public async Task Handle(UpdateRecruitmentCommand request, CancellationToken cancellationToken)
    {
        var recruitment = await dbContext.Recruitments
            .Include(r => r.Steps)
            .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Recruitment), request.Id);

        recruitment.UpdateDetails(request.Title, request.Description, request.JobRequisitionId);

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
