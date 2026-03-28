using System.Text.RegularExpressions;
using DocumentGenerator.Application.Exceptions;

namespace DocumentGenerator.Application.Documents;

internal static partial class TemplateProcessingErrorParser
{
    [GeneratedRegex(@"'(?<placeholder>\{\{[^}]+\}\})' could not be replaced", RegexOptions.CultureInvariant)]
    private static partial Regex PlaceholderRegex();

    public static bool TryParse(Exception exception, out IReadOnlyCollection<ValidationError> errors)
    {
        var placeholder = TryExtractPlaceholder(exception.Message)
            ?? TryExtractPlaceholder(exception.InnerException?.Message);

        if (placeholder is null)
        {
            errors = Array.Empty<ValidationError>();
            return false;
        }

        var path = ExtractPlaceholderPath(placeholder);
        if (string.IsNullOrWhiteSpace(path))
        {
            errors = Array.Empty<ValidationError>();
            return false;
        }

        errors =
        [
            new ValidationError(
                $"data.{path}",
                $"Missing data for placeholder '{placeholder}'.")
        ];

        return true;
    }

    private static string? TryExtractPlaceholder(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return null;
        }

        var match = PlaceholderRegex().Match(message);
        return match.Success ? match.Groups["placeholder"].Value : null;
    }

    private static string ExtractPlaceholderPath(string placeholder)
    {
        var trimmed = placeholder.Trim();

        if (!trimmed.StartsWith("{{", StringComparison.Ordinal) ||
            !trimmed.EndsWith("}}", StringComparison.Ordinal))
        {
            return string.Empty;
        }

        return trimmed[2..^2].Trim();
    }
}
