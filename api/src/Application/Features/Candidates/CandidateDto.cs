using api.Application.Features.Candidates.Commands;
using api.Domain.Entities;

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

    public static CandidateDto From(Candidate candidate) => new()
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
    };
}
