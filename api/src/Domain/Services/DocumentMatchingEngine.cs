using api.Domain.Entities;
using api.Domain.Enums;
using api.Domain.Models;

namespace api.Domain.Services;

public class DocumentMatchingEngine
{
    public IReadOnlyList<DocumentMatchResult> MatchDocumentsToCandidates(
        IReadOnlyList<SplitDocument> documents,
        IReadOnlyList<Candidate> candidates)
    {
        var candidateLookup = candidates
            .GroupBy(c => NameNormalizer.Normalize(c.FullName))
            .ToDictionary(g => g.Key, g => g.ToList());

        var results = new List<DocumentMatchResult>();

        foreach (var doc in documents)
        {
            var normalizedDocName = NameNormalizer.Normalize(doc.CandidateName);

            if (candidateLookup.TryGetValue(normalizedDocName, out var matches)
                && matches.Count == 1)
            {
                results.Add(new DocumentMatchResult(
                    doc, matches[0].Id, ImportDocumentMatchStatus.AutoMatched));
            }
            else
            {
                results.Add(new DocumentMatchResult(
                    doc, null, ImportDocumentMatchStatus.Unmatched));
            }
        }

        return results;
    }
}
