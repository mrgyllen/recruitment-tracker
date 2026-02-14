namespace api.Application.Features.Team.Queries.SearchDirectory;

public class SearchDirectoryQueryValidator : AbstractValidator<SearchDirectoryQuery>
{
    public SearchDirectoryQueryValidator()
    {
        RuleFor(x => x.SearchTerm)
            .NotEmpty().WithMessage("Search term is required.")
            .MinimumLength(2).WithMessage("Search term must be at least 2 characters.")
            .MaximumLength(100);
    }
}
