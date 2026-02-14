using api.Application.Features.Recruitments.Queries.GetRecruitmentOverview;
using FluentAssertions;
using NUnit.Framework;

namespace api.Application.UnitTests.Features.Recruitments.Queries.GetRecruitmentOverview;

[TestFixture]
public class GetRecruitmentOverviewQueryValidatorTests
{
    private GetRecruitmentOverviewQueryValidator _validator = null!;

    [SetUp]
    public void SetUp()
    {
        _validator = new GetRecruitmentOverviewQueryValidator();
    }

    [Test]
    public void Validate_EmptyRecruitmentId_HasValidationError()
    {
        var query = new GetRecruitmentOverviewQuery { RecruitmentId = Guid.Empty };

        var result = _validator.Validate(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "RecruitmentId");
    }

    [Test]
    public void Validate_ValidRecruitmentId_PassesValidation()
    {
        var query = new GetRecruitmentOverviewQuery { RecruitmentId = Guid.NewGuid() };

        var result = _validator.Validate(query);

        result.IsValid.Should().BeTrue();
    }
}
