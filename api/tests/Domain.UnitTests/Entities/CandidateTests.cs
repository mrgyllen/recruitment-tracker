using api.Domain.Entities;
using api.Domain.Enums;
using api.Domain.Events;
using api.Domain.Exceptions;
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

    [Test]
    public void UpdateProfile_UpdatesAllProfileFields()
    {
        var candidate = CreateCandidate();

        candidate.UpdateProfile("Bob Smith", "+9876543210", "London, UK", DateTimeOffset.Parse("2025-06-15T00:00:00Z"));

        candidate.FullName.Should().Be("Bob Smith");
        candidate.PhoneNumber.Should().Be("+9876543210");
        candidate.Location.Should().Be("London, UK");
        candidate.DateApplied.Should().Be(DateTimeOffset.Parse("2025-06-15T00:00:00Z"));
    }

    [Test]
    public void UpdateProfile_DoesNotAffectOutcomes()
    {
        var candidate = CreateCandidate();
        candidate.RecordOutcome(Guid.NewGuid(), OutcomeStatus.Pass, Guid.NewGuid());
        var outcomeCount = candidate.Outcomes.Count;

        candidate.UpdateProfile("Updated Name", null, null, DateTimeOffset.UtcNow);

        candidate.Outcomes.Should().HaveCount(outcomeCount);
    }

    [Test]
    public void UpdateProfile_DoesNotAffectDocuments()
    {
        var candidate = CreateCandidate();
        candidate.AttachDocument("CV", "https://blob.storage/cv.pdf");
        var docCount = candidate.Documents.Count;

        candidate.UpdateProfile("Updated Name", null, null, DateTimeOffset.UtcNow);

        candidate.Documents.Should().HaveCount(docCount);
    }

    [Test]
    public void UpdateProfile_DoesNotChangeEmail()
    {
        var candidate = CreateCandidate();
        var originalEmail = candidate.Email;

        candidate.UpdateProfile("New Name", null, null, DateTimeOffset.UtcNow);

        candidate.Email.Should().Be(originalEmail);
    }

    [Test]
    public void ReplaceDocument_ExistingDocument_RemovesOldAndAddsNew()
    {
        var candidate = CreateCandidate();
        candidate.AttachDocument("CV", "https://blob.storage/old.pdf");
        candidate.ClearDomainEvents();

        var oldUrl = candidate.ReplaceDocument("CV", "https://blob.storage/new.pdf");

        oldUrl.Should().Be("https://blob.storage/old.pdf");
        candidate.Documents.Should().HaveCount(1);
        candidate.Documents.First().BlobStorageUrl.Should().Be("https://blob.storage/new.pdf");
    }

    [Test]
    public void ReplaceDocument_NoExistingDocument_AddsNewAndReturnsNull()
    {
        var candidate = CreateCandidate();

        var oldUrl = candidate.ReplaceDocument("CV", "https://blob.storage/new.pdf");

        oldUrl.Should().BeNull();
        candidate.Documents.Should().HaveCount(1);
    }

    [Test]
    public void ReplaceDocument_CaseInsensitiveType_ReplacesExisting()
    {
        var candidate = CreateCandidate();
        candidate.AttachDocument("CV", "https://blob.storage/old.pdf");

        var oldUrl = candidate.ReplaceDocument("cv", "https://blob.storage/new.pdf");

        oldUrl.Should().Be("https://blob.storage/old.pdf");
        candidate.Documents.Should().HaveCount(1);
    }

    [Test]
    public void ReplaceDocument_RaisesDocumentUploadedEvent()
    {
        var candidate = CreateCandidate();
        candidate.ClearDomainEvents();

        candidate.ReplaceDocument("CV", "https://blob.storage/new.pdf");

        candidate.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<DocumentUploadedEvent>();
    }

    [Test]
    public void ReplaceDocument_WithWorkdayParams_SetsMetadataOnDocument()
    {
        var candidate = CreateCandidate();

        candidate.ReplaceDocument("CV", "https://blob.storage/new.pdf",
            workdayCandidateId: "WD-123", documentSource: DocumentSource.BundleSplit);

        var doc = candidate.Documents.First();
        doc.WorkdayCandidateId.Should().Be("WD-123");
        doc.DocumentSource.Should().Be(DocumentSource.BundleSplit);
    }

    #region Workflow Enforcement (Story 4.3)

    private static (Candidate candidate, Recruitment recruitment) CreateCandidateWithWorkflow()
    {
        var userId = Guid.NewGuid();
        var recruitment = Recruitment.Create("Test Recruitment", null, userId);
        recruitment.AddStep("Screening", 1);
        recruitment.AddStep("Interview", 2);
        recruitment.AddStep("Final", 3);
        var orderedSteps = recruitment.Steps.OrderBy(s => s.Order).ToList();

        var candidate = Candidate.Create(
            recruitment.Id, "Alice", "alice@example.com", null, null, DateTimeOffset.UtcNow);
        candidate.AssignToWorkflowStep(orderedSteps[0].Id);

        return (candidate, recruitment);
    }

    [Test]
    public void RecordOutcome_ValidStep_CreatesOutcomeWithReason()
    {
        var (candidate, recruitment) = CreateCandidateWithWorkflow();
        var steps = recruitment.Steps.OrderBy(s => s.Order).ToList();

        candidate.RecordOutcome(steps[0].Id, OutcomeStatus.Pass, Guid.NewGuid(), "Strong candidate", steps);

        candidate.Outcomes.Should().HaveCount(1);
        var outcome = candidate.Outcomes.First();
        outcome.WorkflowStepId.Should().Be(steps[0].Id);
        outcome.Status.Should().Be(OutcomeStatus.Pass);
        outcome.Reason.Should().Be("Strong candidate");
    }

    [Test]
    public void RecordOutcome_WrongStep_ThrowsInvalidWorkflowTransitionException()
    {
        var (candidate, recruitment) = CreateCandidateWithWorkflow();
        var steps = recruitment.Steps.OrderBy(s => s.Order).ToList();

        var act = () => candidate.RecordOutcome(steps[2].Id, OutcomeStatus.Pass, Guid.NewGuid(), null, steps);

        act.Should().Throw<InvalidWorkflowTransitionException>();
    }

    [Test]
    public void RecordOutcome_PassNotLastStep_AdvancesToNextStep()
    {
        var (candidate, recruitment) = CreateCandidateWithWorkflow();
        var steps = recruitment.Steps.OrderBy(s => s.Order).ToList();

        candidate.RecordOutcome(steps[0].Id, OutcomeStatus.Pass, Guid.NewGuid(), null, steps);

        candidate.CurrentWorkflowStepId.Should().Be(steps[1].Id);
        candidate.IsCompleted.Should().BeFalse();
    }

    [Test]
    public void RecordOutcome_PassLastStep_SetsIsCompleted()
    {
        var (candidate, recruitment) = CreateCandidateWithWorkflow();
        var steps = recruitment.Steps.OrderBy(s => s.Order).ToList();
        candidate.RecordOutcome(steps[0].Id, OutcomeStatus.Pass, Guid.NewGuid(), null, steps);
        candidate.RecordOutcome(steps[1].Id, OutcomeStatus.Pass, Guid.NewGuid(), null, steps);

        candidate.RecordOutcome(steps[2].Id, OutcomeStatus.Pass, Guid.NewGuid(), null, steps);

        candidate.IsCompleted.Should().BeTrue();
        candidate.CurrentWorkflowStepId.Should().Be(steps[2].Id);
    }

    [Test]
    public void RecordOutcome_FailOrHold_StaysAtCurrentStep()
    {
        var (candidate, recruitment) = CreateCandidateWithWorkflow();
        var steps = recruitment.Steps.OrderBy(s => s.Order).ToList();
        var originalStepId = candidate.CurrentWorkflowStepId;

        candidate.RecordOutcome(steps[0].Id, OutcomeStatus.Fail, Guid.NewGuid(), "Not qualified", steps);

        candidate.CurrentWorkflowStepId.Should().Be(originalStepId);
        candidate.IsCompleted.Should().BeFalse();
    }

    [Test]
    public void RecordOutcome_ReRecord_ReplacesExistingOutcome()
    {
        var (candidate, recruitment) = CreateCandidateWithWorkflow();
        var steps = recruitment.Steps.OrderBy(s => s.Order).ToList();
        var userId = Guid.NewGuid();
        candidate.RecordOutcome(steps[0].Id, OutcomeStatus.Fail, userId, "Initial", steps);

        candidate.RecordOutcome(steps[0].Id, OutcomeStatus.Pass, userId, "Reconsidered", steps);

        candidate.Outcomes.Where(o => o.WorkflowStepId == steps[0].Id).Should().HaveCount(1);
        candidate.Outcomes.First(o => o.WorkflowStepId == steps[0].Id).Status.Should().Be(OutcomeStatus.Pass);
        candidate.Outcomes.First(o => o.WorkflowStepId == steps[0].Id).Reason.Should().Be("Reconsidered");
    }

    [Test]
    public void RecordOutcome_NoCurrentStep_ThrowsInvalidOperationException()
    {
        var candidate = Candidate.Create(
            Guid.NewGuid(), "Alice", "alice@example.com", null, null, DateTimeOffset.UtcNow);

        var act = () => candidate.RecordOutcome(Guid.NewGuid(), OutcomeStatus.Pass, Guid.NewGuid(), null, new List<WorkflowStep>());

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*not been assigned*");
    }

    [Test]
    public void RecordOutcome_EnhancedMethod_RaisesOutcomeRecordedEvent()
    {
        var (candidate, recruitment) = CreateCandidateWithWorkflow();
        var steps = recruitment.Steps.OrderBy(s => s.Order).ToList();
        candidate.ClearDomainEvents();

        candidate.RecordOutcome(steps[0].Id, OutcomeStatus.Pass, Guid.NewGuid(), null, steps);

        candidate.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<OutcomeRecordedEvent>();
    }

    [Test]
    public void RecordOutcome_HoldThenReRecordAsPass_AdvancesToNextStep()
    {
        var (candidate, recruitment) = CreateCandidateWithWorkflow();
        var steps = recruitment.Steps.OrderBy(s => s.Order).ToList();
        candidate.RecordOutcome(steps[0].Id, OutcomeStatus.Hold, Guid.NewGuid(), "Needs review", steps);

        candidate.RecordOutcome(steps[0].Id, OutcomeStatus.Pass, Guid.NewGuid(), "Approved after review", steps);

        candidate.CurrentWorkflowStepId.Should().Be(steps[1].Id);
    }

    #endregion
}
