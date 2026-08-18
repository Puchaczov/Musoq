using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Optimization;

namespace Musoq.Examples.DataSources.Csv;

internal sealed class CsvQueryRowSource<TRow, TMaterializer> : DiagnosticChunkedRowSource<TRow>
    where TMaterializer : struct, IQueryRowMaterializer<TRow>
{
    private const int ChunkSize = 32;

    private readonly SourceExecutionContext _context;
    private readonly QueryRowShape _shape;
    private readonly string? _path;
    private readonly bool _hasHeader;
    private readonly int _skipRows;
    private readonly char _delimiter;
    private readonly Encoding _encoding;

    public CsvQueryRowSource(
        CsvSourceOptions options,
        QueryScopedRowSourceRequest request)
        : base(request.ExecutionContext, CsvFileSource.SourceName)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(request);
        if (options.SkipRows < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options.SkipRows),
                options.SkipRows,
                "Skipped row count cannot be negative.");
        }

        _context = request.ExecutionContext;
        _shape = request.Shape;
        _path = options.Path;
        _hasHeader = options.HasHeader;
        _skipRows = options.SkipRows;
        _delimiter = CsvFileSource.ResolveDelimiter(options.Delimiter);
        _encoding = ResolveEncoding(_shape.Fields);
    }

    protected override void CollectChunks(DiagnosticChunkWriter<TRow> writer)
    {
        var token = writer.CancellationToken;
        token.ThrowIfCancellationRequested();

        _context.ReportDataSourceBegin(CsvFileSource.SourceName);
        var rowsRead = 0L;

        try
        {
            if (string.IsNullOrWhiteSpace(_path))
            {
                _context.ReportDataSourceRowsKnown(CsvFileSource.SourceName, 0);
                return;
            }

            using var stream = File.OpenRead(_path);
            using var reader = new StreamReader(stream, _encoding, detectEncodingFromByteOrderMarks: true);
            using var records = CsvFileSource.ReadRecords(reader, _delimiter, token).GetEnumerator();

            CsvFileSource.SkipRecords(records, _skipRows, token);
            var header = _hasHeader && records.MoveNext()
                ? records.Current
                : null;
            var mappings = CreateMappings(_shape.Fields, header, _hasHeader);
            var rows = Apply(
                ReadRows(records, mappings, token),
                _context.Plan,
                mappings);
            var chunk = new List<TRow>(ChunkSize);

            foreach (var rawRow in rows)
            {
                token.ThrowIfCancellationRequested();
                var fieldReader = new CsvFieldReader(rawRow.Record, mappings);
                chunk.Add(TMaterializer.Materialize<CsvFieldReader>(ref fieldReader));

                if (chunk.Count < ChunkSize)
                    continue;

                rowsRead += WriteChunk(writer, chunk);
                _context.ReportDataSourceRowsRead(CsvFileSource.SourceName, rowsRead);
            }

            var finalRows = WriteChunk(writer, chunk);
            if (finalRows > 0)
            {
                rowsRead += finalRows;
                _context.ReportDataSourceRowsRead(CsvFileSource.SourceName, rowsRead);
            }
        }
        finally
        {
            _context.ReportDataSourceEnd(CsvFileSource.SourceName, rowsRead);
        }
    }

    private static Encoding ResolveEncoding(IReadOnlyList<QueryRowField> fields)
    {
        string? requestedEncoding = null;

        foreach (var field in fields)
        {
            if (!field.ReadModifiers.TryGetValue(ColumnReadModifiers.Encoding, out var encoding))
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

        return CsvFileSource.ResolveEncoding(requestedEncoding);
    }

    private static IEnumerable<CsvRawQueryRow> ReadRows(
        IEnumerator<string[]> records,
        CsvQueryFieldMapping[] mappings,
        CancellationToken token)
    {
        while (records.MoveNext())
        {
            token.ThrowIfCancellationRequested();
            yield return new CsvRawQueryRow(records.Current, mappings);
        }
    }

    private static IEnumerable<CsvRawQueryRow> Apply(
        IEnumerable<CsvRawQueryRow> rows,
        SourceExecutionPlan plan,
        CsvQueryFieldMapping[] mappings)
    {
        var query = rows;
        if (plan.AcceptedPredicate != null)
        {
            query = query.Where(row => CsvSourcePlan.EvaluatePredicate(
                plan.AcceptedPredicate,
                row.ReadValue));
        }

        if (plan.AcceptedOrderBy.Count > 0)
        {
            query = query.OrderBy(
                static row => row,
                new CsvRawQueryRowComparer(plan.AcceptedOrderBy));
        }

        if (plan.AcceptedSkip.HasValue)
            query = query.Skip(checked((int)plan.AcceptedSkip.Value));

        if (plan.AcceptedTake.HasValue)
            query = query.Take(checked((int)plan.AcceptedTake.Value));

        return query;
    }

    private static CsvQueryFieldMapping[] CreateMappings(
        IReadOnlyList<QueryRowField> fields,
        IReadOnlyList<string>? header,
        bool hasHeader)
    {
        var headerIndexes = hasHeader && header != null
            ? CreateHeaderIndexes(header)
            : new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var mappings = new CsvQueryFieldMapping[fields.Count];

        foreach (var field in fields)
        {
            var column = new SchemaColumn(
                field.Name,
                field.Slot,
                field.FieldType,
                field.ReadModifiers);
            var sourceIndex = ResolveSourceIndex(field, headerIndexes, hasHeader);
            mappings[field.Slot] = new CsvQueryFieldMapping(field, column, sourceIndex);
        }

        return mappings;
    }

    private static Dictionary<string, int> CreateHeaderIndexes(IReadOnlyList<string> header)
    {
        var indexes = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < header.Count; index++)
            indexes.TryAdd(header[index], index);

        return indexes;
    }

    private static int ResolveSourceIndex(
        QueryRowField field,
        IReadOnlyDictionary<string, int> headerIndexes,
        bool hasHeader)
    {
        if (field.ReadModifiers.TryGetValue(CsvColumnReadModifiers.SourceIndex, out var explicitIndex))
        {
            if (int.TryParse(explicitIndex, NumberStyles.None, CultureInfo.InvariantCulture, out var parsedIndex) &&
                parsedIndex >= 0)
            {
                return parsedIndex;
            }

            throw new InvalidOperationException(
                $"CSV source index modifier for column '{field.Name}' must be a non-negative integer.");
        }

        if (!hasHeader)
            return field.SourceColumnIndex;

        var sourceName = field.ReadModifiers.TryGetValue(CsvColumnReadModifiers.SourceName, out var explicitName)
            ? explicitName
            : field.Name;

        if (headerIndexes.TryGetValue(sourceName, out var headerIndex))
            return headerIndex;

        throw new InvalidOperationException(
            $"CSV header does not contain source column '{sourceName}' for table column '{field.Name}'.");
    }

    private static int WriteChunk(DiagnosticChunkWriter<TRow> writer, List<TRow> chunk)
    {
        if (chunk.Count == 0)
            return 0;

        var rowsWritten = chunk.Count;
        writer.Write(chunk.ToArray());
        chunk.Clear();
        return rowsWritten;
    }

    private readonly struct CsvRawQueryRow
    {
        public CsvRawQueryRow(string[] record, CsvQueryFieldMapping[] mappings)
        {
            Record = record;
            Mappings = mappings;
        }

        public string[] Record { get; }

        private CsvQueryFieldMapping[] Mappings { get; }

        public object? ReadValue(string name)
        {
            foreach (var mapping in Mappings)
            {
                if (!string.Equals(mapping.Field.Name, name, StringComparison.OrdinalIgnoreCase))
                    continue;

                var rawValue = mapping.SourceIndex < Record.Length
                    ? Record[mapping.SourceIndex]
                    : null;
                return CsvFileSource.ConvertValue(rawValue, mapping.Column);
            }

            throw new InvalidOperationException(
                $"CSV source has no execution column '{name}'.");
        }
    }

    private sealed class CsvRawQueryRowComparer(IReadOnlyList<OrderByExpression> orderBy)
        : IComparer<CsvRawQueryRow>
    {
        public int Compare(CsvRawQueryRow x, CsvRawQueryRow y)
        {
            foreach (var order in orderBy)
            {
                var comparison = CsvSourcePlan.CompareValues(
                    x.ReadValue(order.Column.Name),
                    y.ReadValue(order.Column.Name));
                if (comparison == 0)
                    continue;

                return order.Direction == OrderDirection.Descending
                    ? -comparison
                    : comparison;
            }

            return 0;
        }
    }

}

internal ref struct CsvFieldReader : IQuerySourceFieldReader
{
    private readonly string[] _record;
    private readonly CsvQueryFieldMapping[] _mappings;

    public CsvFieldReader(
        string[] record,
        CsvQueryFieldMapping[] mappings)
    {
        _record = record;
        _mappings = mappings;
    }

    public T Read<T>(int slot)
    {
        if ((uint)slot >= (uint)_mappings.Length)
            throw new ArgumentOutOfRangeException(nameof(slot), slot, "CSV query row slot is out of range.");

        var mapping = _mappings[slot];
        var rawValue = mapping.SourceIndex < _record.Length
            ? _record[mapping.SourceIndex]
            : null;
        return CsvValueReader.Read<T>(rawValue, mapping.Column);
    }
}

internal sealed record CsvQueryFieldMapping(
    QueryRowField Field,
    ISchemaColumn Column,
    int SourceIndex);

internal static class CsvValueReader
{
    public static T Read<T>(string? rawValue, ISchemaColumn column)
    {
        var text = Normalize(rawValue, column);
        if (typeof(T) == typeof(string))
        {
            string? value = text;
            return Cast<string?, T>(value);
        }

        var targetType = Nullable.GetUnderlyingType(typeof(T));
        var isNullable = targetType != null || !typeof(T).IsValueType;
        if (text == null || (text.Length == 0 && targetType != typeof(string) && isNullable))
            return default!;

        var culture = CsvFileSource.ResolveCulture(column.ReadModifiers);
        var actualType = targetType ?? typeof(T);
        var nullableTarget = targetType != null;

        if (actualType == typeof(int))
        {
            var value = int.Parse(text, NumberStyles.Any, culture);
            return nullableTarget ? Cast<int?, T>(value) : Cast<int, T>(value);
        }
        if (actualType == typeof(long))
        {
            var value = long.Parse(text, NumberStyles.Any, culture);
            return nullableTarget ? Cast<long?, T>(value) : Cast<long, T>(value);
        }
        if (actualType == typeof(short))
        {
            var value = short.Parse(text, NumberStyles.Any, culture);
            return nullableTarget ? Cast<short?, T>(value) : Cast<short, T>(value);
        }
        if (actualType == typeof(byte))
        {
            var value = byte.Parse(text, NumberStyles.Any, culture);
            return nullableTarget ? Cast<byte?, T>(value) : Cast<byte, T>(value);
        }
        if (actualType == typeof(uint))
        {
            var value = uint.Parse(text, NumberStyles.Any, culture);
            return nullableTarget ? Cast<uint?, T>(value) : Cast<uint, T>(value);
        }
        if (actualType == typeof(ulong))
        {
            var value = ulong.Parse(text, NumberStyles.Any, culture);
            return nullableTarget ? Cast<ulong?, T>(value) : Cast<ulong, T>(value);
        }
        if (actualType == typeof(float))
        {
            var value = float.Parse(text, NumberStyles.Any, culture);
            return nullableTarget ? Cast<float?, T>(value) : Cast<float, T>(value);
        }
        if (actualType == typeof(double))
        {
            var value = double.Parse(text, NumberStyles.Any, culture);
            return nullableTarget ? Cast<double?, T>(value) : Cast<double, T>(value);
        }
        if (actualType == typeof(decimal))
        {
            var value = decimal.Parse(text, NumberStyles.Any, culture);
            return nullableTarget ? Cast<decimal?, T>(value) : Cast<decimal, T>(value);
        }
        if (actualType == typeof(bool))
        {
            var value = bool.Parse(text);
            return nullableTarget ? Cast<bool?, T>(value) : Cast<bool, T>(value);
        }
        if (actualType == typeof(Guid))
        {
            var value = Guid.Parse(text);
            return nullableTarget ? Cast<Guid?, T>(value) : Cast<Guid, T>(value);
        }
        if (actualType == typeof(DateTime))
        {
            var value = CsvFileSource.ParseDateTime(text, column.ReadModifiers, culture);
            return nullableTarget ? Cast<DateTime?, T>(value) : Cast<DateTime, T>(value);
        }
        if (actualType == typeof(DateTimeOffset))
        {
            var value = CsvFileSource.ParseDateTimeOffset(text, column.ReadModifiers, culture);
            return nullableTarget ? Cast<DateTimeOffset?, T>(value) : Cast<DateTimeOffset, T>(value);
        }

        if (actualType == typeof(TimeSpan))
        {
            var value = CsvFileSource.ParseTimeSpan(text, column.ReadModifiers, culture);
            return nullableTarget ? Cast<TimeSpan?, T>(value) : Cast<TimeSpan, T>(value);
        }

        var converted = CsvFileSource.ConvertValue(text, column);
        return converted is null ? default! : (T)converted;
    }

    private static string? Normalize(string? rawValue, ISchemaColumn column)
    {
        if (rawValue == null)
            return null;

        return column.ReadModifiers.ContainsKey(ColumnReadModifiers.Trim)
            ? rawValue.Trim()
            : rawValue;
    }

    private static T Cast<TValue, T>(TValue value)
    {
        return Unsafe.As<TValue, T>(ref value);
    }
}
