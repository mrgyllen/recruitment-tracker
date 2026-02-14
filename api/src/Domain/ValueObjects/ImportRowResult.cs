using api.Domain.Enums;

namespace api.Domain.ValueObjects;

public sealed class ImportRowResult
{
    public int RowNumber { get; private set; }
    public string? CandidateEmail { get; private set; }
    public ImportRowAction Action { get; private set; }
    public string? ErrorMessage { get; private set; }
    public string? Resolution { get; private set; }

    private ImportRowResult() { } // EF Core

    public ImportRowResult(int rowNumber, string? candidateEmail, ImportRowAction action, string? errorMessage)
    {
        RowNumber = rowNumber;
        CandidateEmail = candidateEmail;
        Action = action;
        ErrorMessage = errorMessage;
    }

    public void Confirm()
    {
        if (Action != ImportRowAction.Flagged)
            throw new InvalidOperationException("Only flagged rows can be confirmed.");
        if (Resolution is not null)
            throw new InvalidOperationException("This match has already been resolved.");
        Resolution = "Confirmed";
    }

    public void Reject()
    {
        if (Action != ImportRowAction.Flagged)
            throw new InvalidOperationException("Only flagged rows can be rejected.");
        if (Resolution is not null)
            throw new InvalidOperationException("This match has already been resolved.");
        Resolution = "Rejected";
    }
}
