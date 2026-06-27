using System.Globalization;
using System.Text;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Optimization;

namespace Musoq.Examples.DataSources.Csv;

public sealed class CsvFileSource : DiagnosticChunkedRowSource<CsvRow>
{
    private const int ChunkSize = 32;
    private const string DefaultDelimiter = ",";
    internal const string SourceName = "csv.file";

    private readonly SourceExecutionContext _context;
    private readonly ISchemaColumn[] _columns;
    private readonly string? _path;
    private readonly bool _hasHeader;
    private readonly int _skipRows;
    private readonly char _delimiter;
    private readonly Encoding _encoding;

    public CsvFileSource(SourceExecutionContext context)
        : this(new CsvFileSourceOptions(null, false, 0, DefaultDelimiter), context)
    {
    }

    public CsvFileSource(string path, SourceExecutionContext context)
        : this(new CsvFileSourceOptions(path, false, 0, DefaultDelimiter), context)
    {
    }

    public CsvFileSource(string path, bool hasHeader, SourceExecutionContext context)
        : this(new CsvFileSourceOptions(path, hasHeader, 0, DefaultDelimiter), context)
    {
    }

    public CsvFileSource(string path, bool hasHeader, int skipRows, SourceExecutionContext context)
        : this(new CsvFileSourceOptions(path, hasHeader, skipRows, DefaultDelimiter), context)
    {
    }

    public CsvFileSource(
        string path,
        bool hasHeader,
        int skipRows,
        string delimiter,
        SourceExecutionContext context)
        : this(new CsvFileSourceOptions(path, hasHeader, skipRows, delimiter), context)
    {
    }

    protected override void CollectChunks(DiagnosticChunkWriter<CsvRow> writer)
    {
        var token = writer.CancellationToken;
        token.ThrowIfCancellationRequested();

        _context.ReportDataSourceBegin(SourceName);
        var rowsRead = 0L;

        try
        {
            if (string.IsNullOrWhiteSpace(_path))
            {
                _context.ReportDataSourceRowsKnown(SourceName, 0);
                return;
            }

            using var stream = File.OpenRead(_path);
            using var reader = new StreamReader(stream, _encoding, detectEncodingFromByteOrderMarks: true);
            using var records = ReadRecords(reader, _delimiter, token).GetEnumerator();

            SkipRecords(records, _skipRows, token);
            var headerRead = _hasHeader
                ? ReadNextRecord(records, token)
                : CsvRecordReadResult.Missing;
            var header = headerRead.Found
                ? headerRead.Record
                : null;
            var mappings = CreateMappings(_columns, header, _hasHeader);
            var chunk = new List<CsvRow>(ChunkSize);
            var rows = CsvSourcePlan.Apply(
                ReadRows(records, mappings, token),
                _context.Plan,
                _columns);

            foreach (var row in rows)
            {
                token.ThrowIfCancellationRequested();
                chunk.Add(row);

                if (chunk.Count < ChunkSize)
                    continue;

                rowsRead += WriteChunk(writer, chunk);
                _context.ReportDataSourceRowsRead(SourceName, rowsRead);
            }

            var finalRows = WriteChunk(writer, chunk);
            if (finalRows > 0)
            {
                rowsRead += finalRows;
                _context.ReportDataSourceRowsRead(SourceName, rowsRead);
            }
        }
        finally
        {
            _context.ReportDataSourceEnd(SourceName, rowsRead);
        }
    }

    private CsvFileSource(
        CsvFileSourceOptions options,
        SourceExecutionContext context)
        : base(context, SourceName)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (options.SkipRows < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options.SkipRows),
                options.SkipRows,
                "Skipped row count cannot be negative.");
        }

        _context = context;
        _columns = ResolveExecutionColumns(context.AllColumns, context.Plan.AcceptedColumns);
        _path = options.Path;
        _hasHeader = options.HasHeader;
        _skipRows = options.SkipRows;
        _delimiter = ResolveDelimiter(options.Delimiter);
        _encoding = ResolveEncoding(_columns);
    }

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

    private static Encoding ResolveEncoding(IReadOnlyCollection<ISchemaColumn> columns)
    {
        string? requestedEncoding = null;

        foreach (var column in columns)
        {
            if (!column.ReadModifiers.TryGetValue(ColumnReadModifiers.Encoding, out var encoding))
                continue;

            if (requestedEncoding == null)
            {
                requestedEncoding = encoding;
                continue;
            }

            if (!string.Equals(requestedEncoding, encoding, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"CSV file encoding is file-wide, but columns requested both '{requestedEncoding}' and '{encoding}'.");
            }
        }

        return ResolveEncoding(requestedEncoding);
    }

    private static ISchemaColumn[] ResolveExecutionColumns(
        IReadOnlyCollection<ISchemaColumn> columns,
        IReadOnlyList<SourceColumnRef> acceptedColumns)
    {
        var allColumns = columns.ToArray();
        if (acceptedColumns.Count == 0)
            return allColumns;

        var acceptedNames = acceptedColumns
            .Select(static column => column.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        return allColumns
            .Where(column => acceptedNames.Contains(column.ColumnName))
            .ToArray();
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

    private static void SkipRecords(
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

    private static CsvRecordReadResult ReadNextRecord(
        IEnumerator<string[]> records,
        CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        if (records.MoveNext())
            return CsvRecordReadResult.FoundRecord(records.Current);

        return CsvRecordReadResult.Missing;
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
                    throw new FormatException("Malformed CSV: quoted field was not closed.");

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

    private static IEnumerable<CsvRow> ReadRows(
        IEnumerator<string[]> records,
        IReadOnlyList<CsvColumnMapping> mappings,
        CancellationToken token)
    {
        while (true)
        {
            var recordRead = ReadNextRecord(records, token);
            if (!recordRead.Found)
                yield break;

            token.ThrowIfCancellationRequested();
            yield return CreateRow(recordRead.Record, mappings);
        }
    }

    private static CsvColumnMapping[] CreateMappings(
        IReadOnlyList<ISchemaColumn> columns,
        IReadOnlyList<string>? header,
        bool hasHeader)
    {
        var headerIndexes = hasHeader && header != null
            ? CreateHeaderIndexes(header)
            : new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        return columns
            .Select(column => CreateMapping(column, headerIndexes, hasHeader))
            .ToArray();
    }

    private static Dictionary<string, int> CreateHeaderIndexes(IReadOnlyList<string> header)
    {
        var indexes = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < header.Count; index++)
            indexes.TryAdd(header[index], index);

        return indexes;
    }

    private static CsvColumnMapping CreateMapping(
        ISchemaColumn column,
        IReadOnlyDictionary<string, int> headerIndexes,
        bool hasHeader)
    {
        var sourceIndex = ResolveSourceIndex(column, headerIndexes, hasHeader);
        return new CsvColumnMapping(column, sourceIndex, column.ColumnIndex);
    }

    private static int ResolveSourceIndex(
        ISchemaColumn column,
        IReadOnlyDictionary<string, int> headerIndexes,
        bool hasHeader)
    {
        if (column.ReadModifiers.TryGetValue(CsvColumnReadModifiers.SourceIndex, out var explicitIndex))
        {
            if (int.TryParse(explicitIndex, NumberStyles.None, CultureInfo.InvariantCulture, out var parsedIndex) &&
                parsedIndex >= 0)
            {
                return parsedIndex;
            }

            throw new InvalidOperationException(
                $"CSV source index modifier for column '{column.ColumnName}' must be a non-negative integer.");
        }

        if (!hasHeader)
            return column.ColumnIndex;

        var sourceName = column.ReadModifiers.TryGetValue(CsvColumnReadModifiers.SourceName, out var explicitName)
            ? explicitName
            : column.ColumnName;

        if (headerIndexes.TryGetValue(sourceName, out var headerIndex))
            return headerIndex;

        throw new InvalidOperationException(
            $"CSV header does not contain source column '{sourceName}' for table column '{column.ColumnName}'.");
    }

    private static CsvRow CreateRow(
        IReadOnlyList<string> record,
        IReadOnlyList<CsvColumnMapping> mappings)
    {
        var valueCount = mappings.Count == 0
            ? 0
            : mappings.Max(static mapping => mapping.ValueIndex) + 1;
        var values = new object?[valueCount];

        foreach (var mapping in mappings)
        {
            var rawValue = mapping.SourceIndex < record.Count
                ? record[mapping.SourceIndex]
                : null;
            values[mapping.ValueIndex] = ConvertValue(rawValue, mapping.Column);
        }

        return new CsvRow(values);
    }

    private static object? ConvertValue(string? rawValue, ISchemaColumn column)
    {
        if (rawValue == null)
            return null;

        var text = column.ReadModifiers.ContainsKey(ColumnReadModifiers.Trim)
            ? rawValue.Trim()
            : rawValue;
        var targetType = Nullable.GetUnderlyingType(column.ColumnType) ?? column.ColumnType;
        var isNullable = Nullable.GetUnderlyingType(column.ColumnType) != null || !column.ColumnType.IsValueType;

        if (text.Length == 0 && targetType != typeof(string) && isNullable)
            return null;

        if (targetType == typeof(string) || targetType == typeof(object))
            return text;

        var culture = ResolveCulture(column.ReadModifiers);

        if (targetType == typeof(DateTime))
            return ParseDateTime(text, column.ReadModifiers, culture);

        if (targetType == typeof(DateTimeOffset))
            return ParseDateTimeOffset(text, column.ReadModifiers, culture);

        if (targetType == typeof(TimeSpan))
            return ParseTimeSpan(text, column.ReadModifiers, culture);

        if (targetType == typeof(Guid))
            return Guid.Parse(text);

        if (targetType == typeof(bool))
            return bool.Parse(text);

        return Convert.ChangeType(text, targetType, culture);
    }

    private static DateTime ParseDateTime(
        string text,
        IReadOnlyDictionary<string, string> modifiers,
        CultureInfo culture)
    {
        return modifiers.TryGetValue(ColumnReadModifiers.Format, out var format)
            ? DateTime.ParseExact(text, format, culture)
            : DateTime.Parse(text, culture);
    }

    private static DateTimeOffset ParseDateTimeOffset(
        string text,
        IReadOnlyDictionary<string, string> modifiers,
        CultureInfo culture)
    {
        return modifiers.TryGetValue(ColumnReadModifiers.Format, out var format)
            ? DateTimeOffset.ParseExact(text, format, culture)
            : DateTimeOffset.Parse(text, culture);
    }

    private static TimeSpan ParseTimeSpan(
        string text,
        IReadOnlyDictionary<string, string> modifiers,
        CultureInfo culture)
    {
        return modifiers.TryGetValue(ColumnReadModifiers.Format, out var format)
            ? TimeSpan.ParseExact(text, format, culture)
            : TimeSpan.Parse(text, culture);
    }

    private static CultureInfo ResolveCulture(IReadOnlyDictionary<string, string> modifiers)
    {
        return modifiers.TryGetValue(ColumnReadModifiers.Culture, out var culture)
            ? CultureInfo.GetCultureInfo(culture)
            : CultureInfo.InvariantCulture;
    }

    private static int WriteChunk(DiagnosticChunkWriter<CsvRow> writer, List<CsvRow> chunk)
    {
        if (chunk.Count == 0)
            return 0;

        var rowsWritten = chunk.Count;
        writer.Write(chunk.ToArray());
        chunk.Clear();
        return rowsWritten;
    }

    private sealed record CsvColumnMapping(ISchemaColumn Column, int SourceIndex, int ValueIndex);

    private sealed record CsvRecordReadResult(bool Found, string[] Record)
    {
        public static CsvRecordReadResult Missing { get; } = new(false, []);

        public static CsvRecordReadResult FoundRecord(string[] record)
        {
            return new CsvRecordReadResult(true, record);
        }
    }

    private sealed record CsvFileSourceOptions(
        string? Path,
        bool HasHeader,
        int SkipRows,
        string Delimiter);
}
