namespace api.Web.Middleware;

public class NoindexMiddleware
{
    private readonly RequestDelegate _next;

    public NoindexMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.Headers["X-Robots-Tag"] = "noindex";
        await _next(context);
    }
}
