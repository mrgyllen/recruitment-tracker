namespace api.Application.Features.Candidates.Commands.UploadDocument;

public class UploadDocumentCommandValidator : AbstractValidator<UploadDocumentCommand>
{
    private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB

    public UploadDocumentCommandValidator()
    {
        RuleFor(x => x.RecruitmentId).NotEmpty();
        RuleFor(x => x.CandidateId).NotEmpty();
        RuleFor(x => x.FileName)
            .NotEmpty()
            .Must(f => f.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Only PDF files are accepted.");
        RuleFor(x => x.FileSize)
            .LessThanOrEqualTo(MaxFileSizeBytes)
            .WithMessage("File size must not exceed 10 MB.");
    }
}
