namespace api.Application.Features.Candidates.Commands.AssignDocument;

public class AssignDocumentCommandValidator : AbstractValidator<AssignDocumentCommand>
{
    public AssignDocumentCommandValidator()
    {
        RuleFor(x => x.RecruitmentId).NotEmpty();
        RuleFor(x => x.CandidateId).NotEmpty();
        RuleFor(x => x.DocumentBlobUrl).NotEmpty();
        RuleFor(x => x.DocumentName).NotEmpty();
    }
}
