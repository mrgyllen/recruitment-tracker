using api.Application.Features.Team.Queries.SearchDirectory;
using FluentAssertions;
using NUnit.Framework;

namespace api.Application.UnitTests.Features.Team.Queries.SearchDirectory;

[TestFixture]
public class SearchDirectoryQueryValidatorTests
{
    private SearchDirectoryQueryValidator _validator = null!;

    [SetUp]
    public void SetUp()
    {
        _validator = new SearchDirectoryQueryValidator();
    }

    [Test]
    public void Validate_EmptySearchTerm_Fails()
    {
        var query = new SearchDirectoryQuery { SearchTerm = "" };
        var result = _validator.Validate(query);
        result.IsValid.Should().BeFalse();
    }

    [Test]
    public void Validate_SingleCharSearchTerm_Fails()
    {
        var query = new SearchDirectoryQuery { SearchTerm = "a" };
        var result = _validator.Validate(query);
        result.IsValid.Should().BeFalse();
    }

    [Test]
    public void Validate_ValidSearchTerm_Passes()
    {
        var query = new SearchDirectoryQuery { SearchTerm = "erik" };
        var result = _validator.Validate(query);
        result.IsValid.Should().BeTrue();
    }

    [Test]
    public void Validate_SearchTermExceedsMaxLength_Fails()
    {
        var query = new SearchDirectoryQuery { SearchTerm = new string('a', 101) };
        var result = _validator.Validate(query);
        result.IsValid.Should().BeFalse();
    }
}
