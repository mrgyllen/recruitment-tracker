using api.Domain.Entities;
using api.Domain.Enums;
using api.Domain.Events;
using FluentAssertions;
using NUnit.Framework;

namespace api.Domain.UnitTests.Entities;

public class CandidateTests
{
    private Candidate CreateCandidate()
    {
        return Candidate.Create(
            recruitmentId: Guid.NewGuid(),
            fullName: "Alice Johnson",
            email: "alice@example.com",
            phoneNumber: "+1234567890",
            location: "Oslo, Norway",
            dateApplied: DateTimeOffset.UtcNow);
    }

    [Test]
    public void RecordOutcome_ValidInput_AddsOutcome()
    {
        var candidate = CreateCandidate();
        var stepId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        candidate.RecordOutcome(stepId, OutcomeStatus.Pass, userId);

        candidate.Outcomes.Should().HaveCount(1);
        candidate.Outcomes.First().WorkflowStepId.Should().Be(stepId);
        candidate.Outcomes.First().Status.Should().Be(OutcomeStatus.Pass);
    }

    [Test]
    public void RecordOutcome_RaisesOutcomeRecordedEvent()
    {
        var candidate = CreateCandidate();
        candidate.ClearDomainEvents();

        candidate.RecordOutcome(Guid.NewGuid(), OutcomeStatus.Pass, Guid.NewGuid());

        candidate.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<OutcomeRecordedEvent>();
    }

    [Test]
    public void AttachDocument_ValidInput_AddsDocument()
    {
        var candidate = CreateCandidate();

        candidate.AttachDocument("CV", "https://blob.storage/cv.pdf");

        candidate.Documents.Should().HaveCount(1);
        candidate.Documents.First().DocumentType.Should().Be("CV");
    }

    [Test]
    public void AttachDocument_DuplicateType_ThrowsInvalidOperationException()
    {
        var candidate = CreateCandidate();
        candidate.AttachDocument("CV", "https://blob.storage/cv.pdf");

        var act = () => candidate.AttachDocument("CV", "https://blob.storage/cv2.pdf");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*document of type*already exists*");
    }

    [Test]
    public void AttachDocument_RaisesDocumentUploadedEvent()
    {
        var candidate = CreateCandidate();
        candidate.ClearDomainEvents();

        candidate.AttachDocument("CV", "https://blob.storage/cv.pdf");

        candidate.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<DocumentUploadedEvent>();
    }

    [Test]
    public void Anonymize_ClearsPiiFields()
    {
        var candidate = CreateCandidate();
        var originalId = candidate.Id;
        var originalRecruitmentId = candidate.RecruitmentId;
        var originalDateApplied = candidate.DateApplied;
        candidate.RecordOutcome(Guid.NewGuid(), OutcomeStatus.Pass, Guid.NewGuid());

        candidate.Anonymize();

        candidate.FullName.Should().BeNull();
        candidate.Email.Should().BeNull();
        candidate.PhoneNumber.Should().BeNull();
        candidate.Location.Should().BeNull();
        // Preserved fields
        candidate.Id.Should().Be(originalId);
        candidate.RecruitmentId.Should().Be(originalRecruitmentId);
        candidate.DateApplied.Should().Be(originalDateApplied);
        candidate.Outcomes.Should().HaveCount(1);
    }
}
