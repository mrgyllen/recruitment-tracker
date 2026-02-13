namespace api.Application.Common.Interfaces;

public interface ITenantContext
{
    string? UserId { get; }
    Guid? UserGuid { get; }
    Guid? RecruitmentId { get; set; }
    bool IsServiceContext { get; set; }
}
