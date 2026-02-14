using api.Infrastructure.Services;
using ClosedXML.Excel;
using FluentAssertions;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace api.Application.UnitTests.Features.Import.Services;

[TestFixture]
public class XlsxParserServiceTests
{
    private XlsxParserService _parser = null!;

    [SetUp]
    public void SetUp()
    {
        var options = Options.Create(new XlsxColumnMappingOptions());
        _parser = new XlsxParserService(options);
    }

    [Test]
    public void Parse_ValidFile_ExtractsAllRows()
    {
        using var stream = CreateXlsx(
            ["Full Name", "Email", "Phone", "Location", "Date Applied"],
            ["Alice Johnson", "alice@example.com", "+1234567890", "Oslo", "2025-01-15"],
            ["Bob Smith", "bob@example.com", "+0987654321", "London", "2025-02-01"]);

        var result = _parser.Parse(stream);

        result.Should().HaveCount(2);
        result[0].FullName.Should().Be("Alice Johnson");
        result[0].Email.Should().Be("alice@example.com");
        result[0].PhoneNumber.Should().Be("+1234567890");
        result[0].Location.Should().Be("Oslo");
        result[0].DateApplied.Should().NotBeNull();
        result[0].RowNumber.Should().Be(2);
        result[1].FullName.Should().Be("Bob Smith");
        result[1].RowNumber.Should().Be(3);
    }

    [Test]
    public void Parse_MissingRequiredColumns_ThrowsWithClearMessage()
    {
        using var stream = CreateXlsx(
            ["Name", "Phone Number"],
            ["Alice", "+123"]);

        // "Name" maps to FullName via default mapping, but "Email" is missing
        var act = () => _parser.Parse(stream);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Email*");
    }

    [Test]
    public void Parse_EmptyFile_ReturnsEmptyList()
    {
        using var stream = CreateXlsx(["Full Name", "Email"]);

        var result = _parser.Parse(stream);

        result.Should().BeEmpty();
    }

    [Test]
    public void Parse_AlternateColumnNames_ResolvesViaMapping()
    {
        using var stream = CreateXlsx(
            ["Candidate Name", "Email Address", "Tel", "City", "Application Date"],
            ["Alice Johnson", "alice@example.com", "+123", "Oslo", "2025-01-15"]);

        var result = _parser.Parse(stream);

        result.Should().HaveCount(1);
        result[0].FullName.Should().Be("Alice Johnson");
        result[0].Email.Should().Be("alice@example.com");
        result[0].PhoneNumber.Should().Be("+123");
        result[0].Location.Should().Be("Oslo");
    }

    [Test]
    public void Parse_SkipsBlankRows()
    {
        using var stream = CreateXlsx(
            ["Full Name", "Email"],
            ["Alice", "alice@example.com"],
            ["", ""],
            ["Bob", "bob@example.com"]);

        var result = _parser.Parse(stream);

        result.Should().HaveCount(2);
        result[0].FullName.Should().Be("Alice");
        result[1].FullName.Should().Be("Bob");
    }

    [Test]
    public void Parse_TrimsWhitespace()
    {
        using var stream = CreateXlsx(
            ["Full Name", "Email"],
            ["  Alice Johnson  ", "  alice@example.com  "]);

        var result = _parser.Parse(stream);

        result.Should().HaveCount(1);
        result[0].FullName.Should().Be("Alice Johnson");
        result[0].Email.Should().Be("alice@example.com");
    }

    [Test]
    public void Parse_MissingBothRequiredColumns_ListsBothInError()
    {
        using var stream = CreateXlsx(
            ["Phone", "Location"],
            ["+123", "Oslo"]);

        var act = () => _parser.Parse(stream);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Full Name*Email*");
    }

    private static MemoryStream CreateXlsx(string[] headers, params string[][] rows)
    {
        var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Sheet1");

        for (int col = 0; col < headers.Length; col++)
        {
            ws.Cell(1, col + 1).Value = headers[col];
        }

        for (int rowIdx = 0; rowIdx < rows.Length; rowIdx++)
        {
            for (int col = 0; col < rows[rowIdx].Length; col++)
            {
                ws.Cell(rowIdx + 2, col + 1).Value = rows[rowIdx][col];
            }
        }

        var stream = new MemoryStream();
        wb.SaveAs(stream);
        stream.Position = 0;
        return stream;
    }
}
