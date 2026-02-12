using api.Application.Common.Interfaces;
using api.Infrastructure.Identity;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace api.Application.UnitTests.Common.Identity;

[TestFixture]
public class TenantContextTests
{
    [Test]
    public void UserId_DelegatesToCurrentUserService()
    {
        var currentUserService = Substitute.For<ICurrentUserService>();
        currentUserService.UserId.Returns("user-456");

        var tenantContext = new TenantContext(currentUserService);

        tenantContext.UserId.Should().Be("user-456");
    }

    [Test]
    public void RecruitmentId_DefaultsToNull()
    {
        var currentUserService = Substitute.For<ICurrentUserService>();
        var tenantContext = new TenantContext(currentUserService);

        tenantContext.RecruitmentId.Should().BeNull();
    }

    [Test]
    public void IsServiceContext_DefaultsToFalse()
    {
        var currentUserService = Substitute.For<ICurrentUserService>();
        var tenantContext = new TenantContext(currentUserService);

        tenantContext.IsServiceContext.Should().BeFalse();
    }
}
