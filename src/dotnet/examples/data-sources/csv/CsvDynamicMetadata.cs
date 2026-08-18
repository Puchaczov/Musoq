using Musoq.Schema;
using Musoq.Schema.DataSources;
using System.Text;

namespace Musoq.Examples.DataSources.Csv;

internal static class CsvDynamicMetadata
{
    public static ISchemaColumn[] Discover(IReadOnlyList<object?> parameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        try
        {
            var options = CsvSourceOptions.FromParameters(parameters);
            if (string.IsNullOrWhiteSpace(options.Path) || !File.Exists(options.Path))
                return [];

            var delimiter = CsvFileSource.ResolveDelimiter(options.Delimiter);
            using var stream = File.OpenRead(options.Path);
            using var reader = new StreamReader(
                stream,
                CsvFileSource.ResolveEncoding((string?)null),
                detectEncodingFromByteOrderMarks: true);
            using var records = CsvFileSource.ReadRecords(reader, delimiter, CancellationToken.None).GetEnumerator();

            CsvFileSource.SkipRecords(records, options.SkipRows, CancellationToken.None);
            if (!records.MoveNext())
                return [];

            var firstRecord = records.Current;
            var names = options.HasHeader
                ? CreateHeaderNames(firstRecord)
                : CreateOrdinalNames(firstRecord.Length);

            return names
                .Select(static (name, index) => (ISchemaColumn)new SchemaColumn(
                    name,
                    index,
                    typeof(string),
                    new Dictionary<string, string>
                    {
                        [CsvColumnReadModifiers.SourceIndex] = index.ToString(System.Globalization.CultureInfo.InvariantCulture)
                    }))
                .ToArray();
        }
        catch (FormatException)
        {
            return [];
        }
        catch (DecoderFallbackException)
        {
            return [];
        }
        catch (IOException)
        {
            return [];
        }
        catch (ArgumentException)
        {
            return [];
        }
    }

    private static string[] CreateHeaderNames(IReadOnlyList<string> header)
    {
        var names = new string[header.Count];
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var index = 0; index < header.Count; index++)
        {
            var name = string.IsNullOrWhiteSpace(header[index])
                ? CreateOrdinalName(index)
                : header[index];
            if (!used.Add(name))
                name = CreateOrdinalName(index);

            names[index] = name;
        }

        return names;
    }

    private static string[] CreateOrdinalNames(int count)
    {
        var names = new string[count];
        for (var index = 0; index < count; index++)
            names[index] = CreateOrdinalName(index);

        return names;
    }

    private static string CreateOrdinalName(int index)
    {
        return $"Column{index}";
    }
}
