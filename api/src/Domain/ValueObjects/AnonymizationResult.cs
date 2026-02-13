namespace api.Domain.ValueObjects;

public sealed record AnonymizationResult(int CandidatesAnonymized, int DocumentsDeleted);
