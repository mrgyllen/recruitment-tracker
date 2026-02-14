namespace api.Application.Features.Import.Queries.GetImportSession;

public record GetImportSessionQuery(Guid Id) : IRequest<ImportSessionDto>;
