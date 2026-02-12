using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace api.Web.Configuration;

public static class DevelopmentAuthenticationConfiguration
{
    public const string SchemeName = "DevAuth";

    public static IServiceCollection AddDevelopmentAuthentication(
        this IServiceCollection services)
    {
        services.AddAuthentication(SchemeName)
            .AddScheme<AuthenticationSchemeOptions, DevelopmentAuthenticationHandler>(
                SchemeName, _ => { });

        return services;
    }
}

public class DevelopmentAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public DevelopmentAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var userId = Request.Headers["X-Dev-User-Id"].FirstOrDefault();

        if (string.IsNullOrEmpty(userId))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var userName = Request.Headers["X-Dev-User-Name"].FirstOrDefault() ?? "Dev User";

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Name, userName),
        };

        var identity = new ClaimsIdentity(claims, DevelopmentAuthenticationConfiguration.SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, DevelopmentAuthenticationConfiguration.SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
