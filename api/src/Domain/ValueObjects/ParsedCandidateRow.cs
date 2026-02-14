namespace api.Domain.ValueObjects;

public sealed record ParsedCandidateRow(
    int RowNumber,
    string FullName,
    string Email,
    string? PhoneNumber,
    string? Location,
    DateTimeOffset? DateApplied);
