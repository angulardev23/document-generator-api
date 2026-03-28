using System.Globalization;
using System.Text.Json;

namespace DocumentGenerator.Infrastructure.Documents;

internal static class JsonTemplateModelConverter
{
    public static IReadOnlyDictionary<string, object?> Convert(JsonElement rootElement)
    {
        if (rootElement.ValueKind != JsonValueKind.Object)
        {
            throw new ArgumentException("Root element must be a JSON object.", nameof(rootElement));
        }

        return (IReadOnlyDictionary<string, object?>)ConvertValue(rootElement)!;
    }

    private static object? ConvertValue(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Object => ConvertObject(element),
            JsonValueKind.Array => element.EnumerateArray().Select(ConvertValue).ToList(),
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => ConvertNumber(element),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            JsonValueKind.Undefined => null,
            _ => element.GetRawText()
        };
    }

    private static Dictionary<string, object?> ConvertObject(JsonElement element)
    {
        var dictionary = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        foreach (var property in element.EnumerateObject())
        {
            dictionary[property.Name] = ConvertValue(property.Value);
        }

        return dictionary;
    }

    private static object ConvertNumber(JsonElement element)
    {
        if (element.TryGetInt32(out var int32Value))
        {
            return int32Value;
        }

        if (element.TryGetInt64(out var int64Value))
        {
            return int64Value;
        }

        if (element.TryGetDecimal(out var decimalValue))
        {
            return decimalValue;
        }

        return double.Parse(element.GetRawText(), CultureInfo.InvariantCulture);
    }
}

