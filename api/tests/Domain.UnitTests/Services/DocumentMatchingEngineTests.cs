using api.Domain.Entities;
using api.Domain.Enums;
using api.Domain.Models;
using api.Domain.Services;
using FluentAssertions;
using NUnit.Framework;

namespace api.Domain.UnitTests.Services;

public class DocumentMatchingEngineTests
{
    private DocumentMatchingEngine _engine = null!;

    [SetUp]
    public void SetUp() => _engine = new DocumentMatchingEngine();

    private static Candidate CreateCandidate(string fullName)
    {
        return Candidate.Create(
            Guid.NewGuid(), fullName, $"{fullName.Replace(" ", "")}@test.com",
            null, null, DateTimeOffset.UtcNow);
    }

    [Test]
    public void Match_ExactNameAfterNormalization_ReturnsAutoMatched()
    {
        var candidates = new[] { CreateCandidate("Alice Johnson") };
        var docs = new[] { new SplitDocument("alice johnson", "blob://cv.pdf", null) };

        var results = _engine.MatchDocumentsToCandidates(docs, candidates);

        results.Should().ContainSingle()
            .Which.Status.Should().Be(ImportDocumentMatchStatus.AutoMatched);
        results[0].MatchedCandidateId.Should().Be(candidates[0].Id);
    }

    [Test]
    public void Match_NoMatchingCandidate_ReturnsUnmatched()
    {
        var candidates = new[] { CreateCandidate("Bob Smith") };
        var docs = new[] { new SplitDocument("Unknown Person", "blob://cv.pdf", null) };

        var results = _engine.MatchDocumentsToCandidates(docs, candidates);

        results.Should().ContainSingle()
            .Which.Status.Should().Be(ImportDocumentMatchStatus.Unmatched);
        results[0].MatchedCandidateId.Should().BeNull();
    }

    [Test]
    public void Match_MultipleCandidatesWithSameName_ReturnsUnmatched()
    {
        var candidates = new[]
        {
            CreateCandidate("Alice Johnson"),
            CreateCandidate("Alice Johnson"),
        };
        var docs = new[] { new SplitDocument("Alice Johnson", "blob://cv.pdf", null) };

        var results = _engine.MatchDocumentsToCandidates(docs, candidates);

        results.Should().ContainSingle()
            .Which.Status.Should().Be(ImportDocumentMatchStatus.Unmatched);
    }

    [Test]
    public void Match_DiacriticsInDocumentName_MatchesStrippedCandidate()
    {
        var candidates = new[] { CreateCandidate("Eric du Pont") };
        var docs = new[] { new SplitDocument("Éric du Pont", "blob://cv.pdf", null) };

        var results = _engine.MatchDocumentsToCandidates(docs, candidates);

        results.Should().ContainSingle()
            .Which.Status.Should().Be(ImportDocumentMatchStatus.AutoMatched);
    }

    [Test]
    public void Match_EmptyDocumentList_ReturnsEmptyResults()
    {
        var candidates = new[] { CreateCandidate("Alice") };

        var results = _engine.MatchDocumentsToCandidates([], candidates);

        results.Should().BeEmpty();
    }

    [Test]
    public void Match_EmptyCandidateList_AllUnmatched()
    {
        var docs = new[] { new SplitDocument("Alice", "blob://cv.pdf", null) };

        var results = _engine.MatchDocumentsToCandidates(docs, []);

        results.Should().ContainSingle()
            .Which.Status.Should().Be(ImportDocumentMatchStatus.Unmatched);
    }
}
