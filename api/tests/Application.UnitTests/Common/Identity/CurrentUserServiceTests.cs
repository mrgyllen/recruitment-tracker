using System.Security.Claims;
using api.Infrastructure.Identity;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using NUnit.Framework;

namespace api.Application.UnitTests.Common.Identity;

[TestFixture]
public class CurrentUserServiceTests
{
    [Test]
    public void UserId_WhenClaimPresent_ReturnsClaimValue()
    {
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, "user-123") };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        httpContextAccessor.HttpContext.Returns(new DefaultHttpContext { User = principal });

        var service = new CurrentUserService(httpContextAccessor);

        service.UserId.Should().Be("user-123");
    }

    [Test]
    public void UserId_WhenNoHttpContext_ReturnsNull()
    {
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        httpContextAccessor.HttpContext.Returns((HttpContext?)null);

        var service = new CurrentUserService(httpContextAccessor);

        service.UserId.Should().BeNull();
    }

    [Test]
    public void UserId_WhenNoClaim_ReturnsNull()
    {
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        var identity = new ClaimsIdentity();
        var principal = new ClaimsPrincipal(identity);
        httpContextAccessor.HttpContext.Returns(new DefaultHttpContext { User = principal });

        var service = new CurrentUserService(httpContextAccessor);

        service.UserId.Should().BeNull();
    }
}
