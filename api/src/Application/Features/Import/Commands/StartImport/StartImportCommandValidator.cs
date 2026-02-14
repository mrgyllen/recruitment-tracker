namespace api.Application.Features.Import.Commands.StartImport;

public class StartImportCommandValidator : AbstractValidator<StartImportCommand>
{
    private const long MaxFileSize = 10 * 1024 * 1024; // 10 MB

    public StartImportCommandValidator()
    {
        RuleFor(x => x.RecruitmentId).NotEmpty();
        RuleFor(x => x.FileContent).NotEmpty().WithMessage("File content is required.");
        RuleFor(x => x.FileName)
            .NotEmpty().WithMessage("File name is required.")
            .Must(name => name.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Only .xlsx files are supported.");
        RuleFor(x => x.FileSize)
            .LessThanOrEqualTo(MaxFileSize)
            .WithMessage("File size must not exceed 10 MB.");
    }
}
