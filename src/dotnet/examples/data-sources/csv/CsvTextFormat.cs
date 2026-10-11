using System.Globalization;
using System.Text;
using Musoq.Schema;

namespace Musoq.Examples.DataSources.Csv;

internal static class CsvTextFormat
{
    internal const string SourceName = "csv.file";
    internal static char ResolveDelimiter(string delimiter)
    {
        if (string.IsNullOrEmpty(delimiter))
            throw new ArgumentException("CSV delimiter cannot be empty.", nameof(delimiter));

        if (delimiter.Length != 1)
            throw new ArgumentException("CSV delimiter must be a single character.", nameof(delimiter));

        return delimiter[0] switch
        {
            '"' or '\r' or '\n' => throw new ArgumentException("CSV delimiter cannot be a quote or newline.", nameof(delimiter)),
            var value => value
        };
    }

    internal static Encoding ResolveEncoding(string? encoding)
    {
        if (string.IsNullOrWhiteSpace(encoding) ||
            string.Equals(encoding, "utf-8", StringComparison.OrdinalIgnoreCase))
        {
            return new UTF8Encoding(false, true);
        }

        if (string.Equals(encoding, "utf-16le", StringComparison.OrdinalIgnoreCase))
            return Encoding.Unicode;

        if (string.Equals(encoding, "utf-16be", StringComparison.OrdinalIgnoreCase))
            return Encoding.BigEndianUnicode;

        return Encoding.GetEncoding(encoding);
    }

    internal static void SkipRecords(
        IEnumerator<string[]> records,
        int skipRows,
        CancellationToken token)
    {
        for (var index = 0; index < skipRows; index++)
        {
            token.ThrowIfCancellationRequested();
            if (!records.MoveNext())
                return;
        }
    }

    internal static IEnumerable<string[]> ReadRecords(
        TextReader reader,
        char delimiter,
        CancellationToken token)
    {
        var fields = new List<string>();
        var field = new StringBuilder();
        var inQuotes = false;
        var hasAnyData = false;

        while (true)
        {
            token.ThrowIfCancellationRequested();
            var value = reader.Read();
            if (value < 0)
            {
                if (inQuotes)
                    throw new Musoq.Schema.Diagnostics.DataSourceInputException(Musoq.Schema.Diagnostics.DataSourceInputFailureReason.UnterminatedQuotedField);

                if (hasAnyData || field.Length > 0 || fields.Count > 0)
                {
                    fields.Add(field.ToString());
                    yield return fields.ToArray();
                }

                yield break;
            }

            var character = (char)value;
            hasAnyData = true;

            if (inQuotes)
            {
                if (character == '"')
                {
                    if (reader.Peek() == '"')
                    {
                        _ = reader.Read();
                        field.Append('"');
                        continue;
                    }

                    inQuotes = false;
                    continue;
                }

                field.Append(character);
                continue;
            }

            if (character == '"' && field.Length == 0)
            {
                inQuotes = true;
                continue;
            }

            if (character == delimiter)
            {
                fields.Add(field.ToString());
                field.Clear();
                continue;
            }

            if (character == '\r')
            {
                if (reader.Peek() == '\n')
                    _ = reader.Read();

                yield return CompleteRecord(fields, field);
                hasAnyData = false;
                continue;
            }

            if (character == '\n')
            {
                yield return CompleteRecord(fields, field);
                hasAnyData = false;
                continue;
            }

            field.Append(character);
        }
    }

    private static string[] CompleteRecord(List<string> fields, StringBuilder field)
    {
        fields.Add(field.ToString());
        field.Clear();

        var record = fields.ToArray();
        fields.Clear();
        return record;
    }

    internal static DateTime ParseDateTime(
        string text,
        IReadOnlyDictionary<string, string> modifiers,
        CultureInfo culture)
    {
        return modifiers.TryGetValue(ColumnReadModifiers.Format, out var format)
            ? DateTime.ParseExact(text, format, culture)
            : DateTime.Parse(text, culture);
    }

    internal static DateTimeOffset ParseDateTimeOffset(
        string text,
        IReadOnlyDictionary<string, string> modifiers,
        CultureInfo culture)
    {
        return modifiers.TryGetValue(ColumnReadModifiers.Format, out var format)
            ? DateTimeOffset.ParseExact(text, format, culture)
            : DateTimeOffset.Parse(text, culture);
    }

    internal static TimeSpan ParseTimeSpan(
        string text,
        IReadOnlyDictionary<string, string> modifiers,
        CultureInfo culture)
    {
        return modifiers.TryGetValue(ColumnReadModifiers.Format, out var format)
            ? TimeSpan.ParseExact(text, format, culture)
            : TimeSpan.Parse(text, culture);
    }

    internal static CultureInfo ResolveCulture(IReadOnlyDictionary<string, string> modifiers)
    {
        return modifiers.TryGetValue(ColumnReadModifiers.Culture, out var culture)
            ? CultureInfo.GetCultureInfo(culture)
            : CultureInfo.InvariantCulture;
    }

}
