using System.Globalization;

namespace Musoq.Schema.StructuralInputs;

/// <summary>Formats constant values using the canonical Core SQL literal spelling.</summary>
public static class StructuralSqlLiteralFormatter
{
    /// <summary>Returns canonical SQL text for a scalar default value.</summary>
    public static string Format(object? value)
    {
        return value switch
        {
            null => "null",
            string text => Quote(text),
            char character => Quote(character.ToString()),
            bool flag => flag ? "true" : "false",
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture) ?? string.Empty,
            _ => value.ToString() ?? string.Empty
        };
    }

    private static string Quote(string value) => $"'{Escape(value)}'";

    private static string Escape(string value) => value
        .Replace("\\", "\\\\", StringComparison.Ordinal)
        .Replace("'", "\\'", StringComparison.Ordinal)
        .Replace("\r", "\\r", StringComparison.Ordinal)
        .Replace("\n", "\\n", StringComparison.Ordinal)
        .Replace("\t", "\\t", StringComparison.Ordinal)
        .Replace("\b", "\\b", StringComparison.Ordinal)
        .Replace("\f", "\\f", StringComparison.Ordinal)
        .Replace("\0", "\\0", StringComparison.Ordinal);
}
