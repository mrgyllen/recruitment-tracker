namespace api.Application.Features.Candidates.Queries.GetCandidateById;

public record CandidateDetailDto
{
    public Guid Id { get; init; }
    public Guid RecruitmentId { get; init; }
    public string FullName { get; init; } = null!;
    public string Email { get; init; } = null!;
    public string? PhoneNumber { get; init; }
    public string? Location { get; init; }
    public DateTimeOffset DateApplied { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public Guid? CurrentWorkflowStepId { get; init; }
    public string? CurrentWorkflowStepName { get; init; }
    public string? CurrentOutcomeStatus { get; init; }
    public List<DocumentDetailDto> Documents { get; init; } = [];
    public List<OutcomeHistoryDto> OutcomeHistory { get; init; } = [];
}

public record DocumentDetailDto
{
    public Guid Id { get; init; }
    public string DocumentType { get; init; } = null!;
    public string SasUrl { get; init; } = null!;
    public DateTimeOffset UploadedAt { get; init; }
}

public record OutcomeHistoryDto
{
    public Guid WorkflowStepId { get; init; }
    public string WorkflowStepName { get; init; } = null!;
    public int StepOrder { get; init; }
    public string Status { get; init; } = null!;
    public DateTimeOffset RecordedAt { get; init; }
    public Guid RecordedByUserId { get; init; }
}
