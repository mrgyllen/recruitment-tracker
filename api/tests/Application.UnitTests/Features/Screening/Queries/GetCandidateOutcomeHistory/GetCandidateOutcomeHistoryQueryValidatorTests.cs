using api.Application.Features.Screening.Queries.GetCandidateOutcomeHistory;
using FluentAssertions;
using NUnit.Framework;

namespace api.Application.UnitTests.Features.Screening.Queries.GetCandidateOutcomeHistory;

[TestFixture]
public class GetCandidateOutcomeHistoryQueryValidatorTests
{
    private GetCandidateOutcomeHistoryQueryValidator _validator = null!;

    [SetUp]
    public void SetUp()
    {
        _validator = new GetCandidateOutcomeHistoryQueryValidator();
    }

    [Test]
    public void Validate_ValidQuery_Passes()
    {
        var query = new GetCandidateOutcomeHistoryQuery(Guid.NewGuid(), Guid.NewGuid());
        var result = _validator.Validate(query);
        result.IsValid.Should().BeTrue();
    }

    [Test]
    public void Validate_EmptyRecruitmentId_Fails()
    {
        var query = new GetCandidateOutcomeHistoryQuery(Guid.Empty, Guid.NewGuid());
        var result = _validator.Validate(query);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "RecruitmentId");
    }

    [Test]
    public void Validate_EmptyCandidateId_Fails()
    {
        var query = new GetCandidateOutcomeHistoryQuery(Guid.NewGuid(), Guid.Empty);
        var result = _validator.Validate(query);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CandidateId");
    }
}
