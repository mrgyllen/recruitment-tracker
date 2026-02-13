using api.Application.Common.Interfaces;
using api.Domain.Entities;

namespace api.Application.Common.Behaviours;

public class AuditBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ITenantContext _tenantContext;

    public AuditBehaviour(IApplicationDbContext dbContext, ITenantContext tenantContext)
    {
        _dbContext = dbContext;
        _tenantContext = tenantContext;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var response = await next();

        // Only audit commands (convention: class name ends with "Command")
        if (!IsCommand(request))
        {
            return response;
        }

        var userId = Guid.TryParse(_tenantContext.UserId, out var parsed) ? parsed : Guid.Empty;

        var auditEntry = AuditEntry.Create(
            recruitmentId: Guid.Empty, // Set by specific command handlers in future stories
            entityId: null,
            entityType: typeof(TRequest).Name.Replace("Command", ""),
            actionType: "Command",
            performedBy: userId,
            context: null); // No PII -- context populated by specific handlers

        _dbContext.AuditEntries.Add(auditEntry);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return response;
    }

    private static bool IsCommand(TRequest request)
    {
        return typeof(TRequest).Name.EndsWith("Command", StringComparison.Ordinal);
    }
}
