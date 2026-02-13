using api.Domain.Common;
using api.Domain.Enums;
using api.Domain.Events;
using api.Domain.Exceptions;

namespace api.Domain.Entities;

public class Recruitment : GuidEntity
{
    public string Title { get; private set; } = null!;
    public string? Description { get; private set; }
    public string? JobRequisitionId { get; private set; }
    public RecruitmentStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? ClosedAt { get; private set; }
    public Guid CreatedByUserId { get; private set; }

    private readonly List<WorkflowStep> _steps = new();
    public IReadOnlyCollection<WorkflowStep> Steps => _steps.AsReadOnly();

    private readonly List<RecruitmentMember> _members = new();
    public IReadOnlyCollection<RecruitmentMember> Members => _members.AsReadOnly();

    // Track which steps have outcomes (set by application layer when outcomes exist)
    private readonly HashSet<Guid> _stepsWithOutcomes = new();

    private Recruitment() { } // EF Core

    public static Recruitment Create(string title, string? description, Guid createdByUserId)
    {
        var recruitment = new Recruitment
        {
            Title = title,
            Description = description,
            Status = RecruitmentStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedByUserId = createdByUserId,
        };

        recruitment._members.Add(RecruitmentMember.Create(
            recruitment.Id, createdByUserId, "Recruiting Leader"));

        recruitment.AddDomainEvent(new RecruitmentCreatedEvent(recruitment.Id));
        return recruitment;
    }

    public void AddStep(string name, int order)
    {
        EnsureNotClosed();

        if (_steps.Any(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
        {
            throw new DuplicateStepNameException(name);
        }

        _steps.Add(WorkflowStep.Create(Id, name, order));
    }

    public void RemoveStep(Guid stepId)
    {
        EnsureNotClosed();

        if (_stepsWithOutcomes.Contains(stepId))
        {
            throw new StepHasOutcomesException(stepId);
        }

        var step = _steps.FirstOrDefault(s => s.Id == stepId)
            ?? throw new InvalidOperationException($"Step {stepId} not found.");

        _steps.Remove(step);
    }

    public void MarkStepHasOutcomes(Guid stepId)
    {
        _stepsWithOutcomes.Add(stepId);
    }

    public void AddMember(Guid userId, string role)
    {
        EnsureNotClosed();

        if (_members.Any(m => m.UserId == userId))
        {
            throw new InvalidOperationException($"User {userId} is already a member.");
        }

        var member = RecruitmentMember.Create(Id, userId, role);
        _members.Add(member);
        AddDomainEvent(new MembershipChangedEvent(Id, userId, "Added"));
    }

    public void RemoveMember(Guid memberId)
    {
        EnsureNotClosed();

        var member = _members.FirstOrDefault(m => m.Id == memberId)
            ?? throw new InvalidOperationException($"Member {memberId} not found.");

        // Cannot remove the last Recruiting Leader
        if (member.Role == "Recruiting Leader" &&
            _members.Count(m => m.Role == "Recruiting Leader") <= 1)
        {
            throw new InvalidOperationException("Cannot remove the last member \u2014 at least one Recruiting Leader must exist.");
        }

        _members.Remove(member);
        AddDomainEvent(new MembershipChangedEvent(Id, member.UserId, "Removed"));
    }

    public void Close()
    {
        EnsureNotClosed();

        Status = RecruitmentStatus.Closed;
        ClosedAt = DateTimeOffset.UtcNow;
        AddDomainEvent(new RecruitmentClosedEvent(Id));
    }

    private void EnsureNotClosed()
    {
        if (Status == RecruitmentStatus.Closed)
        {
            throw new RecruitmentClosedException(Id);
        }
    }
}
