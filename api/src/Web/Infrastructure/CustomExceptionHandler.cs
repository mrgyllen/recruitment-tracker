using api.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ForbiddenAccessException = api.Application.Common.Exceptions.ForbiddenAccessException;
using NotFoundException = api.Application.Common.Exceptions.NotFoundException;
using ValidationException = api.Application.Common.Exceptions.ValidationException;

namespace api.Web.Infrastructure;

public class CustomExceptionHandler : IExceptionHandler
{
    private readonly ILogger<CustomExceptionHandler> _logger;
    private readonly Dictionary<Type, Func<HttpContext, Exception, Task>> _exceptionHandlers;

    public CustomExceptionHandler(ILogger<CustomExceptionHandler> logger)
    {
        _logger = logger;
        // Register known exception types and handlers.
        _exceptionHandlers = new()
            {
                { typeof(ValidationException), HandleValidationException },
                { typeof(NotFoundException), HandleNotFoundException },
                { typeof(UnauthorizedAccessException), HandleUnauthorizedAccessException },
                { typeof(ForbiddenAccessException), HandleForbiddenAccessException },
                { typeof(RecruitmentClosedException), HandleRecruitmentClosedException },
                { typeof(StepHasOutcomesException), HandleStepHasOutcomesException },
                { typeof(DuplicateStepNameException), HandleDuplicateStepNameException },
                { typeof(DomainRuleViolationException), HandleDomainRuleViolationException },
                { typeof(DuplicateCandidateException), HandleDuplicateCandidateException },
            };
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var exceptionType = exception.GetType();

        if (_exceptionHandlers.ContainsKey(exceptionType))
        {
            await _exceptionHandlers[exceptionType].Invoke(httpContext, exception);
            return true;
        }

        return false;
    }

    private async Task HandleValidationException(HttpContext httpContext, Exception ex)
    {
        var exception = (ValidationException)ex;

        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;

        await httpContext.Response.WriteAsJsonAsync(new ValidationProblemDetails(exception.Errors)
        {
            Status = StatusCodes.Status400BadRequest,
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1"
        });
    }

    private async Task HandleNotFoundException(HttpContext httpContext, Exception ex)
    {
        _logger.LogWarning(ex, "Resource not found: {Message}", ex.Message);

        httpContext.Response.StatusCode = StatusCodes.Status404NotFound;

        await httpContext.Response.WriteAsJsonAsync(new ProblemDetails()
        {
            Status = StatusCodes.Status404NotFound,
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4",
            Title = "The specified resource was not found.",
        });
    }

    private async Task HandleUnauthorizedAccessException(HttpContext httpContext, Exception ex)
    {
        httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;

        await httpContext.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = StatusCodes.Status401Unauthorized,
            Title = "Unauthorized",
            Type = "https://tools.ietf.org/html/rfc7235#section-3.1"
        });
    }

    private async Task HandleForbiddenAccessException(HttpContext httpContext, Exception ex)
    {
        httpContext.Response.StatusCode = StatusCodes.Status403Forbidden;

        await httpContext.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = StatusCodes.Status403Forbidden,
            Title = "Forbidden",
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.3"
        });
    }

    private async Task HandleRecruitmentClosedException(HttpContext httpContext, Exception ex)
    {
        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;

        await httpContext.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Recruitment is closed",
            Detail = ex.Message,
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1"
        });
    }

    private async Task HandleStepHasOutcomesException(HttpContext httpContext, Exception ex)
    {
        httpContext.Response.StatusCode = StatusCodes.Status409Conflict;

        await httpContext.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = StatusCodes.Status409Conflict,
            Title = "Cannot remove -- outcomes recorded at this step",
            Detail = ex.Message,
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.8"
        });
    }

    private async Task HandleDuplicateStepNameException(HttpContext httpContext, Exception ex)
    {
        httpContext.Response.StatusCode = StatusCodes.Status409Conflict;

        await httpContext.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = StatusCodes.Status409Conflict,
            Title = "Duplicate step name",
            Detail = ex.Message,
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.8"
        });
    }

    private async Task HandleDomainRuleViolationException(HttpContext httpContext, Exception ex)
    {
        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;

        await httpContext.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Domain rule violation",
            Detail = ex.Message,
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1"
        });
    }

    private async Task HandleDuplicateCandidateException(HttpContext httpContext, Exception ex)
    {
        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;

        await httpContext.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "A candidate with this email already exists in this recruitment",
            Detail = ex.Message,
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1"
        });
    }
}
