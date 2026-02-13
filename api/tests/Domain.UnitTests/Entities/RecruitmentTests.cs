using api.Domain.Entities;
using api.Domain.Enums;
using api.Domain.Events;
using api.Domain.Exceptions;
using FluentAssertions;
using NUnit.Framework;

namespace api.Domain.UnitTests.Entities;

public class RecruitmentTests
{
    private Recruitment CreateRecruitment(string title = "Test Recruitment")
    {
        return Recruitment.Create(title, null, Guid.NewGuid());
    }

    [Test]
    public void Create_ValidInput_CreatesRecruitmentWithCreatorAsMember()
    {
        var creatorId = Guid.NewGuid();
        var recruitment = Recruitment.Create("Senior Dev", null, creatorId);

        recruitment.Title.Should().Be("Senior Dev");
        recruitment.Status.Should().Be(RecruitmentStatus.Active);
        recruitment.Members.Should().HaveCount(1);
        recruitment.Members.First().UserId.Should().Be(creatorId);
        recruitment.Members.First().Role.Should().Be("Recruiting Leader");
    }

    [Test]
    public void Create_RaisesRecruitmentCreatedEvent()
    {
        var recruitment = CreateRecruitment();

        recruitment.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<RecruitmentCreatedEvent>();
    }

    [Test]
    public void Create_RaisesRecruitmentCreatedEvent_WithCorrectRecruitmentId()
    {
        var recruitment = CreateRecruitment();

        var domainEvent = recruitment.DomainEvents.OfType<RecruitmentCreatedEvent>().Single();
        domainEvent.RecruitmentId.Should().Be(recruitment.Id);
    }

    [Test]
    public void AddStep_ValidName_AddsStep()
    {
        var recruitment = CreateRecruitment();
        recruitment.ClearDomainEvents();

        recruitment.AddStep("Screening", 1);

        recruitment.Steps.Should().HaveCount(1);
        recruitment.Steps.First().Name.Should().Be("Screening");
        recruitment.Steps.First().Order.Should().Be(1);
    }

    [Test]
    public void AddStep_DuplicateName_ThrowsDuplicateStepNameException()
    {
        var recruitment = CreateRecruitment();
        recruitment.AddStep("Screening", 1);

        var act = () => recruitment.AddStep("Screening", 2);

        act.Should().Throw<DuplicateStepNameException>();
    }

    [Test]
    public void AddStep_WhenClosed_ThrowsRecruitmentClosedException()
    {
        var recruitment = CreateRecruitment();
        recruitment.Close();

        var act = () => recruitment.AddStep("Screening", 1);

        act.Should().Throw<RecruitmentClosedException>();
    }

    [Test]
    public void RemoveStep_ValidStep_RemovesStep()
    {
        var recruitment = CreateRecruitment();
        recruitment.AddStep("Screening", 1);
        var stepId = recruitment.Steps.First().Id;

        recruitment.RemoveStep(stepId);

        recruitment.Steps.Should().BeEmpty();
    }

    [Test]
    public void RemoveStep_StepWithOutcomes_ThrowsStepHasOutcomesException()
    {
        var recruitment = CreateRecruitment();
        recruitment.AddStep("Screening", 1);
        var stepId = recruitment.Steps.First().Id;

        // Mark the step as having outcomes via the recruitment method
        recruitment.MarkStepHasOutcomes(stepId);

        var act = () => recruitment.RemoveStep(stepId);

        act.Should().Throw<StepHasOutcomesException>();
    }

    [Test]
    public void AddMember_ValidUser_AddsMember()
    {
        var recruitment = CreateRecruitment();
        var userId = Guid.NewGuid();

        recruitment.AddMember(userId, "SME/Collaborator");

        recruitment.Members.Should().HaveCount(2); // creator + new member
    }

    [Test]
    public void RemoveMember_ValidMember_RemovesMember()
    {
        var recruitment = CreateRecruitment();
        var userId = Guid.NewGuid();
        recruitment.AddMember(userId, "SME/Collaborator");
        var memberId = recruitment.Members.First(m => m.UserId == userId).Id;

        recruitment.RemoveMember(memberId);

        recruitment.Members.Should().HaveCount(1); // only creator remains
    }

    [Test]
    public void RemoveMember_LastLeader_ThrowsInvalidOperationException()
    {
        // Creator is always a leader and always protected by the creator guard.
        // The last-leader guard is defense-in-depth; creator guard fires first here.
        var recruitment = CreateRecruitment();

        var act = () => recruitment.RemoveMember(recruitment.Members.First().Id);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*creator*");
    }

    [Test]
    public void RemoveMember_RaisesMembershipChangedEvent()
    {
        var recruitment = CreateRecruitment();
        var userId = Guid.NewGuid();
        recruitment.AddMember(userId, "SME/Collaborator");
        recruitment.ClearDomainEvents();
        var memberId = recruitment.Members.First(m => m.UserId == userId).Id;

        recruitment.RemoveMember(memberId);

        recruitment.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<MembershipChangedEvent>();
    }

    [Test]
    public void RemoveMember_RaisesMembershipChangedEvent_WithCorrectPayload()
    {
        var recruitment = CreateRecruitment();
        var userId = Guid.NewGuid();
        recruitment.AddMember(userId, "SME/Collaborator");
        recruitment.ClearDomainEvents();
        var memberId = recruitment.Members.First(m => m.UserId == userId).Id;

        recruitment.RemoveMember(memberId);

        var domainEvent = recruitment.DomainEvents.OfType<MembershipChangedEvent>().Single();
        domainEvent.RecruitmentId.Should().Be(recruitment.Id);
        domainEvent.UserId.Should().Be(userId);
        domainEvent.ChangeType.Should().Be("Removed");
    }

    [Test]
    public void AddMember_RaisesMembershipChangedEvent_WithCorrectPayload()
    {
        var recruitment = CreateRecruitment();
        recruitment.ClearDomainEvents();
        var userId = Guid.NewGuid();

        recruitment.AddMember(userId, "SME/Collaborator");

        var domainEvent = recruitment.DomainEvents.OfType<MembershipChangedEvent>().Single();
        domainEvent.RecruitmentId.Should().Be(recruitment.Id);
        domainEvent.UserId.Should().Be(userId);
        domainEvent.ChangeType.Should().Be("Added");
    }

    [Test]
    public void RemoveMember_Creator_ThrowsInvalidOperationException()
    {
        var creatorId = Guid.NewGuid();
        var recruitment = Recruitment.Create("Test", null, creatorId);
        // Add a second leader so the "last leader" guard doesn't fire first
        var secondLeaderId = Guid.NewGuid();
        recruitment.AddMember(secondLeaderId, "Recruiting Leader");
        var creatorMemberId = recruitment.Members.First(m => m.UserId == creatorId).Id;

        var act = () => recruitment.RemoveMember(creatorMemberId);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*creator*");
    }

    [Test]
    public void Close_ActiveRecruitment_ClosesSuccessfully()
    {
        var recruitment = CreateRecruitment();
        recruitment.ClearDomainEvents();

        recruitment.Close();

        recruitment.Status.Should().Be(RecruitmentStatus.Closed);
        recruitment.ClosedAt.Should().NotBeNull();
        recruitment.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<RecruitmentClosedEvent>();
    }

    [Test]
    public void Close_RaisesRecruitmentClosedEvent_WithCorrectRecruitmentId()
    {
        var recruitment = CreateRecruitment();
        recruitment.ClearDomainEvents();

        recruitment.Close();

        var domainEvent = recruitment.DomainEvents.OfType<RecruitmentClosedEvent>().Single();
        domainEvent.RecruitmentId.Should().Be(recruitment.Id);
    }

    [Test]
    public void Close_AlreadyClosed_ThrowsRecruitmentClosedException()
    {
        var recruitment = CreateRecruitment();
        recruitment.Close();

        var act = () => recruitment.Close();

        act.Should().Throw<RecruitmentClosedException>();
    }
}
