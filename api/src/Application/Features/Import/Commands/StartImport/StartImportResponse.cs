namespace api.Application.Features.Import.Commands.StartImport;

public record StartImportResponse(
    Guid ImportSessionId,
    string StatusUrl);
