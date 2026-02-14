using api.Application.Features.Candidates.Commands;
using api.Domain.Entities;
using api.Domain.Enums;

namespace api.Application.Features.Candidates;

public record CandidateDto
{
    public Guid Id { get; init; }
    public Guid RecruitmentId { get; init; }
    public string FullName { get; init; } = null!;
    public string Email { get; init; } = null!;
    public string? PhoneNumber { get; init; }
    public string? Location { get; init; }
    public DateTimeOffset DateApplied { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DocumentDto? Document { get; init; }
    public Guid? CurrentWorkflowStepId { get; init; }
    public string? CurrentWorkflowStepName { get; init; }
    public string? CurrentOutcomeStatus { get; init; }

    public static CandidateDto From(
        Candidate candidate,
        IReadOnlyList<WorkflowStep> workflowSteps)
    {
        var (currentStep, outcomeStatus) = ComputeCurrentStep(candidate, workflowSteps);

        return new()
        {
            Id = candidate.Id,
            RecruitmentId = candidate.RecruitmentId,
            FullName = candidate.FullName!,
            Email = candidate.Email!,
            PhoneNumber = candidate.PhoneNumber,
            Location = candidate.Location,
            DateApplied = candidate.DateApplied,
            CreatedAt = candidate.CreatedAt,
            Document = candidate.Documents.FirstOrDefault() is { } doc
                ? DocumentDto.From(doc)
                : null,
            CurrentWorkflowStepId = currentStep?.Id,
            CurrentWorkflowStepName = currentStep?.Name,
            CurrentOutcomeStatus = outcomeStatus?.ToString(),
        };
    }

    public static CandidateDto From(Candidate candidate) =>
        From(candidate, Array.Empty<WorkflowStep>());

    internal static (WorkflowStep? step, OutcomeStatus? status) ComputeCurrentStep(
        Candidate candidate,
        IReadOnlyList<WorkflowStep> steps)
    {
        if (steps.Count == 0)
            return (null, null);

        var orderedSteps = steps.OrderBy(s => s.Order).ToList();

        if (candidate.Outcomes.Count == 0)
            return (orderedSteps[0], OutcomeStatus.NotStarted);

        // Find the latest outcome by step order (highest step order with an outcome)
        var latestOutcome = candidate.Outcomes
            .OrderByDescending(o => orderedSteps.FindIndex(s => s.Id == o.WorkflowStepId))
            .ThenByDescending(o => o.RecordedAt)
            .First();

        var currentStepIndex = orderedSteps.FindIndex(s => s.Id == latestOutcome.WorkflowStepId);

        if (latestOutcome.Status == OutcomeStatus.Pass
            && currentStepIndex < orderedSteps.Count - 1)
        {
            // Passed: advance to next step
            return (orderedSteps[currentStepIndex + 1], OutcomeStatus.NotStarted);
        }

        // Fail, Hold, or passed last step: stay at current step
        return (orderedSteps[currentStepIndex], latestOutcome.Status);
    }
}
