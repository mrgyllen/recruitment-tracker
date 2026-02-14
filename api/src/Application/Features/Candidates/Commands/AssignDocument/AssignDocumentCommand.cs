namespace api.Application.Features.Candidates.Commands.AssignDocument;

public record AssignDocumentCommand(
    Guid RecruitmentId,
    Guid CandidateId,
    string DocumentBlobUrl,
    string DocumentName,
    Guid? ImportSessionId) : IRequest<DocumentDto>;
