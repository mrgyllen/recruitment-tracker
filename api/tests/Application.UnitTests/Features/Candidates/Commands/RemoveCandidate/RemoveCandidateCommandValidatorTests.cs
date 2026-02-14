using api.Application.Features.Candidates.Commands.RemoveCandidate;
using FluentAssertions;
using NUnit.Framework;

namespace api.Application.UnitTests.Features.Candidates.Commands.RemoveCandidate;

[TestFixture]
public class RemoveCandidateCommandValidatorTests
{
    private RemoveCandidateCommandValidator _validator = null!;

    [SetUp]
    public void SetUp()
    {
        _validator = new RemoveCandidateCommandValidator();
    }

    [Test]
    public void Validate_EmptyCandidateId_Fails()
    {
        var command = new RemoveCandidateCommand
        {
            RecruitmentId = Guid.NewGuid(),
            CandidateId = Guid.Empty,
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CandidateId");
    }

    [Test]
    public void Validate_EmptyRecruitmentId_Fails()
    {
        var command = new RemoveCandidateCommand
        {
            RecruitmentId = Guid.Empty,
            CandidateId = Guid.NewGuid(),
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "RecruitmentId");
    }
}
