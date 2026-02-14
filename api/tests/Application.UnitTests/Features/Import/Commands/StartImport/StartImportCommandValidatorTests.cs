using api.Application.Features.Import.Commands.StartImport;
using FluentAssertions;
using FluentValidation.TestHelper;
using NUnit.Framework;

namespace api.Application.UnitTests.Features.Import.Commands.StartImport;

[TestFixture]
public class StartImportCommandValidatorTests
{
    private StartImportCommandValidator _validator = null!;

    [SetUp]
    public void SetUp()
    {
        _validator = new StartImportCommandValidator();
    }

    [Test]
    public void ValidCommand_Passes()
    {
        var command = new StartImportCommand(
            Guid.NewGuid(),
            new byte[100],
            "export.xlsx",
            100);

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public void EmptyRecruitmentId_Fails()
    {
        var command = new StartImportCommand(
            Guid.Empty,
            new byte[100],
            "export.xlsx",
            100);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.RecruitmentId);
    }

    [Test]
    public void EmptyFileContent_Fails()
    {
        var command = new StartImportCommand(
            Guid.NewGuid(),
            Array.Empty<byte>(),
            "export.xlsx",
            0);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.FileContent);
    }

    [Test]
    public void InvalidExtension_Fails()
    {
        var command = new StartImportCommand(
            Guid.NewGuid(),
            new byte[100],
            "export.csv",
            100);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.FileName);
    }

    [Test]
    public void OversizedFile_Fails()
    {
        var command = new StartImportCommand(
            Guid.NewGuid(),
            new byte[100],
            "export.xlsx",
            11 * 1024 * 1024); // 11 MB

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.FileSize);
    }

    [Test]
    public void EmptyFileName_Fails()
    {
        var command = new StartImportCommand(
            Guid.NewGuid(),
            new byte[100],
            "",
            100);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.FileName);
    }
}
