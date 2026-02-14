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
        var result = new ImportRowResult(
            RowNumber: 3,
            CandidateEmail: "alice@example.com",
            Action: ImportRowAction.Created,
            ErrorMessage: null);

        result.RowNumber.Should().Be(3);
        result.CandidateEmail.Should().Be("alice@example.com");
        result.Action.Should().Be(ImportRowAction.Created);
        result.ErrorMessage.Should().BeNull();
    }

    [Test]
    public void Create_ErroredRow_StoresErrorMessage()
    {
        var result = new ImportRowResult(
            RowNumber: 5,
            CandidateEmail: "bad@example.com",
            Action: ImportRowAction.Errored,
            ErrorMessage: "Invalid email format");

        result.Action.Should().Be(ImportRowAction.Errored);
        result.ErrorMessage.Should().Be("Invalid email format");
    }

    [Test]
    public void ValueEquality_SameValues_AreEqual()
    {
        var a = new ImportRowResult(1, "a@b.com", ImportRowAction.Created, null);
        var b = new ImportRowResult(1, "a@b.com", ImportRowAction.Created, null);

        a.Should().Be(b);
    }
}
