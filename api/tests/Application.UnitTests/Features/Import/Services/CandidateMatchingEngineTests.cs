using api.Domain.Entities;
using api.Domain.Enums;
using api.Domain.ValueObjects;
using api.Infrastructure.Services;
using FluentAssertions;
using NUnit.Framework;

namespace api.Application.UnitTests.Features.Import.Services;

[TestFixture]
public class CandidateMatchingEngineTests
{
    private CandidateMatchingEngine _engine = null!;

    [SetUp]
    public void SetUp()
    {
        _engine = new CandidateMatchingEngine();
    }

    [Test]
    public void Match_EmailMatch_ReturnsHighConfidence()
    {
        var existing = new List<Candidate>
        {
            Candidate.Create(Guid.NewGuid(), "Alice Johnson", "alice@example.com", null, null, DateTimeOffset.UtcNow)
        };
        var row = new ParsedCandidateRow(1, "Alice J", "alice@example.com", null, null, null);

        var result = _engine.Match(row, existing);

        result.Confidence.Should().Be(ImportMatchConfidence.High);
        result.MatchMethod.Should().Be("Email");
    }

    [Test]
    public void Match_EmailMatch_CaseInsensitive()
    {
        var existing = new List<Candidate>
        {
            Candidate.Create(Guid.NewGuid(), "Alice", "Alice@Example.COM", null, null, DateTimeOffset.UtcNow)
        };
        var row = new ParsedCandidateRow(1, "Alice", "alice@example.com", null, null, null);

        var result = _engine.Match(row, existing);

        result.Confidence.Should().Be(ImportMatchConfidence.High);
    }

    [Test]
    public void Match_NameAndPhoneMatch_ReturnsLowConfidence()
    {
        var existing = new List<Candidate>
        {
            Candidate.Create(Guid.NewGuid(), "Alice Johnson", "alice-old@example.com", "+1234567890", null, DateTimeOffset.UtcNow)
        };
        var row = new ParsedCandidateRow(1, "Alice Johnson", "alice-new@example.com", "+1234567890", null, null);

        var result = _engine.Match(row, existing);

        result.Confidence.Should().Be(ImportMatchConfidence.Low);
        result.MatchMethod.Should().Be("NameAndPhone");
    }

    [Test]
    public void Match_NoMatch_ReturnsNoneConfidence()
    {
        var existing = new List<Candidate>
        {
            Candidate.Create(Guid.NewGuid(), "Alice Johnson", "alice@example.com", null, null, DateTimeOffset.UtcNow)
        };
        var row = new ParsedCandidateRow(1, "Bob Smith", "bob@example.com", null, null, null);

        var result = _engine.Match(row, existing);

        result.Confidence.Should().Be(ImportMatchConfidence.None);
        result.MatchMethod.Should().Be("None");
    }

    [Test]
    public void Match_Idempotent_SameInputSameResult()
    {
        var existing = new List<Candidate>
        {
            Candidate.Create(Guid.NewGuid(), "Alice", "alice@example.com", null, null, DateTimeOffset.UtcNow)
        };
        var row = new ParsedCandidateRow(1, "Alice", "alice@example.com", null, null, null);

        var result1 = _engine.Match(row, existing);
        var result2 = _engine.Match(row, existing);

        result1.Should().Be(result2);
    }

    [Test]
    public void Match_NameMatchWithoutPhone_ReturnsNone()
    {
        var existing = new List<Candidate>
        {
            Candidate.Create(Guid.NewGuid(), "Alice Johnson", "different@example.com", null, null, DateTimeOffset.UtcNow)
        };
        var row = new ParsedCandidateRow(1, "Alice Johnson", "other@example.com", null, null, null);

        var result = _engine.Match(row, existing);

        result.Confidence.Should().Be(ImportMatchConfidence.None);
    }
}
