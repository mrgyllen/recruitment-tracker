using api.Domain.Common;
using api.Domain.Enums;
using api.Domain.Events;

namespace api.Domain.Entities;

public class Candidate : GuidEntity
{
    public Guid RecruitmentId { get; private set; }
    public string? FullName { get; private set; }
    public string? Email { get; private set; }
    public string? PhoneNumber { get; private set; }
    public string? Location { get; private set; }
    public DateTimeOffset DateApplied { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private readonly List<CandidateOutcome> _outcomes = new();
    public IReadOnlyCollection<CandidateOutcome> Outcomes => _outcomes.AsReadOnly();

    private readonly List<CandidateDocument> _documents = new();
    public IReadOnlyCollection<CandidateDocument> Documents => _documents.AsReadOnly();

    private Candidate() { } // EF Core

    public static Candidate Create(
        Guid recruitmentId,
        string fullName,
        string email,
        string? phoneNumber,
        string? location,
        DateTimeOffset dateApplied)
    {
        var candidate = new Candidate
        {
            RecruitmentId = recruitmentId,
            FullName = fullName,
            Email = email,
            PhoneNumber = phoneNumber,
            Location = location,
            DateApplied = dateApplied,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        candidate.AddDomainEvent(new CandidateImportedEvent(candidate.Id, recruitmentId));
        return candidate;
    }

    public void RecordOutcome(Guid workflowStepId, OutcomeStatus status, Guid recordedByUserId)
    {
        var outcome = CandidateOutcome.Create(Id, workflowStepId, status, recordedByUserId);
        _outcomes.Add(outcome);
        AddDomainEvent(new OutcomeRecordedEvent(Id, workflowStepId));
    }

    public void AttachDocument(string documentType, string blobStorageUrl)
    {
        if (_documents.Any(d => d.DocumentType.Equals(documentType, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException(
                $"A document of type '{documentType}' already exists for this candidate.");
        }

        var document = CandidateDocument.Create(Id, documentType, blobStorageUrl);
        _documents.Add(document);
        AddDomainEvent(new DocumentUploadedEvent(Id, document.Id));
    }

    public string? ReplaceDocument(
        string documentType,
        string newBlobStorageUrl,
        string? workdayCandidateId = null,
        DocumentSource documentSource = DocumentSource.IndividualUpload)
    {
        var existing = _documents.FirstOrDefault(
            d => d.DocumentType.Equals(documentType, StringComparison.OrdinalIgnoreCase));

        string? oldBlobUrl = null;

        if (existing is not null)
        {
            oldBlobUrl = existing.BlobStorageUrl;
            _documents.Remove(existing);
        }

        var newDoc = CandidateDocument.Create(Id, documentType, newBlobStorageUrl, workdayCandidateId, documentSource);
        _documents.Add(newDoc);
        AddDomainEvent(new DocumentUploadedEvent(Id, newDoc.Id));

        return oldBlobUrl;
    }

    public void UpdateProfile(string fullName, string? phoneNumber, string? location, DateTimeOffset dateApplied)
    {
        FullName = fullName;
        PhoneNumber = phoneNumber;
        Location = location;
        DateApplied = dateApplied;
    }

    public void Anonymize()
    {
        FullName = null;
        Email = null;
        PhoneNumber = null;
        Location = null;
    }
}
