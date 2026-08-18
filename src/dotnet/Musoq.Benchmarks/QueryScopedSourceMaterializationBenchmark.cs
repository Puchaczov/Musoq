using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Logging.Abstractions;
using Musoq.Examples.DataSources.Csv;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Optimization;

namespace Musoq.Benchmarks;

/// <summary>
/// Compares like-for-like legacy, query-scoped struct, and query-scoped class
/// materialization through the example CSV datasource assembly.
/// </summary>
[MemoryDiagnoser]
[DisassemblyDiagnoser(printSource: true, exportCombinedDisassemblyReport: true)]
public class QueryScopedSourceMaterializationBenchmark
{
    private const int Rows = 2048;
    private string _path = string.Empty;
    private string _numericPath = string.Empty;
    private string _failurePath = string.Empty;
    private IReadOnlyList<ISchemaColumn> _columns = [];
    private IReadOnlyList<ISchemaColumn> _numericColumns = [];
    private SourceExecutionContext _context = null!;
    private SourceExecutionContext _numericContext = null!;
    private SourceExecutionContext _selectiveContext = null!;
    private SourceExecutionContext _rejectionContext = null!;
    private SourceExecutionContext _earlyTakeContext = null!;
    private QueryRowShape _shape = null!;
    private QueryRowShape _numericShape = null!;
    private QueryRowShape _selectiveShape = null!;
    private IQueryScopedRowSourceSchema _queryScopedSchema = null!;
    private string[] _materializationValues = [];
    private int[] _numericMaterializationValues = [];

    [Params(2, 8, 32, 64)]
    public int FieldCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _columns = Enumerable.Range(0, FieldCount)
            .Select(index => (ISchemaColumn)new SchemaColumn($"Column{index}", index, typeof(string)))
            .ToArray();
        _numericColumns = Enumerable.Range(0, FieldCount)
            .Select(index => (ISchemaColumn)new SchemaColumn($"Column{index}", index, typeof(int?)))
            .ToArray();
        _path = Path.Combine(Path.GetTempPath(), $"musoq-query-row-baseline-{Guid.NewGuid():N}.csv");
        _numericPath = Path.Combine(Path.GetTempPath(), $"musoq-query-row-numeric-{Guid.NewGuid():N}.csv");
        _failurePath = Path.Combine(Path.GetTempPath(), $"musoq-query-row-failure-{Guid.NewGuid():N}.csv");
        File.WriteAllText(_path, CreateCsvContent(numeric: false), new UTF8Encoding(false));
        File.WriteAllText(_numericPath, CreateCsvContent(numeric: true), new UTF8Encoding(false));
        File.WriteAllText(_failurePath, CreateFailureCsvContent(), new UTF8Encoding(false));
        _materializationValues = Enumerable.Range(0, FieldCount)
            .Select(index => $"value-{index.ToString(CultureInfo.InvariantCulture)}")
            .ToArray();
        _numericMaterializationValues = Enumerable.Range(0, FieldCount).ToArray();

        var identity = new SourceIdentity("#csv", "file", "baseline", "Rows");
        _context = CreateExecutionContext(identity, SourceExecutionPlan.Empty(identity), _columns);
        _numericContext = CreateExecutionContext(identity, SourceExecutionPlan.Empty(identity), _numericColumns);
        _selectiveContext = CreateExecutionContext(
            identity,
            SourceExecutionPlan.Empty(identity) with
            {
                AcceptedColumns = [new SourceColumnRef("Column0")]
            },
            _columns);
        _rejectionContext = CreateExecutionContext(
            identity,
            SourceExecutionPlan.Empty(identity) with
            {
                AcceptedPredicate = new SourcePredicateComparison(
                    SourcePredicateComparisonOperator.Equal,
                    new SourcePredicateColumn(new SourceColumnRef("Column0")),
                    new SourcePredicateLiteral("value-1999-0"))
            },
            _columns);
        _earlyTakeContext = CreateExecutionContext(
            identity,
            SourceExecutionPlan.Empty(identity) with { AcceptedTake = 16 },
            _columns);
        _shape = CreateShape(_columns);
        _numericShape = CreateShape(_numericColumns);
        _selectiveShape = CreateShape(_columns.Take(1).ToArray());
        _queryScopedSchema = (IQueryScopedRowSourceSchema)new CsvSchemaProvider(enableQueryScopedRows: true)
            .GetSchema(CsvSchema.SchemaName);

        VerifyCorrectness("full string", _path, _shape, _context);
        VerifyCorrectness("selective string", _path, _selectiveShape, _selectiveContext);
        VerifyCorrectness("high rejection string", _path, _shape, _rejectionContext);
        VerifyCorrectness("early take string", _path, _shape, _earlyTakeContext);
        VerifyCorrectness("full nullable numeric", _numericPath, _numericShape, _numericContext);
        VerifyFailureBehavior();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        if (File.Exists(_path))
            File.Delete(_path);
        if (File.Exists(_numericPath))
            File.Delete(_numericPath);
        if (File.Exists(_failurePath))
            File.Delete(_failurePath);
    }

    [Benchmark(Baseline = true, Description = "Legacy CSV rows")]
    public long LegacyRows() => RunLegacy(_path, _context, _shape.Fields.Count).Consumer;

    [Benchmark(Description = "Legacy CSV selective projection")]
    public long LegacySelectiveProjection() =>
        RunLegacy(_path, _selectiveContext, _selectiveShape.Fields.Count).Consumer;

    [Benchmark(Description = "Legacy CSV high rejection")]
    public long LegacyHighRejection() =>
        RunLegacy(_path, _rejectionContext, _shape.Fields.Count).Consumer;

    [Benchmark(Description = "Legacy CSV aggregation")]
    public long LegacyAggregation() => RunLegacy(_path, _context, _shape.Fields.Count).Checksum;

    [Benchmark(Description = "Legacy CSV early take")]
    public long LegacyEarlyTake() =>
        RunLegacy(_path, _earlyTakeContext, _shape.Fields.Count).Consumer;

    [Benchmark(Description = "Query-scoped CSV struct rows")]
    public long QueryScopedStructRows() => RunStringStructQueryScoped(_shape, _context).Consumer;

    [Benchmark(Description = "Query-scoped CSV class rows")]
    public long QueryScopedClassRows() => RunStringClassQueryScoped(_shape, _context).Consumer;

    [Benchmark(Description = "Query-scoped CSV selective projection")]
    public long QueryScopedSelectiveProjection() =>
        RunStringStructQueryScoped(_selectiveShape, _selectiveContext).Consumer;

    [Benchmark(Description = "Query-scoped CSV class selective projection")]
    public long QueryScopedClassSelectiveProjection() =>
        RunStringClassQueryScoped(_selectiveShape, _selectiveContext).Consumer;

    [Benchmark(Description = "Query-scoped CSV high rejection")]
    public long QueryScopedHighRejection() =>
        RunStringStructQueryScoped(_shape, _rejectionContext).Consumer;

    [Benchmark(Description = "Query-scoped CSV class high rejection")]
    public long QueryScopedClassHighRejection() =>
        RunStringClassQueryScoped(_shape, _rejectionContext).Consumer;

    [Benchmark(Description = "Query-scoped CSV struct aggregation")]
    public long QueryScopedStructAggregation() => RunStringStructQueryScoped(_shape, _context).Checksum;

    [Benchmark(Description = "Query-scoped CSV class aggregation")]
    public long QueryScopedClassAggregation() => RunStringClassQueryScoped(_shape, _context).Checksum;

    [Benchmark(Description = "Query-scoped CSV early take")]
    public long QueryScopedEarlyTake() =>
        RunStringStructQueryScoped(_shape, _earlyTakeContext).Consumer;

    [Benchmark(Description = "Query-scoped CSV class early take")]
    public long QueryScopedClassEarlyTake() =>
        RunStringClassQueryScoped(_shape, _earlyTakeContext).Consumer;

    [Benchmark(Description = "Legacy numeric CSV rows")]
    public long LegacyNumericRows() =>
        RunLegacy(_numericPath, _numericContext, _numericShape.Fields.Count).Consumer;

    [Benchmark(Description = "Query-scoped numeric CSV struct rows")]
    public long QueryScopedNumericStructRows() =>
        RunNumericStructQueryScoped(
            _numericShape,
            _numericContext,
            _numericPath).Consumer;

    [Benchmark(Description = "Query-scoped nullable numeric CSV class rows")]
    public long QueryScopedNumericClassRows() =>
        RunNumericClassQueryScoped(
            _numericShape,
            _numericContext,
            _numericPath).Consumer;

    [Benchmark(Description = "Legacy object-array materialization")]
    public long LegacyObjectArrayMaterialization()
    {
        var checksum = 0L;
        for (var row = 0; row < Rows; row++)
        {
            var values = new object?[FieldCount];
            for (var column = 0; column < FieldCount; column++)
                values[column] = _materializationValues[column];

            var materialized = new CsvRow(values);
            for (var column = 0; column < FieldCount; column++)
                checksum += ((string?)materialized[column])?.Length ?? 0;
        }

        return checksum;
    }

    [Benchmark(Description = "Query-scoped struct materialization")]
    public int QueryScopedStructMaterialization()
    {
        return FieldCount switch
        {
            2 => MaterializeStringRows<BenchmarkStructRow2, BenchmarkStructMaterializer2>(_materializationValues),
            8 => MaterializeStringRows<BenchmarkStructRow8, BenchmarkStructMaterializer8>(_materializationValues),
            32 => MaterializeStringRows<BenchmarkStructRow32, BenchmarkStructMaterializer32>(_materializationValues),
            64 => MaterializeStringRows<BenchmarkStructRow64, BenchmarkStructMaterializer64>(_materializationValues),
            _ => throw UnsupportedFieldCount()
        };
    }

    [Benchmark(Description = "Query-scoped class materialization")]
    public int QueryScopedClassMaterialization()
    {
        return FieldCount switch
        {
            2 => MaterializeStringRows<BenchmarkClassRow2, BenchmarkClassMaterializer2>(_materializationValues),
            8 => MaterializeStringRows<BenchmarkClassRow8, BenchmarkClassMaterializer8>(_materializationValues),
            32 => MaterializeStringRows<BenchmarkClassRow32, BenchmarkClassMaterializer32>(_materializationValues),
            64 => MaterializeStringRows<BenchmarkClassRow64, BenchmarkClassMaterializer64>(_materializationValues),
            _ => throw UnsupportedFieldCount()
        };
    }

    [Benchmark(Description = "Legacy numeric object-array materialization")]
    public long LegacyNumericObjectArrayMaterialization()
    {
        var checksum = 0L;
        for (var row = 0; row < Rows; row++)
        {
            var values = new object?[FieldCount];
            for (var column = 0; column < FieldCount; column++)
                values[column] = _numericMaterializationValues[column];

            for (var column = 0; column < FieldCount; column++)
                checksum += (int?)values[column] ?? 0;
        }

        return checksum;
    }

    [Benchmark(Description = "Query-scoped numeric struct materialization")]
    public int QueryScopedNumericStructMaterialization()
    {
        return FieldCount switch
        {
            2 => MaterializeNumericRows<BenchmarkNumericRow2, BenchmarkNumericMaterializer2>(_numericMaterializationValues),
            8 => MaterializeNumericRows<BenchmarkNumericRow8, BenchmarkNumericMaterializer8>(_numericMaterializationValues),
            32 => MaterializeNumericRows<BenchmarkNumericRow32, BenchmarkNumericMaterializer32>(_numericMaterializationValues),
            64 => MaterializeNumericRows<BenchmarkNumericRow64, BenchmarkNumericMaterializer64>(_numericMaterializationValues),
            _ => throw UnsupportedFieldCount()
        };
    }

    [Benchmark(Description = "Query-scoped numeric class materialization")]
    public int QueryScopedNumericClassMaterialization()
    {
        return FieldCount switch
        {
            2 => MaterializeNumericRows<BenchmarkNumericClassRow2, BenchmarkNumericClassMaterializer2>(_numericMaterializationValues),
            8 => MaterializeNumericRows<BenchmarkNumericClassRow8, BenchmarkNumericClassMaterializer8>(_numericMaterializationValues),
            32 => MaterializeNumericRows<BenchmarkNumericClassRow32, BenchmarkNumericClassMaterializer32>(_numericMaterializationValues),
            64 => MaterializeNumericRows<BenchmarkNumericClassRow64, BenchmarkNumericClassMaterializer64>(_numericMaterializationValues),
            _ => throw UnsupportedFieldCount()
        };
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int MaterializeStringRows<TRow, TMaterializer>(string[] values)
        where TRow : IBenchmarkRow
        where TMaterializer : struct, IQueryRowMaterializer<TRow>
    {
        var checksum = 0;
        for (var row = 0; row < Rows; row++)
        {
            var reader = new BenchmarkStringFieldReader(values);
            checksum += TMaterializer.Materialize<BenchmarkStringFieldReader>(ref reader).Checksum;
        }

        return checksum;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int MaterializeNumericRows<TRow, TMaterializer>(int[] values)
        where TRow : IBenchmarkRow
        where TMaterializer : struct, IQueryRowMaterializer<TRow>
    {
        var checksum = 0;
        for (var row = 0; row < Rows; row++)
        {
            var reader = new BenchmarkNumericFieldReader(values);
            checksum += TMaterializer.Materialize<BenchmarkNumericFieldReader>(ref reader).Checksum;
        }

        return checksum;
    }

    private BenchmarkOutcome RunQueryScoped<TRow, TMaterializer>(
        QueryRowShape shape,
        SourceExecutionContext context,
        string? path = null)
        where TRow : IBenchmarkRow
        where TMaterializer : struct, IQueryRowMaterializer<TRow>
    {
        var request = new QueryScopedRowSourceRequest(context, shape);
        var source = _queryScopedSchema.GetQueryScopedRowSource<TRow, TMaterializer>(
            CsvSchema.File,
            request,
            path ?? _path,
            true);
        var outcome = new BenchmarkOutcomeBuilder();
        foreach (var chunk in source.Chunks)
        {
            foreach (var row in chunk)
                outcome.Add(row.Checksum);
        }

        return outcome.Build();
    }

    private static BenchmarkOutcome RunLegacy(
        string path,
        SourceExecutionContext context,
        int projectedFieldCount)
    {
        var source = new CsvFileSource(path, hasHeader: true, context);
        var outcome = new BenchmarkOutcomeBuilder();
        var numeric = Nullable.GetUnderlyingType(context.AllColumns.First().ColumnType) == typeof(int);
        foreach (var chunk in source.Chunks)
        {
            foreach (var row in chunk)
            {
                var checksum = 0;
                for (var index = 0; index < projectedFieldCount; index++)
                {
                    checksum += numeric
                        ? (int?)row[index] ?? 0
                        : ((string?)row[index])?.Length ?? 0;
                }

                outcome.Add(checksum);
            }
        }

        return outcome.Build();
    }

    private static QueryRowShape CreateShape(IReadOnlyList<ISchemaColumn> columns)
    {
        return new QueryRowShape(columns
            .Select((column, index) => new QueryRowField(
                index,
                column.ColumnIndex,
                column.ColumnName,
                column.ColumnType,
                isNullable: true,
                column.ReadModifiers))
            .ToArray());
    }

    private string CreateCsvContent(bool numeric)
    {
        var builder = new StringBuilder();
        builder.AppendLine(string.Join(',', _columns.Select(static column => column.ColumnName)));
        for (var row = 0; row < Rows; row++)
        {
            var writtenFields = !numeric && row % 13 == 0 ? FieldCount - 1 : FieldCount;
            for (var column = 0; column < writtenFields; column++)
            {
                if (column > 0)
                    builder.Append(',');

                if (numeric)
                {
                    if (column == 0 || (row + column) % 17 != 0)
                        builder.Append((row * FieldCount + column).ToString(CultureInfo.InvariantCulture));
                }
                else
                {
                    builder.Append("value-");
                    builder.Append(row.ToString(CultureInfo.InvariantCulture));
                    builder.Append('-');
                    builder.Append(column.ToString(CultureInfo.InvariantCulture));
                }
            }

            builder.AppendLine();
        }

        return builder.ToString();
    }

    private string CreateFailureCsvContent()
    {
        return string.Join(',', _columns.Select(static column => column.ColumnName)) +
               Environment.NewLine +
               "\"unterminated";
    }

    private static SourceExecutionContext CreateExecutionContext(
        SourceIdentity identity,
        SourceExecutionPlan plan,
        IReadOnlyCollection<ISchemaColumn> columns)
    {
        return new SourceExecutionContext(
            identity.SourceContextId,
            plan,
            CancellationToken.None,
            columns,
            new Dictionary<string, string>(StringComparer.Ordinal),
            NullLogger.Instance);
    }

    private void VerifyCorrectness(
        string scenario,
        string path,
        QueryRowShape shape,
        SourceExecutionContext context)
    {
        var legacy = RunLegacy(path, context, shape.Fields.Count);
        var structRows = shape.Fields[0].FieldType == typeof(string)
            ? RunStringStructQueryScoped(shape, context, path)
            : RunNumericStructQueryScoped(shape, context, path);
        var classRows = shape.Fields[0].FieldType == typeof(string)
            ? RunStringClassQueryScoped(shape, context, path)
            : RunNumericClassQueryScoped(shape, context, path);

        if (legacy != structRows || legacy != classRows)
        {
            throw new InvalidOperationException(
                $"Query-row correctness oracle failed for {scenario}: " +
                $"legacy={legacy}, struct={structRows}, class={classRows}.");
        }
    }

    private void VerifyFailureBehavior()
    {
        var legacy = CaptureFailure(() => RunLegacy(_failurePath, _context, _shape.Fields.Count));
        var structRows = CaptureFailure(() => RunStringStructQueryScoped(_shape, _context, _failurePath));
        var classRows = CaptureFailure(() => RunStringClassQueryScoped(_shape, _context, _failurePath));
        if (legacy != structRows || legacy != classRows)
        {
            throw new InvalidOperationException(
                $"Query-row failure oracle failed: legacy={legacy}, struct={structRows}, class={classRows}.");
        }
    }

    private static BenchmarkFailure CaptureFailure(Func<BenchmarkOutcome> action)
    {
        try
        {
            _ = action();
            return BenchmarkFailure.None;
        }
        catch (Exception exception)
        {
            return new BenchmarkFailure(exception.GetType().FullName ?? exception.GetType().Name, exception.Message);
        }
    }

    private interface IBenchmarkRow
    {
        int Checksum { get; }
    }

    private readonly record struct BenchmarkOutcome(int RowCount, long Checksum, ulong OrderHash)
    {
        public long Consumer => unchecked(Checksum + RowCount + (long)OrderHash);
    }

    private struct BenchmarkOutcomeBuilder
    {
        private int _rowCount;
        private long _checksum;
        private ulong _orderHash;

        public void Add(int rowChecksum)
        {
            _rowCount++;
            _checksum += rowChecksum;
            _orderHash = unchecked((_orderHash ^ (uint)rowChecksum) * 1099511628211UL);
        }

        public readonly BenchmarkOutcome Build() => new(_rowCount, _checksum, _orderHash);
    }

    private readonly record struct BenchmarkFailure(string Type, string Message)
    {
        public static BenchmarkFailure None { get; } = new(string.Empty, string.Empty);
    }

    private readonly struct BenchmarkStringSlots1(string? v0)
    {
        public int Checksum => (v0?.Length ?? 0);
    }

    private readonly struct BenchmarkStringSlots2(string? v0, string? v1)
    {
        public int Checksum => (v0?.Length ?? 0) + (v1?.Length ?? 0);
    }

    private readonly struct BenchmarkStringSlots8(string? v0, string? v1, string? v2, string? v3, string? v4, string? v5, string? v6, string? v7)
    {
        public int Checksum => (v0?.Length ?? 0) + (v1?.Length ?? 0) + (v2?.Length ?? 0) + (v3?.Length ?? 0) + (v4?.Length ?? 0) + (v5?.Length ?? 0) + (v6?.Length ?? 0) + (v7?.Length ?? 0);
    }

    private readonly struct BenchmarkStringSlots32(string? v0, string? v1, string? v2, string? v3, string? v4, string? v5, string? v6, string? v7, string? v8, string? v9, string? v10, string? v11, string? v12, string? v13, string? v14, string? v15, string? v16, string? v17, string? v18, string? v19, string? v20, string? v21, string? v22, string? v23, string? v24, string? v25, string? v26, string? v27, string? v28, string? v29, string? v30, string? v31)
    {
        public int Checksum => (v0?.Length ?? 0) + (v1?.Length ?? 0) + (v2?.Length ?? 0) + (v3?.Length ?? 0) + (v4?.Length ?? 0) + (v5?.Length ?? 0) + (v6?.Length ?? 0) + (v7?.Length ?? 0) + (v8?.Length ?? 0) + (v9?.Length ?? 0) + (v10?.Length ?? 0) + (v11?.Length ?? 0) + (v12?.Length ?? 0) + (v13?.Length ?? 0) + (v14?.Length ?? 0) + (v15?.Length ?? 0) + (v16?.Length ?? 0) + (v17?.Length ?? 0) + (v18?.Length ?? 0) + (v19?.Length ?? 0) + (v20?.Length ?? 0) + (v21?.Length ?? 0) + (v22?.Length ?? 0) + (v23?.Length ?? 0) + (v24?.Length ?? 0) + (v25?.Length ?? 0) + (v26?.Length ?? 0) + (v27?.Length ?? 0) + (v28?.Length ?? 0) + (v29?.Length ?? 0) + (v30?.Length ?? 0) + (v31?.Length ?? 0);
    }

    private readonly struct BenchmarkStringSlots64(string? v0, string? v1, string? v2, string? v3, string? v4, string? v5, string? v6, string? v7, string? v8, string? v9, string? v10, string? v11, string? v12, string? v13, string? v14, string? v15, string? v16, string? v17, string? v18, string? v19, string? v20, string? v21, string? v22, string? v23, string? v24, string? v25, string? v26, string? v27, string? v28, string? v29, string? v30, string? v31, string? v32, string? v33, string? v34, string? v35, string? v36, string? v37, string? v38, string? v39, string? v40, string? v41, string? v42, string? v43, string? v44, string? v45, string? v46, string? v47, string? v48, string? v49, string? v50, string? v51, string? v52, string? v53, string? v54, string? v55, string? v56, string? v57, string? v58, string? v59, string? v60, string? v61, string? v62, string? v63)
    {
        public int Checksum => (v0?.Length ?? 0) + (v1?.Length ?? 0) + (v2?.Length ?? 0) + (v3?.Length ?? 0) + (v4?.Length ?? 0) + (v5?.Length ?? 0) + (v6?.Length ?? 0) + (v7?.Length ?? 0) + (v8?.Length ?? 0) + (v9?.Length ?? 0) + (v10?.Length ?? 0) + (v11?.Length ?? 0) + (v12?.Length ?? 0) + (v13?.Length ?? 0) + (v14?.Length ?? 0) + (v15?.Length ?? 0) + (v16?.Length ?? 0) + (v17?.Length ?? 0) + (v18?.Length ?? 0) + (v19?.Length ?? 0) + (v20?.Length ?? 0) + (v21?.Length ?? 0) + (v22?.Length ?? 0) + (v23?.Length ?? 0) + (v24?.Length ?? 0) + (v25?.Length ?? 0) + (v26?.Length ?? 0) + (v27?.Length ?? 0) + (v28?.Length ?? 0) + (v29?.Length ?? 0) + (v30?.Length ?? 0) + (v31?.Length ?? 0) + (v32?.Length ?? 0) + (v33?.Length ?? 0) + (v34?.Length ?? 0) + (v35?.Length ?? 0) + (v36?.Length ?? 0) + (v37?.Length ?? 0) + (v38?.Length ?? 0) + (v39?.Length ?? 0) + (v40?.Length ?? 0) + (v41?.Length ?? 0) + (v42?.Length ?? 0) + (v43?.Length ?? 0) + (v44?.Length ?? 0) + (v45?.Length ?? 0) + (v46?.Length ?? 0) + (v47?.Length ?? 0) + (v48?.Length ?? 0) + (v49?.Length ?? 0) + (v50?.Length ?? 0) + (v51?.Length ?? 0) + (v52?.Length ?? 0) + (v53?.Length ?? 0) + (v54?.Length ?? 0) + (v55?.Length ?? 0) + (v56?.Length ?? 0) + (v57?.Length ?? 0) + (v58?.Length ?? 0) + (v59?.Length ?? 0) + (v60?.Length ?? 0) + (v61?.Length ?? 0) + (v62?.Length ?? 0) + (v63?.Length ?? 0);
    }

    private readonly struct BenchmarkStructRow1(BenchmarkStringSlots1 values) : IBenchmarkRow
    {
        public int Checksum => values.Checksum;
    }

    private sealed class BenchmarkClassRow1(BenchmarkStringSlots1 values) : IBenchmarkRow
    {
        public int Checksum => values.Checksum;
    }

    private readonly struct BenchmarkStructRow2(BenchmarkStringSlots2 values) : IBenchmarkRow
    {
        public int Checksum => values.Checksum;
    }

    private sealed class BenchmarkClassRow2(BenchmarkStringSlots2 values) : IBenchmarkRow
    {
        public int Checksum => values.Checksum;
    }

    private readonly struct BenchmarkStructRow8(BenchmarkStringSlots8 values) : IBenchmarkRow
    {
        public int Checksum => values.Checksum;
    }

    private sealed class BenchmarkClassRow8(BenchmarkStringSlots8 values) : IBenchmarkRow
    {
        public int Checksum => values.Checksum;
    }

    private readonly struct BenchmarkStructRow32(BenchmarkStringSlots32 values) : IBenchmarkRow
    {
        public int Checksum => values.Checksum;
    }

    private sealed class BenchmarkClassRow32(BenchmarkStringSlots32 values) : IBenchmarkRow
    {
        public int Checksum => values.Checksum;
    }

    private readonly struct BenchmarkStructRow64(BenchmarkStringSlots64 values) : IBenchmarkRow
    {
        public int Checksum => values.Checksum;
    }

    private sealed class BenchmarkClassRow64(BenchmarkStringSlots64 values) : IBenchmarkRow
    {
        public int Checksum => values.Checksum;
    }

    private readonly struct BenchmarkStructMaterializer1 : IQueryRowMaterializer<BenchmarkStructRow1>
    {
        public static BenchmarkStructRow1 Materialize<TReader>(scoped ref TReader reader)
            where TReader : IQuerySourceFieldReader, allows ref struct
        {
            return new BenchmarkStructRow1(new BenchmarkStringSlots1(reader.Read<string>(0)));
        }
    }

    private readonly struct BenchmarkClassMaterializer1 : IQueryRowMaterializer<BenchmarkClassRow1>
    {
        public static BenchmarkClassRow1 Materialize<TReader>(scoped ref TReader reader)
            where TReader : IQuerySourceFieldReader, allows ref struct
        {
            return new BenchmarkClassRow1(new BenchmarkStringSlots1(reader.Read<string>(0)));
        }
    }

    private readonly struct BenchmarkStructMaterializer2 : IQueryRowMaterializer<BenchmarkStructRow2>
    {
        public static BenchmarkStructRow2 Materialize<TReader>(scoped ref TReader reader)
            where TReader : IQuerySourceFieldReader, allows ref struct
        {
            return new BenchmarkStructRow2(new BenchmarkStringSlots2(reader.Read<string>(0), reader.Read<string>(1)));
        }
    }

    private readonly struct BenchmarkClassMaterializer2 : IQueryRowMaterializer<BenchmarkClassRow2>
    {
        public static BenchmarkClassRow2 Materialize<TReader>(scoped ref TReader reader)
            where TReader : IQuerySourceFieldReader, allows ref struct
        {
            return new BenchmarkClassRow2(new BenchmarkStringSlots2(reader.Read<string>(0), reader.Read<string>(1)));
        }
    }

    private readonly struct BenchmarkStructMaterializer8 : IQueryRowMaterializer<BenchmarkStructRow8>
    {
        public static BenchmarkStructRow8 Materialize<TReader>(scoped ref TReader reader)
            where TReader : IQuerySourceFieldReader, allows ref struct
        {
            return new BenchmarkStructRow8(new BenchmarkStringSlots8(reader.Read<string>(0), reader.Read<string>(1), reader.Read<string>(2), reader.Read<string>(3), reader.Read<string>(4), reader.Read<string>(5), reader.Read<string>(6), reader.Read<string>(7)));
        }
    }

    private readonly struct BenchmarkClassMaterializer8 : IQueryRowMaterializer<BenchmarkClassRow8>
    {
        public static BenchmarkClassRow8 Materialize<TReader>(scoped ref TReader reader)
            where TReader : IQuerySourceFieldReader, allows ref struct
        {
            return new BenchmarkClassRow8(new BenchmarkStringSlots8(reader.Read<string>(0), reader.Read<string>(1), reader.Read<string>(2), reader.Read<string>(3), reader.Read<string>(4), reader.Read<string>(5), reader.Read<string>(6), reader.Read<string>(7)));
        }
    }

    private readonly struct BenchmarkStructMaterializer32 : IQueryRowMaterializer<BenchmarkStructRow32>
    {
        public static BenchmarkStructRow32 Materialize<TReader>(scoped ref TReader reader)
            where TReader : IQuerySourceFieldReader, allows ref struct
        {
            return new BenchmarkStructRow32(new BenchmarkStringSlots32(reader.Read<string>(0), reader.Read<string>(1), reader.Read<string>(2), reader.Read<string>(3), reader.Read<string>(4), reader.Read<string>(5), reader.Read<string>(6), reader.Read<string>(7), reader.Read<string>(8), reader.Read<string>(9), reader.Read<string>(10), reader.Read<string>(11), reader.Read<string>(12), reader.Read<string>(13), reader.Read<string>(14), reader.Read<string>(15), reader.Read<string>(16), reader.Read<string>(17), reader.Read<string>(18), reader.Read<string>(19), reader.Read<string>(20), reader.Read<string>(21), reader.Read<string>(22), reader.Read<string>(23), reader.Read<string>(24), reader.Read<string>(25), reader.Read<string>(26), reader.Read<string>(27), reader.Read<string>(28), reader.Read<string>(29), reader.Read<string>(30), reader.Read<string>(31)));
        }
    }

    private readonly struct BenchmarkClassMaterializer32 : IQueryRowMaterializer<BenchmarkClassRow32>
    {
        public static BenchmarkClassRow32 Materialize<TReader>(scoped ref TReader reader)
            where TReader : IQuerySourceFieldReader, allows ref struct
        {
            return new BenchmarkClassRow32(new BenchmarkStringSlots32(reader.Read<string>(0), reader.Read<string>(1), reader.Read<string>(2), reader.Read<string>(3), reader.Read<string>(4), reader.Read<string>(5), reader.Read<string>(6), reader.Read<string>(7), reader.Read<string>(8), reader.Read<string>(9), reader.Read<string>(10), reader.Read<string>(11), reader.Read<string>(12), reader.Read<string>(13), reader.Read<string>(14), reader.Read<string>(15), reader.Read<string>(16), reader.Read<string>(17), reader.Read<string>(18), reader.Read<string>(19), reader.Read<string>(20), reader.Read<string>(21), reader.Read<string>(22), reader.Read<string>(23), reader.Read<string>(24), reader.Read<string>(25), reader.Read<string>(26), reader.Read<string>(27), reader.Read<string>(28), reader.Read<string>(29), reader.Read<string>(30), reader.Read<string>(31)));
        }
    }

    private readonly struct BenchmarkStructMaterializer64 : IQueryRowMaterializer<BenchmarkStructRow64>
    {
        public static BenchmarkStructRow64 Materialize<TReader>(scoped ref TReader reader)
            where TReader : IQuerySourceFieldReader, allows ref struct
        {
            return new BenchmarkStructRow64(new BenchmarkStringSlots64(reader.Read<string>(0), reader.Read<string>(1), reader.Read<string>(2), reader.Read<string>(3), reader.Read<string>(4), reader.Read<string>(5), reader.Read<string>(6), reader.Read<string>(7), reader.Read<string>(8), reader.Read<string>(9), reader.Read<string>(10), reader.Read<string>(11), reader.Read<string>(12), reader.Read<string>(13), reader.Read<string>(14), reader.Read<string>(15), reader.Read<string>(16), reader.Read<string>(17), reader.Read<string>(18), reader.Read<string>(19), reader.Read<string>(20), reader.Read<string>(21), reader.Read<string>(22), reader.Read<string>(23), reader.Read<string>(24), reader.Read<string>(25), reader.Read<string>(26), reader.Read<string>(27), reader.Read<string>(28), reader.Read<string>(29), reader.Read<string>(30), reader.Read<string>(31), reader.Read<string>(32), reader.Read<string>(33), reader.Read<string>(34), reader.Read<string>(35), reader.Read<string>(36), reader.Read<string>(37), reader.Read<string>(38), reader.Read<string>(39), reader.Read<string>(40), reader.Read<string>(41), reader.Read<string>(42), reader.Read<string>(43), reader.Read<string>(44), reader.Read<string>(45), reader.Read<string>(46), reader.Read<string>(47), reader.Read<string>(48), reader.Read<string>(49), reader.Read<string>(50), reader.Read<string>(51), reader.Read<string>(52), reader.Read<string>(53), reader.Read<string>(54), reader.Read<string>(55), reader.Read<string>(56), reader.Read<string>(57), reader.Read<string>(58), reader.Read<string>(59), reader.Read<string>(60), reader.Read<string>(61), reader.Read<string>(62), reader.Read<string>(63)));
        }
    }

    private readonly struct BenchmarkClassMaterializer64 : IQueryRowMaterializer<BenchmarkClassRow64>
    {
        public static BenchmarkClassRow64 Materialize<TReader>(scoped ref TReader reader)
            where TReader : IQuerySourceFieldReader, allows ref struct
        {
            return new BenchmarkClassRow64(new BenchmarkStringSlots64(reader.Read<string>(0), reader.Read<string>(1), reader.Read<string>(2), reader.Read<string>(3), reader.Read<string>(4), reader.Read<string>(5), reader.Read<string>(6), reader.Read<string>(7), reader.Read<string>(8), reader.Read<string>(9), reader.Read<string>(10), reader.Read<string>(11), reader.Read<string>(12), reader.Read<string>(13), reader.Read<string>(14), reader.Read<string>(15), reader.Read<string>(16), reader.Read<string>(17), reader.Read<string>(18), reader.Read<string>(19), reader.Read<string>(20), reader.Read<string>(21), reader.Read<string>(22), reader.Read<string>(23), reader.Read<string>(24), reader.Read<string>(25), reader.Read<string>(26), reader.Read<string>(27), reader.Read<string>(28), reader.Read<string>(29), reader.Read<string>(30), reader.Read<string>(31), reader.Read<string>(32), reader.Read<string>(33), reader.Read<string>(34), reader.Read<string>(35), reader.Read<string>(36), reader.Read<string>(37), reader.Read<string>(38), reader.Read<string>(39), reader.Read<string>(40), reader.Read<string>(41), reader.Read<string>(42), reader.Read<string>(43), reader.Read<string>(44), reader.Read<string>(45), reader.Read<string>(46), reader.Read<string>(47), reader.Read<string>(48), reader.Read<string>(49), reader.Read<string>(50), reader.Read<string>(51), reader.Read<string>(52), reader.Read<string>(53), reader.Read<string>(54), reader.Read<string>(55), reader.Read<string>(56), reader.Read<string>(57), reader.Read<string>(58), reader.Read<string>(59), reader.Read<string>(60), reader.Read<string>(61), reader.Read<string>(62), reader.Read<string>(63)));
        }
    }

    private readonly struct BenchmarkNumericSlots1(int v0)
    {
        public int Checksum => v0;
    }

    private readonly struct BenchmarkNumericRow1(BenchmarkNumericSlots1 values) : IBenchmarkRow
    {
        public int Checksum => values.Checksum;
    }

    private readonly struct BenchmarkNumericMaterializer1 : IQueryRowMaterializer<BenchmarkNumericRow1>
    {
        public static BenchmarkNumericRow1 Materialize<TReader>(scoped ref TReader reader)
            where TReader : IQuerySourceFieldReader, allows ref struct
        {
            return new BenchmarkNumericRow1(new BenchmarkNumericSlots1(reader.Read<int>(0)));
        }
    }

    private readonly struct BenchmarkNumericSlots2(int v0, int v1)
    {
        public int Checksum => v0 + v1;
    }

    private readonly struct BenchmarkNumericRow2(BenchmarkNumericSlots2 values) : IBenchmarkRow
    {
        public int Checksum => values.Checksum;
    }

    private readonly struct BenchmarkNumericMaterializer2 : IQueryRowMaterializer<BenchmarkNumericRow2>
    {
        public static BenchmarkNumericRow2 Materialize<TReader>(scoped ref TReader reader)
            where TReader : IQuerySourceFieldReader, allows ref struct
        {
            return new BenchmarkNumericRow2(new BenchmarkNumericSlots2(reader.Read<int>(0), reader.Read<int>(1)));
        }
    }

    private readonly struct BenchmarkNumericSlots8(int v0, int v1, int v2, int v3, int v4, int v5, int v6, int v7)
    {
        public int Checksum => v0 + v1 + v2 + v3 + v4 + v5 + v6 + v7;
    }

    private readonly struct BenchmarkNumericRow8(BenchmarkNumericSlots8 values) : IBenchmarkRow
    {
        public int Checksum => values.Checksum;
    }

    private readonly struct BenchmarkNumericMaterializer8 : IQueryRowMaterializer<BenchmarkNumericRow8>
    {
        public static BenchmarkNumericRow8 Materialize<TReader>(scoped ref TReader reader)
            where TReader : IQuerySourceFieldReader, allows ref struct
        {
            return new BenchmarkNumericRow8(new BenchmarkNumericSlots8(reader.Read<int>(0), reader.Read<int>(1), reader.Read<int>(2), reader.Read<int>(3), reader.Read<int>(4), reader.Read<int>(5), reader.Read<int>(6), reader.Read<int>(7)));
        }
    }

    private readonly struct BenchmarkNumericSlots32(int v0, int v1, int v2, int v3, int v4, int v5, int v6, int v7, int v8, int v9, int v10, int v11, int v12, int v13, int v14, int v15, int v16, int v17, int v18, int v19, int v20, int v21, int v22, int v23, int v24, int v25, int v26, int v27, int v28, int v29, int v30, int v31)
    {
        public int Checksum => v0 + v1 + v2 + v3 + v4 + v5 + v6 + v7 + v8 + v9 + v10 + v11 + v12 + v13 + v14 + v15 + v16 + v17 + v18 + v19 + v20 + v21 + v22 + v23 + v24 + v25 + v26 + v27 + v28 + v29 + v30 + v31;
    }

    private readonly struct BenchmarkNumericRow32(BenchmarkNumericSlots32 values) : IBenchmarkRow
    {
        public int Checksum => values.Checksum;
    }

    private readonly struct BenchmarkNumericMaterializer32 : IQueryRowMaterializer<BenchmarkNumericRow32>
    {
        public static BenchmarkNumericRow32 Materialize<TReader>(scoped ref TReader reader)
            where TReader : IQuerySourceFieldReader, allows ref struct
        {
            return new BenchmarkNumericRow32(new BenchmarkNumericSlots32(reader.Read<int>(0), reader.Read<int>(1), reader.Read<int>(2), reader.Read<int>(3), reader.Read<int>(4), reader.Read<int>(5), reader.Read<int>(6), reader.Read<int>(7), reader.Read<int>(8), reader.Read<int>(9), reader.Read<int>(10), reader.Read<int>(11), reader.Read<int>(12), reader.Read<int>(13), reader.Read<int>(14), reader.Read<int>(15), reader.Read<int>(16), reader.Read<int>(17), reader.Read<int>(18), reader.Read<int>(19), reader.Read<int>(20), reader.Read<int>(21), reader.Read<int>(22), reader.Read<int>(23), reader.Read<int>(24), reader.Read<int>(25), reader.Read<int>(26), reader.Read<int>(27), reader.Read<int>(28), reader.Read<int>(29), reader.Read<int>(30), reader.Read<int>(31)));
        }
    }

    private readonly struct BenchmarkNumericSlots64(int v0, int v1, int v2, int v3, int v4, int v5, int v6, int v7, int v8, int v9, int v10, int v11, int v12, int v13, int v14, int v15, int v16, int v17, int v18, int v19, int v20, int v21, int v22, int v23, int v24, int v25, int v26, int v27, int v28, int v29, int v30, int v31, int v32, int v33, int v34, int v35, int v36, int v37, int v38, int v39, int v40, int v41, int v42, int v43, int v44, int v45, int v46, int v47, int v48, int v49, int v50, int v51, int v52, int v53, int v54, int v55, int v56, int v57, int v58, int v59, int v60, int v61, int v62, int v63)
    {
        public int Checksum => v0 + v1 + v2 + v3 + v4 + v5 + v6 + v7 + v8 + v9 + v10 + v11 + v12 + v13 + v14 + v15 + v16 + v17 + v18 + v19 + v20 + v21 + v22 + v23 + v24 + v25 + v26 + v27 + v28 + v29 + v30 + v31 + v32 + v33 + v34 + v35 + v36 + v37 + v38 + v39 + v40 + v41 + v42 + v43 + v44 + v45 + v46 + v47 + v48 + v49 + v50 + v51 + v52 + v53 + v54 + v55 + v56 + v57 + v58 + v59 + v60 + v61 + v62 + v63;
    }

    private readonly struct BenchmarkNumericRow64(BenchmarkNumericSlots64 values) : IBenchmarkRow
    {
        public int Checksum => values.Checksum;
    }

    private readonly struct BenchmarkNumericMaterializer64 : IQueryRowMaterializer<BenchmarkNumericRow64>
    {
        public static BenchmarkNumericRow64 Materialize<TReader>(scoped ref TReader reader)
            where TReader : IQuerySourceFieldReader, allows ref struct
        {
            return new BenchmarkNumericRow64(new BenchmarkNumericSlots64(reader.Read<int>(0), reader.Read<int>(1), reader.Read<int>(2), reader.Read<int>(3), reader.Read<int>(4), reader.Read<int>(5), reader.Read<int>(6), reader.Read<int>(7), reader.Read<int>(8), reader.Read<int>(9), reader.Read<int>(10), reader.Read<int>(11), reader.Read<int>(12), reader.Read<int>(13), reader.Read<int>(14), reader.Read<int>(15), reader.Read<int>(16), reader.Read<int>(17), reader.Read<int>(18), reader.Read<int>(19), reader.Read<int>(20), reader.Read<int>(21), reader.Read<int>(22), reader.Read<int>(23), reader.Read<int>(24), reader.Read<int>(25), reader.Read<int>(26), reader.Read<int>(27), reader.Read<int>(28), reader.Read<int>(29), reader.Read<int>(30), reader.Read<int>(31), reader.Read<int>(32), reader.Read<int>(33), reader.Read<int>(34), reader.Read<int>(35), reader.Read<int>(36), reader.Read<int>(37), reader.Read<int>(38), reader.Read<int>(39), reader.Read<int>(40), reader.Read<int>(41), reader.Read<int>(42), reader.Read<int>(43), reader.Read<int>(44), reader.Read<int>(45), reader.Read<int>(46), reader.Read<int>(47), reader.Read<int>(48), reader.Read<int>(49), reader.Read<int>(50), reader.Read<int>(51), reader.Read<int>(52), reader.Read<int>(53), reader.Read<int>(54), reader.Read<int>(55), reader.Read<int>(56), reader.Read<int>(57), reader.Read<int>(58), reader.Read<int>(59), reader.Read<int>(60), reader.Read<int>(61), reader.Read<int>(62), reader.Read<int>(63)));
        }
    }

    private sealed class BenchmarkNumericClassRow1(BenchmarkNumericRow1 row) : IBenchmarkRow
    {
        public int Checksum => row.Checksum;
    }

    private sealed class BenchmarkNumericClassRow2(BenchmarkNumericRow2 row) : IBenchmarkRow
    {
        public int Checksum => row.Checksum;
    }

    private sealed class BenchmarkNumericClassRow8(BenchmarkNumericRow8 row) : IBenchmarkRow
    {
        public int Checksum => row.Checksum;
    }

    private sealed class BenchmarkNumericClassRow32(BenchmarkNumericRow32 row) : IBenchmarkRow
    {
        public int Checksum => row.Checksum;
    }

    private sealed class BenchmarkNumericClassRow64(BenchmarkNumericRow64 row) : IBenchmarkRow
    {
        public int Checksum => row.Checksum;
    }

    private readonly struct BenchmarkNumericClassMaterializer1 : IQueryRowMaterializer<BenchmarkNumericClassRow1>
    {
        public static BenchmarkNumericClassRow1 Materialize<TReader>(scoped ref TReader reader)
            where TReader : IQuerySourceFieldReader, allows ref struct =>
            new(BenchmarkNumericMaterializer1.Materialize<TReader>(ref reader));
    }

    private readonly struct BenchmarkNumericClassMaterializer2 : IQueryRowMaterializer<BenchmarkNumericClassRow2>
    {
        public static BenchmarkNumericClassRow2 Materialize<TReader>(scoped ref TReader reader)
            where TReader : IQuerySourceFieldReader, allows ref struct =>
            new(BenchmarkNumericMaterializer2.Materialize<TReader>(ref reader));
    }

    private readonly struct BenchmarkNumericClassMaterializer8 : IQueryRowMaterializer<BenchmarkNumericClassRow8>
    {
        public static BenchmarkNumericClassRow8 Materialize<TReader>(scoped ref TReader reader)
            where TReader : IQuerySourceFieldReader, allows ref struct =>
            new(BenchmarkNumericMaterializer8.Materialize<TReader>(ref reader));
    }

    private readonly struct BenchmarkNumericClassMaterializer32 : IQueryRowMaterializer<BenchmarkNumericClassRow32>
    {
        public static BenchmarkNumericClassRow32 Materialize<TReader>(scoped ref TReader reader)
            where TReader : IQuerySourceFieldReader, allows ref struct =>
            new(BenchmarkNumericMaterializer32.Materialize<TReader>(ref reader));
    }

    private readonly struct BenchmarkNumericClassMaterializer64 : IQueryRowMaterializer<BenchmarkNumericClassRow64>
    {
        public static BenchmarkNumericClassRow64 Materialize<TReader>(scoped ref TReader reader)
            where TReader : IQuerySourceFieldReader, allows ref struct =>
            new(BenchmarkNumericMaterializer64.Materialize<TReader>(ref reader));
    }

    [InlineArray(2)]
    private struct BenchmarkNullableNumericSlots2
    {
        private int? _element0;
    }

    [InlineArray(8)]
    private struct BenchmarkNullableNumericSlots8
    {
        private int? _element0;
    }

    [InlineArray(32)]
    private struct BenchmarkNullableNumericSlots32
    {
        private int? _element0;
    }

    [InlineArray(64)]
    private struct BenchmarkNullableNumericSlots64
    {
        private int? _element0;
    }

    private readonly struct BenchmarkNullableNumericStructRow2(BenchmarkNullableNumericSlots2 values) : IBenchmarkRow
    {
        public int Checksum => Sum(values, 2);
    }

    private sealed class BenchmarkNullableNumericClassRow2(BenchmarkNullableNumericSlots2 values) : IBenchmarkRow
    {
        public int Checksum => Sum(values, 2);
    }

    private readonly struct BenchmarkNullableNumericStructRow8(BenchmarkNullableNumericSlots8 values) : IBenchmarkRow
    {
        public int Checksum => Sum(values, 8);
    }

    private sealed class BenchmarkNullableNumericClassRow8(BenchmarkNullableNumericSlots8 values) : IBenchmarkRow
    {
        public int Checksum => Sum(values, 8);
    }

    private readonly struct BenchmarkNullableNumericStructRow32(BenchmarkNullableNumericSlots32 values) : IBenchmarkRow
    {
        public int Checksum => Sum(values, 32);
    }

    private sealed class BenchmarkNullableNumericClassRow32(BenchmarkNullableNumericSlots32 values) : IBenchmarkRow
    {
        public int Checksum => Sum(values, 32);
    }

    private readonly struct BenchmarkNullableNumericStructRow64(BenchmarkNullableNumericSlots64 values) : IBenchmarkRow
    {
        public int Checksum => Sum(values, 64);
    }

    private sealed class BenchmarkNullableNumericClassRow64(BenchmarkNullableNumericSlots64 values) : IBenchmarkRow
    {
        public int Checksum => Sum(values, 64);
    }

    private readonly struct BenchmarkNullableNumericStructMaterializer2 : IQueryRowMaterializer<BenchmarkNullableNumericStructRow2>
    {
        public static BenchmarkNullableNumericStructRow2 Materialize<TReader>(scoped ref TReader reader)
            where TReader : IQuerySourceFieldReader, allows ref struct
        {
            var values = default(BenchmarkNullableNumericSlots2);
            for (var slot = 0; slot < 2; slot++)
                values[slot] = reader.Read<int?>(slot);
            return new BenchmarkNullableNumericStructRow2(values);
        }
    }

    private readonly struct BenchmarkNullableNumericClassMaterializer2 : IQueryRowMaterializer<BenchmarkNullableNumericClassRow2>
    {
        public static BenchmarkNullableNumericClassRow2 Materialize<TReader>(scoped ref TReader reader)
            where TReader : IQuerySourceFieldReader, allows ref struct
        {
            var values = default(BenchmarkNullableNumericSlots2);
            for (var slot = 0; slot < 2; slot++)
                values[slot] = reader.Read<int?>(slot);
            return new BenchmarkNullableNumericClassRow2(values);
        }
    }

    private readonly struct BenchmarkNullableNumericStructMaterializer8 : IQueryRowMaterializer<BenchmarkNullableNumericStructRow8>
    {
        public static BenchmarkNullableNumericStructRow8 Materialize<TReader>(scoped ref TReader reader)
            where TReader : IQuerySourceFieldReader, allows ref struct
        {
            var values = default(BenchmarkNullableNumericSlots8);
            for (var slot = 0; slot < 8; slot++)
                values[slot] = reader.Read<int?>(slot);
            return new BenchmarkNullableNumericStructRow8(values);
        }
    }

    private readonly struct BenchmarkNullableNumericClassMaterializer8 : IQueryRowMaterializer<BenchmarkNullableNumericClassRow8>
    {
        public static BenchmarkNullableNumericClassRow8 Materialize<TReader>(scoped ref TReader reader)
            where TReader : IQuerySourceFieldReader, allows ref struct
        {
            var values = default(BenchmarkNullableNumericSlots8);
            for (var slot = 0; slot < 8; slot++)
                values[slot] = reader.Read<int?>(slot);
            return new BenchmarkNullableNumericClassRow8(values);
        }
    }

    private readonly struct BenchmarkNullableNumericStructMaterializer32 : IQueryRowMaterializer<BenchmarkNullableNumericStructRow32>
    {
        public static BenchmarkNullableNumericStructRow32 Materialize<TReader>(scoped ref TReader reader)
            where TReader : IQuerySourceFieldReader, allows ref struct
        {
            var values = default(BenchmarkNullableNumericSlots32);
            for (var slot = 0; slot < 32; slot++)
                values[slot] = reader.Read<int?>(slot);
            return new BenchmarkNullableNumericStructRow32(values);
        }
    }

    private readonly struct BenchmarkNullableNumericClassMaterializer32 : IQueryRowMaterializer<BenchmarkNullableNumericClassRow32>
    {
        public static BenchmarkNullableNumericClassRow32 Materialize<TReader>(scoped ref TReader reader)
            where TReader : IQuerySourceFieldReader, allows ref struct
        {
            var values = default(BenchmarkNullableNumericSlots32);
            for (var slot = 0; slot < 32; slot++)
                values[slot] = reader.Read<int?>(slot);
            return new BenchmarkNullableNumericClassRow32(values);
        }
    }

    private readonly struct BenchmarkNullableNumericStructMaterializer64 : IQueryRowMaterializer<BenchmarkNullableNumericStructRow64>
    {
        public static BenchmarkNullableNumericStructRow64 Materialize<TReader>(scoped ref TReader reader)
            where TReader : IQuerySourceFieldReader, allows ref struct
        {
            var values = default(BenchmarkNullableNumericSlots64);
            for (var slot = 0; slot < 64; slot++)
                values[slot] = reader.Read<int?>(slot);
            return new BenchmarkNullableNumericStructRow64(values);
        }
    }

    private readonly struct BenchmarkNullableNumericClassMaterializer64 : IQueryRowMaterializer<BenchmarkNullableNumericClassRow64>
    {
        public static BenchmarkNullableNumericClassRow64 Materialize<TReader>(scoped ref TReader reader)
            where TReader : IQuerySourceFieldReader, allows ref struct
        {
            var values = default(BenchmarkNullableNumericSlots64);
            for (var slot = 0; slot < 64; slot++)
                values[slot] = reader.Read<int?>(slot);
            return new BenchmarkNullableNumericClassRow64(values);
        }
    }

    private static int Sum(BenchmarkNullableNumericSlots2 values, int count)
    {
        var checksum = 0;
        for (var index = 0; index < count; index++)
            checksum += values[index] ?? 0;
        return checksum;
    }

    private static int Sum(BenchmarkNullableNumericSlots8 values, int count)
    {
        var checksum = 0;
        for (var index = 0; index < count; index++)
            checksum += values[index] ?? 0;
        return checksum;
    }

    private static int Sum(BenchmarkNullableNumericSlots32 values, int count)
    {
        var checksum = 0;
        for (var index = 0; index < count; index++)
            checksum += values[index] ?? 0;
        return checksum;
    }

    private static int Sum(BenchmarkNullableNumericSlots64 values, int count)
    {
        var checksum = 0;
        for (var index = 0; index < count; index++)
            checksum += values[index] ?? 0;
        return checksum;
    }

    private BenchmarkOutcome RunStringStructQueryScoped(
        QueryRowShape shape,
        SourceExecutionContext context,
        string? path = null)
    {
        return shape.Fields.Count switch
        {
            1 => RunQueryScoped<BenchmarkStructRow1, BenchmarkStructMaterializer1>(shape, context, path),
            2 => RunQueryScoped<BenchmarkStructRow2, BenchmarkStructMaterializer2>(shape, context, path),
            8 => RunQueryScoped<BenchmarkStructRow8, BenchmarkStructMaterializer8>(shape, context, path),
            32 => RunQueryScoped<BenchmarkStructRow32, BenchmarkStructMaterializer32>(shape, context, path),
            64 => RunQueryScoped<BenchmarkStructRow64, BenchmarkStructMaterializer64>(shape, context, path),
            _ => throw UnsupportedFieldCount(shape.Fields.Count)
        };
    }

    private BenchmarkOutcome RunStringClassQueryScoped(
        QueryRowShape shape,
        SourceExecutionContext context,
        string? path = null)
    {
        return shape.Fields.Count switch
        {
            1 => RunQueryScoped<BenchmarkClassRow1, BenchmarkClassMaterializer1>(shape, context, path),
            2 => RunQueryScoped<BenchmarkClassRow2, BenchmarkClassMaterializer2>(shape, context, path),
            8 => RunQueryScoped<BenchmarkClassRow8, BenchmarkClassMaterializer8>(shape, context, path),
            32 => RunQueryScoped<BenchmarkClassRow32, BenchmarkClassMaterializer32>(shape, context, path),
            64 => RunQueryScoped<BenchmarkClassRow64, BenchmarkClassMaterializer64>(shape, context, path),
            _ => throw UnsupportedFieldCount(shape.Fields.Count)
        };
    }

    private BenchmarkOutcome RunNumericStructQueryScoped(
        QueryRowShape shape,
        SourceExecutionContext context,
        string? path)
    {
        return shape.Fields.Count switch
        {
            2 => RunQueryScoped<BenchmarkNullableNumericStructRow2, BenchmarkNullableNumericStructMaterializer2>(shape, context, path),
            8 => RunQueryScoped<BenchmarkNullableNumericStructRow8, BenchmarkNullableNumericStructMaterializer8>(shape, context, path),
            32 => RunQueryScoped<BenchmarkNullableNumericStructRow32, BenchmarkNullableNumericStructMaterializer32>(shape, context, path),
            64 => RunQueryScoped<BenchmarkNullableNumericStructRow64, BenchmarkNullableNumericStructMaterializer64>(shape, context, path),
            _ => throw UnsupportedFieldCount(shape.Fields.Count)
        };
    }

    private BenchmarkOutcome RunNumericClassQueryScoped(
        QueryRowShape shape,
        SourceExecutionContext context,
        string? path)
    {
        return shape.Fields.Count switch
        {
            2 => RunQueryScoped<BenchmarkNullableNumericClassRow2, BenchmarkNullableNumericClassMaterializer2>(shape, context, path),
            8 => RunQueryScoped<BenchmarkNullableNumericClassRow8, BenchmarkNullableNumericClassMaterializer8>(shape, context, path),
            32 => RunQueryScoped<BenchmarkNullableNumericClassRow32, BenchmarkNullableNumericClassMaterializer32>(shape, context, path),
            64 => RunQueryScoped<BenchmarkNullableNumericClassRow64, BenchmarkNullableNumericClassMaterializer64>(shape, context, path),
            _ => throw UnsupportedFieldCount(shape.Fields.Count)
        };
    }

    private InvalidOperationException UnsupportedFieldCount() => UnsupportedFieldCount(FieldCount);

    private static InvalidOperationException UnsupportedFieldCount(int count) =>
        new($"Unsupported benchmark field count {count}.");

    private ref struct BenchmarkStringFieldReader(string[] values) : IQuerySourceFieldReader
    {
        public T Read<T>(int slot)
        {
            if (typeof(T) != typeof(string))
                throw new InvalidOperationException($"Unexpected benchmark field type {typeof(T)}.");

            var value = values[slot];
            return Unsafe.As<string, T>(ref value);
        }
    }

    private ref struct BenchmarkNumericFieldReader(int[] values) : IQuerySourceFieldReader
    {
        public T Read<T>(int slot)
        {
            if (typeof(T) != typeof(int))
                throw new InvalidOperationException($"Unexpected benchmark field type {typeof(T)}.");

            var value = values[slot];
            return Unsafe.As<int, T>(ref value);
        }
    }
}
