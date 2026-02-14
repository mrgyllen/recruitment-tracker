namespace api.Application.Features.Import.Commands.ResolveMatchConflict;

public record ResolveMatchResultDto(
    int MatchIndex,
    string Action,
    string? CandidateEmail);
