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
    public void UserId_DelegatesToIUser()
    {
        var user = Substitute.For<IUser>();
        user.Id.Returns("user-456");

        var tenantContext = new TenantContext(user);

        tenantContext.UserId.Should().Be("user-456");
    }

    [Test]
    public void RecruitmentId_DefaultsToNull()
    {
        var user = Substitute.For<IUser>();
        var tenantContext = new TenantContext(user);

        tenantContext.RecruitmentId.Should().BeNull();
    }

    [Test]
    public void IsServiceContext_DefaultsToFalse()
    {
        var user = Substitute.For<IUser>();
        var tenantContext = new TenantContext(user);

        tenantContext.IsServiceContext.Should().BeFalse();
    }
}
