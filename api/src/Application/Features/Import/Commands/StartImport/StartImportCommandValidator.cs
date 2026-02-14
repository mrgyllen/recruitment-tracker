namespace api.Application.Features.Import.Commands.StartImport;

public class StartImportCommandValidator : AbstractValidator<StartImportCommand>
{
    private const long MaxXlsxFileSize = 10 * 1024 * 1024; // 10 MB
    private const long MaxPdfFileSize = 100 * 1024 * 1024; // 100 MB (AC1)

    public StartImportCommandValidator()
    {
        RuleFor(x => x.RecruitmentId).NotEmpty();
        RuleFor(x => x.FileContent).NotEmpty().WithMessage("File content is required.");
        RuleFor(x => x.FileName)
            .NotEmpty().WithMessage("File name is required.")
            .Must(name =>
                name.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase) ||
                name.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Only .xlsx and .pdf files are supported.");
        RuleFor(x => x.FileSize)
            .Must((cmd, size) =>
            {
                if (cmd.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                    return size <= MaxPdfFileSize;
                return size <= MaxXlsxFileSize;
            })
            .WithMessage("File size exceeds maximum allowed.");
    }
}
