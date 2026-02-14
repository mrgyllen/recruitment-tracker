namespace api.Application.Features.Import.Commands.StartImport;

public record StartImportCommand(
    Guid RecruitmentId,
    byte[] FileContent,
    string FileName,
    long FileSize) : IRequest<StartImportResponse>;
