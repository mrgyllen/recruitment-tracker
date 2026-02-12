using api.Application.Common.Interfaces;

namespace api.Infrastructure.Identity;

public class TenantContext : ITenantContext
{
    private readonly ICurrentUserService _currentUserService;

    public TenantContext(ICurrentUserService currentUserService)
    {
        _currentUserService = currentUserService;
    }

    public string? UserId => _currentUserService.UserId;
    public Guid? RecruitmentId { get; set; }
    public bool IsServiceContext { get; set; }
}
