using api.Domain.Enums;

namespace api.Domain.Models;

public record DocumentMatchResult(
    SplitDocument Document,
    Guid? MatchedCandidateId,
    ImportDocumentMatchStatus Status);
