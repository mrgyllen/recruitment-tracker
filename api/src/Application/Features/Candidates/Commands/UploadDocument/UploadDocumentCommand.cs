namespace api.Application.Features.Candidates.Commands.UploadDocument;

public record UploadDocumentCommand(
    Guid RecruitmentId,
    Guid CandidateId,
    Stream FileStream,
    string FileName,
    long FileSize) : IRequest<DocumentDto>;
