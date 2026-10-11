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
        : base(request.ExecutionContext, CsvTextFormat.SourceName)
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
        _delimiter = CsvTextFormat.ResolveDelimiter(options.Delimiter);
        _encoding = ResolveEncoding(_shape.Fields);
    }

    protected override void CollectChunks(DiagnosticChunkWriter<TRow> writer)
    {
        var token = writer.CancellationToken;
        token.ThrowIfCancellationRequested();

        _context.ReportDataSourceBegin(CsvTextFormat.SourceName);
        var rowsRead = 0L;

        try
        {
            if (string.IsNullOrWhiteSpace(_path))
            {
                _context.ReportDataSourceRowsKnown(CsvTextFormat.SourceName, 0);
                return;
            }

            using var stream = File.OpenRead(_path);
            using var reader = new StreamReader(stream, _encoding, detectEncodingFromByteOrderMarks: true);
            using var records = CsvTextFormat.ReadRecords(reader, _delimiter, token).GetEnumerator();

            CsvTextFormat.SkipRecords(records, _skipRows, token);
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
                _context.ReportDataSourceRowsRead(CsvTextFormat.SourceName, rowsRead);
            }

            var finalRows = WriteChunk(writer, chunk);
            if (finalRows > 0)
            {
                rowsRead += finalRows;
                _context.ReportDataSourceRowsRead(CsvTextFormat.SourceName, rowsRead);
            }
        }
        finally
        {
            _context.ReportDataSourceEnd(CsvTextFormat.SourceName, rowsRead);
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

        return CsvTextFormat.ResolveEncoding(requestedEncoding);
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
        var native = plan.Properties.TryGetValue(CsvNativeExecutionPlan.PropertyName, out var bound)
            ? bound as CsvNativeExecutionPlan : null;
        if ((plan.AcceptedPredicate is not null || plan.AcceptedOrderBy.Count > 0) && native is null)
            throw new InvalidOperationException("CSV execution requires a compiled native plan.");
        var query = rows;
        if (native?.Predicate is { } predicate)
            query = query.Where(row =>
            {
                var reader = row.CreateReader();
                return predicate.Matches(ref reader);
            });
        if (native?.Ordering.Count > 0)
            query = query.OrderBy(static row => row, new CsvRawQueryRowComparer(native));

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

        public CsvFieldReader CreateReader() => new(Record, Mappings);
    }

    private sealed class CsvRawQueryRowComparer(CsvNativeExecutionPlan plan) : IComparer<CsvRawQueryRow>
    {
        public int Compare(CsvRawQueryRow x, CsvRawQueryRow y)
        {
            var left = x.CreateReader();
            var right = y.CreateReader();
            for (var index = 0; index < plan.Ordering.Count; index++)
            {
                var comparison = plan.Ordering[index].Compare(ref left, ref right);
                if (comparison != 0)
                    return comparison;
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

    public T Read<T>(string name)
    {
        for (var slot = 0; slot < _mappings.Length; slot++)
            if (string.Equals(_mappings[slot].Field.Name, name, StringComparison.OrdinalIgnoreCase))
                return Read<T>(slot);
        throw new InvalidOperationException($"CSV source has no execution column '{name}'.");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T Read<T>(string? rawValue, ISchemaColumn column)
        => Decoder<T>.Read(rawValue, column);

    public static bool Supports(Type type)
    {
        var concreteType = Nullable.GetUnderlyingType(type) ?? type;
        return concreteType == typeof(string) || SupportedValueTypes.Contains(concreteType);
    }

    private static readonly HashSet<Type> SupportedValueTypes =
    [
        typeof(int), typeof(long), typeof(short), typeof(byte), typeof(uint), typeof(ulong),
        typeof(float), typeof(double), typeof(decimal), typeof(bool), typeof(Guid),
        typeof(DateTime), typeof(DateTimeOffset), typeof(TimeSpan), typeof(sbyte), typeof(ushort), typeof(char)
    ];

    private static class Decoder<T>
    {
        internal static readonly Func<string?, ISchemaColumn, T> Read = Bind();

        private static Func<string?, ISchemaColumn, T> Bind()
        {
            if (typeof(T) == typeof(string))
                return (rawValue, column) => Cast<string?, T>(Normalize(rawValue, column));

            if (typeof(T) == typeof(int))
                return Required<int>(Parseint);
            if (typeof(T) == typeof(int?))
                return Optional<int>(Parseint);
            if (typeof(T) == typeof(long))
                return Required<long>(Parselong);
            if (typeof(T) == typeof(long?))
                return Optional<long>(Parselong);
            if (typeof(T) == typeof(short))
                return Required<short>(Parseshort);
            if (typeof(T) == typeof(short?))
                return Optional<short>(Parseshort);
            if (typeof(T) == typeof(byte))
                return Required<byte>(Parsebyte);
            if (typeof(T) == typeof(byte?))
                return Optional<byte>(Parsebyte);
            if (typeof(T) == typeof(uint))
                return Required<uint>(Parseuint);
            if (typeof(T) == typeof(uint?))
                return Optional<uint>(Parseuint);
            if (typeof(T) == typeof(ulong))
                return Required<ulong>(Parseulong);
            if (typeof(T) == typeof(ulong?))
                return Optional<ulong>(Parseulong);
            if (typeof(T) == typeof(float))
                return Required<float>(Parsefloat);
            if (typeof(T) == typeof(float?))
                return Optional<float>(Parsefloat);
            if (typeof(T) == typeof(double))
                return Required<double>(Parsedouble);
            if (typeof(T) == typeof(double?))
                return Optional<double>(Parsedouble);
            if (typeof(T) == typeof(decimal))
                return Required<decimal>(Parsedecimal);
            if (typeof(T) == typeof(decimal?))
                return Optional<decimal>(Parsedecimal);
            if (typeof(T) == typeof(bool))
                return Required<bool>(Parsebool);
            if (typeof(T) == typeof(bool?))
                return Optional<bool>(Parsebool);
            if (typeof(T) == typeof(Guid))
                return Required<Guid>(ParseGuid);
            if (typeof(T) == typeof(Guid?))
                return Optional<Guid>(ParseGuid);
            if (typeof(T) == typeof(DateTime))
                return Required<DateTime>(ParseDateTime);
            if (typeof(T) == typeof(DateTime?))
                return Optional<DateTime>(ParseDateTime);
            if (typeof(T) == typeof(DateTimeOffset))
                return Required<DateTimeOffset>(ParseDateTimeOffset);
            if (typeof(T) == typeof(DateTimeOffset?))
                return Optional<DateTimeOffset>(ParseDateTimeOffset);
            if (typeof(T) == typeof(TimeSpan))
                return Required<TimeSpan>(ParseTimeSpan);
            if (typeof(T) == typeof(TimeSpan?))
                return Optional<TimeSpan>(ParseTimeSpan);
            if (typeof(T) == typeof(sbyte))
                return Required<sbyte>(Parsesbyte);
            if (typeof(T) == typeof(sbyte?))
                return Optional<sbyte>(Parsesbyte);
            if (typeof(T) == typeof(ushort))
                return Required<ushort>(Parseushort);
            if (typeof(T) == typeof(ushort?))
                return Optional<ushort>(Parseushort);
            if (typeof(T) == typeof(char))
                return Required<char>(Parsechar);
            if (typeof(T) == typeof(char?))
                return Optional<char>(Parsechar);
            throw new InvalidOperationException($"CSV has no native decoder for '{typeof(T)}'.");
        }

        private static Func<string?, ISchemaColumn, T> Required<TValue>(Func<string, ISchemaColumn, TValue> parse)
            where TValue : struct
            => (rawValue, column) =>
            {
                var text = Normalize(rawValue, column);
                if (text is null)
                    return default!;
                return Cast<TValue, T>(parse(text, column));
            };

        private static Func<string?, ISchemaColumn, T> Optional<TValue>(Func<string, ISchemaColumn, TValue> parse)
            where TValue : struct
            => (rawValue, column) =>
            {
                var text = Normalize(rawValue, column);
                if (string.IsNullOrEmpty(text))
                    return default!;
                TValue? value = parse(text, column);
                return Cast<TValue?, T>(value);
            };
    }

    private static int Parseint(string text, ISchemaColumn column)
    {
        var culture = CsvTextFormat.ResolveCulture(column.ReadModifiers);
        return int.Parse(text, NumberStyles.Any, culture);
    }

    private static long Parselong(string text, ISchemaColumn column)
    {
        var culture = CsvTextFormat.ResolveCulture(column.ReadModifiers);
        return long.Parse(text, NumberStyles.Any, culture);
    }

    private static short Parseshort(string text, ISchemaColumn column)
    {
        var culture = CsvTextFormat.ResolveCulture(column.ReadModifiers);
        return short.Parse(text, NumberStyles.Any, culture);
    }

    private static byte Parsebyte(string text, ISchemaColumn column)
    {
        var culture = CsvTextFormat.ResolveCulture(column.ReadModifiers);
        return byte.Parse(text, NumberStyles.Any, culture);
    }

    private static uint Parseuint(string text, ISchemaColumn column)
    {
        var culture = CsvTextFormat.ResolveCulture(column.ReadModifiers);
        return uint.Parse(text, NumberStyles.Any, culture);
    }

    private static ulong Parseulong(string text, ISchemaColumn column)
    {
        var culture = CsvTextFormat.ResolveCulture(column.ReadModifiers);
        return ulong.Parse(text, NumberStyles.Any, culture);
    }

    private static float Parsefloat(string text, ISchemaColumn column)
    {
        var culture = CsvTextFormat.ResolveCulture(column.ReadModifiers);
        return float.Parse(text, NumberStyles.Any, culture);
    }

    private static double Parsedouble(string text, ISchemaColumn column)
    {
        var culture = CsvTextFormat.ResolveCulture(column.ReadModifiers);
        return double.Parse(text, NumberStyles.Any, culture);
    }

    private static decimal Parsedecimal(string text, ISchemaColumn column)
    {
        var culture = CsvTextFormat.ResolveCulture(column.ReadModifiers);
        return decimal.Parse(text, NumberStyles.Any, culture);
    }

    private static bool Parsebool(string text, ISchemaColumn column)
    {
        return bool.Parse(text);
    }

    private static Guid ParseGuid(string text, ISchemaColumn column)
    {
        return Guid.Parse(text);
    }

    private static DateTime ParseDateTime(string text, ISchemaColumn column)
    {
        var culture = CsvTextFormat.ResolveCulture(column.ReadModifiers);
        return CsvTextFormat.ParseDateTime(text, column.ReadModifiers, culture);
    }

    private static DateTimeOffset ParseDateTimeOffset(string text, ISchemaColumn column)
    {
        var culture = CsvTextFormat.ResolveCulture(column.ReadModifiers);
        return CsvTextFormat.ParseDateTimeOffset(text, column.ReadModifiers, culture);
    }

    private static TimeSpan ParseTimeSpan(string text, ISchemaColumn column)
    {
        var culture = CsvTextFormat.ResolveCulture(column.ReadModifiers);
        return CsvTextFormat.ParseTimeSpan(text, column.ReadModifiers, culture);
    }

    private static sbyte Parsesbyte(string text, ISchemaColumn column)
    {
        var culture = CsvTextFormat.ResolveCulture(column.ReadModifiers);
        return sbyte.Parse(text, NumberStyles.Any, culture);
    }

    private static ushort Parseushort(string text, ISchemaColumn column)
    {
        var culture = CsvTextFormat.ResolveCulture(column.ReadModifiers);
        return ushort.Parse(text, NumberStyles.Any, culture);
    }

    private static char Parsechar(string text, ISchemaColumn column)
    {
        return char.Parse(text);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string? Normalize(string? rawValue, ISchemaColumn column)
    {
        if (rawValue is null)
            return null;
        if (column.ReadModifiers.ContainsKey(ColumnReadModifiers.Trim))
            return rawValue.Trim();
        return rawValue;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static T Cast<TValue, T>(TValue value)
        => Unsafe.As<TValue, T>(ref value);
}
