using api.Domain.Entities;
using api.Domain.ValueObjects;

namespace api.Application.Common.Interfaces;

public interface ICandidateMatchingEngine
{
    CandidateMatch Match(ParsedCandidateRow row, IReadOnlyList<Candidate> existingCandidates);
}
