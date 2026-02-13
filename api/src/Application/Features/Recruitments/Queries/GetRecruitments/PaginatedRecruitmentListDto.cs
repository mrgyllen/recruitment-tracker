using api.Domain.Entities;

namespace api.Application.Features.Recruitments.Queries.GetRecruitments;

public record PaginatedRecruitmentListDto
{
    public List<RecruitmentListItemDto> Items { get; init; } = [];
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
}

public record RecruitmentListItemDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = null!;
    public string? Description { get; init; }
    public string Status { get; init; } = null!;
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? ClosedAt { get; init; }
    public int StepCount { get; init; }
    public int MemberCount { get; init; }

    public static RecruitmentListItemDto From(Recruitment entity) => new()
    {
        Id = entity.Id,
        Title = entity.Title,
        Description = entity.Description,
        Status = entity.Status.ToString(),
        CreatedAt = entity.CreatedAt,
        ClosedAt = entity.ClosedAt,
        StepCount = entity.Steps.Count,
        MemberCount = entity.Members.Count,
    };
}
