namespace api.Application.Features.Recruitments.Queries.GetRecruitmentOverview;

public record RecruitmentOverviewDto
{
    public Guid RecruitmentId { get; init; }
    public int TotalCandidates { get; init; }
    public int PendingActionCount { get; init; }
    public int TotalStale { get; init; }
    public int StaleDays { get; init; }
    public List<WorkflowStepOverviewDto> Steps { get; init; } = [];
}

public record WorkflowStepOverviewDto
{
    public Guid StepId { get; init; }
    public string StepName { get; init; } = null!;
    public int StepOrder { get; init; }
    public int TotalCandidates { get; init; }
    public int PendingCount { get; init; }
    public int StaleCount { get; init; }
    public OutcomeBreakdownDto OutcomeBreakdown { get; init; } = new();
}

public record OutcomeBreakdownDto
{
    public int NotStarted { get; init; }
    public int Pass { get; init; }
    public int Fail { get; init; }
    public int Hold { get; init; }
}
