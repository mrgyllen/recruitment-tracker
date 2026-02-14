using api.Domain.Enums;

namespace api.Domain.ValueObjects;

public sealed record ImportRowResult(
    int RowNumber,
    string? CandidateEmail,
    ImportRowAction Action,
    string? ErrorMessage);
