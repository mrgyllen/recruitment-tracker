using api.Application.Common.Interfaces;
using api.Domain.Entities;
using api.Domain.Enums;
using api.Domain.ValueObjects;

namespace api.Infrastructure.Services;

public class CandidateMatchingEngine : ICandidateMatchingEngine
{
    public CandidateMatch Match(ParsedCandidateRow row, IReadOnlyList<Candidate> existingCandidates)
    {
        // Primary match: email (case-insensitive, high confidence)
        var emailMatch = existingCandidates.FirstOrDefault(c =>
            !string.IsNullOrEmpty(c.Email) &&
            c.Email.Equals(row.Email, StringComparison.OrdinalIgnoreCase));

        if (emailMatch is not null)
        {
            return new CandidateMatch(ImportMatchConfidence.High, "Email", emailMatch.Id);
        }

        // Fallback match: name + phone (low confidence)
        if (!string.IsNullOrWhiteSpace(row.PhoneNumber))
        {
            var namePhoneMatch = existingCandidates.FirstOrDefault(c =>
                !string.IsNullOrEmpty(c.FullName) &&
                !string.IsNullOrEmpty(c.PhoneNumber) &&
                c.FullName.Equals(row.FullName, StringComparison.OrdinalIgnoreCase) &&
                c.PhoneNumber.Equals(row.PhoneNumber, StringComparison.OrdinalIgnoreCase));

            if (namePhoneMatch is not null)
            {
                return new CandidateMatch(ImportMatchConfidence.Low, "NameAndPhone", namePhoneMatch.Id);
            }
        }

        return new CandidateMatch(ImportMatchConfidence.None, "None");
    }
}
