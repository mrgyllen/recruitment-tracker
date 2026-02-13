using api.Application.Common.Interfaces;

namespace api.Infrastructure.Identity;

public class TenantContext : ITenantContext
{
    private readonly IUser _user;

    public TenantContext(IUser user)
    {
        _user = user;
    }

    public string? UserId => _user.Id;
    public Guid? UserGuid => Guid.TryParse(_user.Id, out var guid) ? guid : null;
    public Guid? RecruitmentId { get; set; }
    public bool IsServiceContext { get; set; }
}
