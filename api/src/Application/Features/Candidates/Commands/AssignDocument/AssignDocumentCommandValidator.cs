namespace api.Application.Features.Candidates.Commands.AssignDocument;

public class AssignDocumentCommandValidator : AbstractValidator<AssignDocumentCommand>
{
    public AssignDocumentCommandValidator()
    {
        RuleFor(x => x.RecruitmentId).NotEmpty();
        RuleFor(x => x.CandidateId).NotEmpty();
        RuleFor(x => x.DocumentBlobUrl)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .Must((cmd, url) => url.StartsWith($"{cmd.RecruitmentId}/", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Document URL must belong to the target recruitment's storage path.");
        RuleFor(x => x.DocumentName).NotEmpty();
    }
}
