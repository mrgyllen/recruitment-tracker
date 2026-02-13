using api.Application.Common.Interfaces;
using api.Domain.Entities;

namespace api.Application.Features.Recruitments.Commands.CreateRecruitment;

public class CreateRecruitmentCommandHandler : IRequestHandler<CreateRecruitmentCommand, Guid>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IUser _user;

    public CreateRecruitmentCommandHandler(IApplicationDbContext dbContext, IUser user)
    {
        _dbContext = dbContext;
        _user = user;
    }

    public async Task<Guid> Handle(CreateRecruitmentCommand request, CancellationToken cancellationToken)
    {
        var recruitment = Recruitment.Create(
            request.Title,
            request.Description,
            Guid.Parse(_user.Id!),
            request.JobRequisitionId);

        foreach (var step in request.Steps.OrderBy(s => s.Order))
        {
            recruitment.AddStep(step.Name, step.Order);
        }

        _dbContext.Recruitments.Add(recruitment);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return recruitment.Id;
    }
}
