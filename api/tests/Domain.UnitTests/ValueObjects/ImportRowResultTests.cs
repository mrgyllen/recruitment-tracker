using api.Domain.Enums;
using api.Domain.ValueObjects;
using FluentAssertions;
using NUnit.Framework;

namespace api.Domain.UnitTests.ValueObjects;

[TestFixture]
public class ImportRowResultTests
{
    [Test]
    public void Create_ValidInput_SetsAllProperties()
    {
        var result = new ImportRowResult(3, "alice@example.com", ImportRowAction.Created, null);

        result.RowNumber.Should().Be(3);
        result.CandidateEmail.Should().Be("alice@example.com");
        result.Action.Should().Be(ImportRowAction.Created);
        result.ErrorMessage.Should().BeNull();
        result.Resolution.Should().BeNull();
    }

    [Test]
    public void Create_ErroredRow_StoresErrorMessage()
    {
        var result = new ImportRowResult(5, "bad@example.com", ImportRowAction.Errored, "Invalid email format");

        result.Action.Should().Be(ImportRowAction.Errored);
        result.ErrorMessage.Should().Be("Invalid email format");
    }

    [Test]
    public void Confirm_FlaggedRow_SetsResolution()
    {
        var result = new ImportRowResult(1, "a@b.com", ImportRowAction.Flagged, null);

        result.Confirm();

        result.Resolution.Should().Be("Confirmed");
    }

    [Test]
    public void Reject_FlaggedRow_SetsResolution()
    {
        var result = new ImportRowResult(1, "a@b.com", ImportRowAction.Flagged, null);

        result.Reject();

        result.Resolution.Should().Be("Rejected");
    }

    [Test]
    public void Confirm_NonFlaggedRow_Throws()
    {
        var result = new ImportRowResult(1, "a@b.com", ImportRowAction.Created, null);

        var act = () => result.Confirm();

        act.Should().Throw<InvalidOperationException>();
    }

    [Test]
    public void Confirm_AlreadyResolved_Throws()
    {
        var result = new ImportRowResult(1, "a@b.com", ImportRowAction.Flagged, null);
        result.Confirm();

        var act = () => result.Confirm();

        act.Should().Throw<InvalidOperationException>();
    }
}
