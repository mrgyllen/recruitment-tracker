using api.Domain.ValueObjects;
using FluentAssertions;
using NUnit.Framework;

namespace api.Domain.UnitTests.ValueObjects;

public class AnonymizationResultTests
{
    [Test]
    public void Equals_SameValues_ReturnsTrue()
    {
        var a = new AnonymizationResult(10, 5);
        var b = new AnonymizationResult(10, 5);

        a.Should().Be(b);
        (a == b).Should().BeTrue();
    }

    [Test]
    public void Equals_DifferentValues_ReturnsFalse()
    {
        var a = new AnonymizationResult(10, 5);
        var b = new AnonymizationResult(20, 10);

        a.Should().NotBe(b);
    }

    [Test]
    public void Properties_AreReadOnly()
    {
        var result = new AnonymizationResult(10, 5);

        result.CandidatesAnonymized.Should().Be(10);
        result.DocumentsDeleted.Should().Be(5);
    }
}
