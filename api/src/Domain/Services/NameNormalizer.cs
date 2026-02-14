using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace api.Domain.Services;

public static partial class NameNormalizer
{
    public static string Normalize(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return string.Empty;

        var result = name.Trim();
        result = result.ToLowerInvariant();
        result = RemoveDiacritics(result);
        result = MultipleSpaces().Replace(result, " ");

        return result;
    }

    private static string RemoveDiacritics(string text)
    {
        var normalized = text.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalized.Length);

        foreach (var c in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(c);
            if (category != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }

        return sb.ToString().Normalize(NormalizationForm.FormC);
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex MultipleSpaces();
}
