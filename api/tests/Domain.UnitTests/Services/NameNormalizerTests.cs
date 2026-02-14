using api.Domain.Services;
using FluentAssertions;
using NUnit.Framework;

namespace api.Domain.UnitTests.Services;

public class NameNormalizerTests
{
    [Test]
    public void Normalize_NameWithDiacritics_ReturnsDiacriticsStripped()
    {
        NameNormalizer.Normalize("Éric du Pont").Should().Be("eric du pont");
    }

    [Test]
    public void Normalize_NameWithMultipleSpaces_ReturnsCollapsedSpaces()
    {
        NameNormalizer.Normalize("  Éric  du   Pont ").Should().Be("eric du pont");
    }

    [Test]
    public void Normalize_NullInput_ReturnsEmptyString()
    {
        NameNormalizer.Normalize(null).Should().Be(string.Empty);
    }

    [Test]
    public void Normalize_EmptyInput_ReturnsEmptyString()
    {
        NameNormalizer.Normalize("  ").Should().Be(string.Empty);
    }

    [Test]
    public void Normalize_HyphensPreserved()
    {
        NameNormalizer.Normalize("AnnA-Lisa").Should().Be("anna-lisa");
    }

    [Test]
    public void Normalize_IcelandicName_ReturnsDiacriticsStripped()
    {
        // Note: ð (eth) is a standalone Unicode letter, not a decomposable diacritical.
        // Both sides of matching normalize consistently, so matching still works.
        NameNormalizer.Normalize("Björk Guðmundsdóttir").Should().Be("bjork guðmundsdottir");
    }

    [Test]
    public void Normalize_MixedCase_ReturnsLowercase()
    {
        NameNormalizer.Normalize("ALICE JOHNSON").Should().Be("alice johnson");
    }
}
