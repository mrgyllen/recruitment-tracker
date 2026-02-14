namespace api.Application.Features.Import.Commands.ProcessPdfBundle;

public record ProcessPdfBundleCommand(
    Guid ImportSessionId,
    Guid RecruitmentId,
    Stream PdfStream) : IRequest;
