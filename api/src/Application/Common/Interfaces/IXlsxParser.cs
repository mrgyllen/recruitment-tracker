using api.Domain.ValueObjects;

namespace api.Application.Common.Interfaces;

public interface IXlsxParser
{
    List<ParsedCandidateRow> Parse(Stream xlsxStream);
}
