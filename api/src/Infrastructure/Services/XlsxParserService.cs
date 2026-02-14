using api.Application.Common.Interfaces;
using api.Domain.ValueObjects;
using ClosedXML.Excel;
using Microsoft.Extensions.Options;

namespace api.Infrastructure.Services;

public class XlsxParserService(IOptions<XlsxColumnMappingOptions> options) : IXlsxParser
{
    private readonly XlsxColumnMappingOptions _mapping = options.Value;

    public List<ParsedCandidateRow> Parse(Stream xlsxStream)
    {
        using var workbook = new XLWorkbook(xlsxStream);
        var worksheet = workbook.Worksheets.First();

        var headerRow = worksheet.Row(1);
        var columnMap = ResolveColumnIndices(headerRow);

        ValidateRequiredColumns(columnMap);

        var results = new List<ParsedCandidateRow>();
        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 1;

        for (int rowNum = 2; rowNum <= lastRow; rowNum++)
        {
            var row = worksheet.Row(rowNum);

            var fullName = GetCellValue(row, columnMap, "FullName");
            var email = GetCellValue(row, columnMap, "Email");

            if (string.IsNullOrWhiteSpace(fullName) && string.IsNullOrWhiteSpace(email))
                continue; // Skip empty rows

            results.Add(new ParsedCandidateRow(
                RowNumber: rowNum,
                FullName: fullName?.Trim() ?? string.Empty,
                Email: email?.Trim() ?? string.Empty,
                PhoneNumber: GetCellValue(row, columnMap, "PhoneNumber")?.Trim(),
                Location: GetCellValue(row, columnMap, "Location")?.Trim(),
                DateApplied: ParseDate(GetCellValue(row, columnMap, "DateApplied"))));
        }

        return results;
    }

    private Dictionary<string, int> ResolveColumnIndices(IXLRow headerRow)
    {
        var map = new Dictionary<string, int>();
        var lastCol = headerRow.LastCellUsed()?.Address.ColumnNumber ?? 0;

        for (int col = 1; col <= lastCol; col++)
        {
            var headerValue = headerRow.Cell(col).GetString().Trim();
            if (string.IsNullOrEmpty(headerValue)) continue;

            if (_mapping.FullName.Contains(headerValue, StringComparer.OrdinalIgnoreCase))
                map.TryAdd("FullName", col);
            else if (_mapping.Email.Contains(headerValue, StringComparer.OrdinalIgnoreCase))
                map.TryAdd("Email", col);
            else if (_mapping.PhoneNumber.Contains(headerValue, StringComparer.OrdinalIgnoreCase))
                map.TryAdd("PhoneNumber", col);
            else if (_mapping.Location.Contains(headerValue, StringComparer.OrdinalIgnoreCase))
                map.TryAdd("Location", col);
            else if (_mapping.DateApplied.Contains(headerValue, StringComparer.OrdinalIgnoreCase))
                map.TryAdd("DateApplied", col);
        }

        return map;
    }

    private static void ValidateRequiredColumns(Dictionary<string, int> columnMap)
    {
        var missing = new List<string>();
        if (!columnMap.ContainsKey("FullName")) missing.Add("Full Name");
        if (!columnMap.ContainsKey("Email")) missing.Add("Email");

        if (missing.Count > 0)
        {
            throw new InvalidOperationException(
                $"Required columns not found in XLSX: {string.Join(", ", missing)}. " +
                "Check that your Workday export includes these columns.");
        }
    }

    private static string? GetCellValue(IXLRow row, Dictionary<string, int> columnMap, string field)
    {
        if (!columnMap.TryGetValue(field, out var colIndex))
            return null;

        var cell = row.Cell(colIndex);
        return cell.IsEmpty() ? null : cell.GetString();
    }

    private static DateTimeOffset? ParseDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return DateTimeOffset.TryParse(value, out var date) ? date : null;
    }
}
