using api.Domain.Enums;
using FluentAssertions;
using NUnit.Framework;

namespace api.Domain.UnitTests.Enums;

public class EnumValueTests
{
    [Test]
    public void OutcomeStatus_HasExpectedValues()
    {
        Enum.GetNames<OutcomeStatus>().Should()
            .BeEquivalentTo("NotStarted", "Pass", "Fail", "Hold");
    }

    [Test]
    public void ImportMatchConfidence_HasExpectedValues()
    {
        Enum.GetNames<ImportMatchConfidence>().Should()
            .BeEquivalentTo("High", "Low", "None");
    }

    [Test]
    public void RecruitmentStatus_HasExpectedValues()
    {
        Enum.GetNames<RecruitmentStatus>().Should()
            .BeEquivalentTo("Active", "Closed");
    }

    [Test]
    public void ImportSessionStatus_HasExpectedValues()
    {
        Enum.GetNames<ImportSessionStatus>().Should()
            .BeEquivalentTo("Processing", "Completed", "Failed");
    }
}
