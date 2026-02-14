namespace api.Application.Features.Import.Commands.ResolveMatchConflict;

public record ResolveMatchConflictCommand(
    Guid ImportSessionId,
    int MatchIndex,
    string Action
) : IRequest<ResolveMatchResultDto>;
