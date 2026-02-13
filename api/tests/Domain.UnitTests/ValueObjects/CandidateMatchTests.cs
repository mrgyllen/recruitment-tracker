using api.Domain.Enums;
using api.Domain.ValueObjects;
using FluentAssertions;
using NUnit.Framework;

namespace api.Domain.UnitTests.ValueObjects;

public class CandidateMatchTests
{
    [Test]
    public void Equals_SameValues_ReturnsTrue()
    {
        var a = new CandidateMatch(ImportMatchConfidence.High, "email");
        var b = new CandidateMatch(ImportMatchConfidence.High, "email");

        a.Should().Be(b);
        (a == b).Should().BeTrue();
    }

    [Test]
    public void Equals_DifferentValues_ReturnsFalse()
    {
        var a = new CandidateMatch(ImportMatchConfidence.High, "email");
        var b = new CandidateMatch(ImportMatchConfidence.Low, "name+phone");

        a.Should().NotBe(b);
        (a != b).Should().BeTrue();
    }

    [Test]
    public void Properties_AreReadOnly()
    {
        var match = new CandidateMatch(ImportMatchConfidence.High, "email");

        match.Confidence.Should().Be(ImportMatchConfidence.High);
        match.MatchMethod.Should().Be("email");
    }
}
