using api.Application.Common.Interfaces;

namespace api.Web.Middleware;

public class TenantContextMiddleware
{
    private readonly RequestDelegate _next;

    public TenantContextMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext)
    {
        // ITenantContext.UserId is already populated via ICurrentUserService (lazy from DI).
        // This middleware ensures the context is resolved early in the pipeline.
        // No additional action needed -- the scoped DI resolution handles it.
        await _next(context);
    }
}
