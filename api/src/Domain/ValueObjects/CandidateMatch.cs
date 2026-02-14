using api.Domain.Enums;

namespace api.Domain.ValueObjects;

public sealed record CandidateMatch(ImportMatchConfidence Confidence, string MatchMethod, Guid? MatchedCandidateId = null);
