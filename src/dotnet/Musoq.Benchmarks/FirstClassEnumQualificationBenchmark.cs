using BenchmarkDotNet.Attributes;
using System.Runtime.CompilerServices;
using Musoq.Benchmarks.Components;
using Musoq.Converter;
using Musoq.Evaluator;
using Musoq.Evaluator.Tables;
using Musoq.Plugins;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;

namespace Musoq.Benchmarks;

public enum FirstClassEnumScenario
{
    Equality,
    In,
    Flags,
    Join,
    Grouping,
    Distinct,
    Helpers,
    Projection
}

/// <summary>
/// Compares logical enum queries with carrier-identical primitive queries. Query
/// compilation is excluded; table materialization remains in both sides so the
/// existing final-row boxing cost cancels out of the paired comparison.
/// </summary>
[MemoryDiagnoser]
[MediumRunJob]
public class FirstClassEnumQualificationBenchmark
{
    private FirstClassEnumBenchmarkPair _pair = null!;

    [ParamsAllValues]
    public FirstClassEnumScenario Scenario { get; set; }

    [Params(8192)]
    public int RowsCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _pair = FirstClassEnumBenchmarkSupport.CompilePair(Scenario, RowsCount);
        var carrier = _pair.ExecuteCarrier();
        var logicalEnum = _pair.ExecuteEnum();
        if (carrier != logicalEnum)
        {
            throw new InvalidOperationException(
                $"Enum benchmark correctness oracle failed for {Scenario}: " +
                $"carrier={carrier}, enum={logicalEnum}.");
        }
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _pair.Dispose();
    }

    [Benchmark(Baseline = true)]
    public long ExecuteCarrier() => _pair.ExecuteCarrier().Consumer;

    [Benchmark]
    public long ExecuteEnum() => _pair.ExecuteEnum().Consumer;
}

internal static class FirstClassEnumBenchmarkSupport
{
    private enum QueryVariant
    {
        Carrier,
        LogicalEnum
    }

    private static readonly CompilationOptions Options = BenchmarkCompilationOptions.Materialized(
        new CompilationOptions(ParallelizationMode.None));

    public static FirstClassEnumBenchmarkPair CompilePair(
        FirstClassEnumScenario scenario,
        int rowsCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(rowsCount);
        var provider = FirstClassEnumBenchmarkSchemaProvider.Create(rowsCount);
        var logger = new BenchmarkLoggerResolver();

        var carrier = InstanceCreator.CompileForExecution(
            QueryFor(scenario, QueryVariant.Carrier),
            $"EnumQualification_Carrier_{scenario}_{Guid.NewGuid():N}",
            provider,
            logger,
            Options);
        var logicalEnum = InstanceCreator.CompileForExecution(
            QueryFor(scenario, QueryVariant.LogicalEnum),
            $"EnumQualification_Enum_{scenario}_{Guid.NewGuid():N}",
            provider,
            logger,
            Options);

        return new FirstClassEnumBenchmarkPair(carrier, logicalEnum);
    }

    public static QueryInspectionResult InspectEnum(FirstClassEnumScenario scenario)
    {
        return InstanceCreator.CompileForInspection(
            QueryFor(scenario, QueryVariant.LogicalEnum),
            $"EnumQualification_Inspection_{scenario}_{Guid.NewGuid():N}",
            FirstClassEnumBenchmarkSchemaProvider.Create(8),
            new BenchmarkLoggerResolver(),
            new CompilationOptions(ParallelizationMode.None));
    }

    public static QueryInspectionResult InspectCarrier(FirstClassEnumScenario scenario)
    {
        return InstanceCreator.CompileForInspection(
            QueryFor(scenario, QueryVariant.Carrier),
            $"EnumQualification_CarrierInspection_{scenario}_{Guid.NewGuid():N}",
            FirstClassEnumBenchmarkSchemaProvider.Create(8),
            new BenchmarkLoggerResolver(),
            new CompilationOptions(ParallelizationMode.None));
    }

    public static CompiledQuery CompileEnumShapeProbe(FirstClassEnumScenario scenario)
    {
        return InstanceCreator.CompileForExecution(
            QueryFor(scenario, QueryVariant.LogicalEnum),
            $"EnumQualification_ShapeProbe_{scenario}_{Guid.NewGuid():N}",
            FirstClassEnumBenchmarkSchemaProvider.Create(64),
            new BenchmarkLoggerResolver(),
            new CompilationOptions(ParallelizationMode.None));
    }

    private static string QueryFor(FirstClassEnumScenario scenario, QueryVariant variant)
    {
        var logicalEnum = variant == QueryVariant.LogicalEnum;
        var contract = logicalEnum
            ? "enum JobStatus : short { Queued = 10s, Running = 20s, Finished = 30s };" +
              "flags enum FileAccess : uint { None = 0ui, Read = 1ui, Write = 2ui, ReadWrite = 3ui };" +
              "table BenchmarkRows { Id: int, Status: JobStatus, Access: FileAccess };" +
              "couple #enumperf.rows with table BenchmarkRows as Data;"
            : "table BenchmarkRows { Id: int, Status: short, Access: uint };" +
              "couple #carrierperf.rows with table BenchmarkRows as Data;";
        var query = scenario switch
        {
            FirstClassEnumScenario.Equality =>
                "select Count(e.Id) as Total from Data() e where e.Status = " +
                (logicalEnum ? "'Running'" : "20s"),
            FirstClassEnumScenario.In =>
                "select Count(e.Id) as Total from Data() e where e.Status in " +
                (logicalEnum ? "('Queued', 'Running')" : "(10s, 20s)"),
            FirstClassEnumScenario.Flags =>
                logicalEnum
                    ? "select Count(e.Id) as Total from Data() e " +
                      "where HasAllFlags(e.Access, 'Read', 'Write')"
                    : "select Count(e.Id) as Total from Data() e " +
                      "where (e.Access & 3ui) = 3ui",
            FirstClassEnumScenario.Join =>
                "select Count(a.Id) as Total from Data() a " +
                "inner join Data() b on a.Status = b.Status",
            FirstClassEnumScenario.Grouping =>
                "select e.Status, Count(e.Status) as Total from Data() e group by e.Status",
            FirstClassEnumScenario.Distinct =>
                "select distinct e.Status from Data() e",
            FirstClassEnumScenario.Helpers =>
                logicalEnum
                    ? "select EnumValue(e.Status) as StatusValue, EnumName(e.Status) as StatusName, " +
                      "IsDefined(e.Status) as StatusDefined, HasAnyFlags(e.Access, 'Read') as HasRead, " +
                      "HasAllFlags(e.Access, 'Read', 'Write') as HasReadWrite from Data() e"
                    : "select e.Status as StatusValue, " +
                      "case when e.Status = 10s then 'Queued' when e.Status = 20s then 'Running' " +
                      "when e.Status = 30s then 'Finished' else null end as StatusName, " +
                      "e.Status in (10s, 20s, 30s) as StatusDefined, " +
                      "(e.Access & 1ui) <> 0ui as HasRead, " +
                      "(e.Access & 3ui) = 3ui as HasReadWrite from Data() e",
            FirstClassEnumScenario.Projection =>
                "select e.Status, e.Access from Data() e",
            _ => throw new ArgumentOutOfRangeException(nameof(scenario), scenario, null)
        };

        return contract + query;
    }
}

internal sealed class FirstClassEnumBenchmarkPair(
    CompiledQuery carrier,
    CompiledQuery logicalEnum) : IDisposable
{
    internal CompiledQuery CarrierQuery { get; } = carrier;

    internal CompiledQuery EnumQuery { get; } = logicalEnum;

    public FirstClassEnumBenchmarkOutcome ExecuteCarrier() => Execute(CarrierQuery);

    public FirstClassEnumBenchmarkOutcome ExecuteEnum() => Execute(EnumQuery);

    public void Dispose()
    {
        CarrierQuery.Dispose();
        EnumQuery.Dispose();
    }

    private static FirstClassEnumBenchmarkOutcome Execute(CompiledQuery query)
    {
        using var table = query.Run();
        var checksum = 0L;
        foreach (var row in table)
        foreach (var value in row.Values)
            checksum = unchecked(checksum * 397 + ValueChecksum(value));

        return new FirstClassEnumBenchmarkOutcome(table.Count, checksum);
    }

    private static long ValueChecksum(object? value)
    {
        return value switch
        {
            null => 0,
            byte number => number,
            sbyte number => number,
            short number => number,
            ushort number => number,
            int number => number,
            uint number => number,
            long number => number,
            ulong number => unchecked((long)number),
            bool boolean => boolean ? 1 : 0,
            string text => StringChecksum(text),
            _ => throw new InvalidOperationException(
                $"Unexpected enum benchmark result value '{value.GetType()}'.")
        };
    }

    private static long StringChecksum(string value)
    {
        var checksum = 17L;
        foreach (var character in value)
            checksum = unchecked(checksum * 31 + character);
        return checksum;
    }
}

internal readonly record struct FirstClassEnumBenchmarkOutcome(int RowCount, long Checksum)
{
    public long Consumer => unchecked(Checksum + RowCount);
}

internal sealed class FirstClassEnumBenchmarkSchemaProvider(
    FirstClassEnumBenchmarkInput[] rows) : ISchemaProvider
{
    public static FirstClassEnumBenchmarkSchemaProvider Create(int rowsCount)
    {
        var rows = new FirstClassEnumBenchmarkInput[rowsCount];
        for (var index = 0; index < rowsCount; index++)
        {
            rows[index] = new FirstClassEnumBenchmarkInput(
                index,
                (short)index,
                (uint)(index & 7));
        }

        return new FirstClassEnumBenchmarkSchemaProvider(rows);
    }

    public ISchema GetSchema(string schema)
    {
        var normalized = schema.TrimStart('#');
        if (string.Equals(normalized, "enumperf", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(normalized, "carrierperf", StringComparison.OrdinalIgnoreCase))
        {
            return new FirstClassEnumBenchmarkSchema(rows);
        }

        throw new NotSupportedException(schema);
    }
}

internal sealed class FirstClassEnumBenchmarkSchema(FirstClassEnumBenchmarkInput[] rows)
    : SchemaBase("enum-performance", CreateLibrary()), IQueryScopedRowSourceSchema
{
    public override ISchemaTable GetTableByName(
        string name,
        SourceMetadataContext metadataContext,
        params object?[] parameters)
    {
        return string.Equals(name, "rows", StringComparison.OrdinalIgnoreCase)
            ? new FirstClassEnumBenchmarkTable(
                metadataContext.AllColumns.Count == 0
                    ?
                    [
                        new SchemaColumn(nameof(FirstClassEnumBenchmarkInput.Id), 0, typeof(int)),
                        new SchemaColumn(nameof(FirstClassEnumBenchmarkInput.Status), 1, typeof(short)),
                        new SchemaColumn(nameof(FirstClassEnumBenchmarkInput.Access), 2, typeof(uint))
                    ]
                    : metadataContext.AllColumns.ToArray())
            : throw new NotSupportedException(name);
    }

    public override SourceDescriptor DescribeSource(
        string name,
        SourceDescribeContext context,
        params object?[] parameters)
    {
        return base.DescribeSource(name, context, parameters) with
        {
            Columns = context.MetadataContext.AllColumns.Count == 0
                ? base.DescribeSource(name, context, parameters).Columns
                : context.MetadataContext.AllColumns.ToArray(),
            TransferCapabilities = SourceTransferCapabilities.QueryScopedRows |
                                   SourceTransferCapabilities.LogicalScalarReads
        };
    }

    public override RowSource<TRow> GetRowSource<TRow>(
        string name,
        SourceExecutionContext executionContext,
        params object?[] parameters)
    {
        return string.Equals(name, "rows", StringComparison.OrdinalIgnoreCase)
            ? EnsureSourceType<TRow, FirstClassEnumBenchmarkInput>(
                name,
                new FirstClassEnumBenchmarkLegacyRowSource(rows))
            : throw new NotSupportedException(name);
    }

    public RowSource<TRow> GetQueryScopedRowSource<TRow, TMaterializer>(
        string name,
        QueryScopedRowSourceRequest request,
        params object?[] parameters)
        where TMaterializer : struct, IQueryRowMaterializer<TRow>
    {
        return string.Equals(name, "rows", StringComparison.OrdinalIgnoreCase)
            ? new FirstClassEnumBenchmarkQueryRowSource<TRow, TMaterializer>(rows, request.Shape.Fields)
            : throw new NotSupportedException(name);
    }

    private static MethodsAggregator CreateLibrary()
    {
        var methods = new MethodsManager();
        methods.RegisterLibraries(new LibraryBase());
        return new MethodsAggregator(methods);
    }
}

internal sealed class FirstClassEnumBenchmarkTable(ISchemaColumn[] columns) : ISchemaTable
{
    public ISchemaColumn[] Columns { get; } = columns;

    public SchemaTableMetadata Metadata { get; } = new(typeof(FirstClassEnumBenchmarkInput));

    public ISchemaColumn? GetColumnByName(string name) =>
        Columns.SingleOrDefault(column =>
            string.Equals(column.ColumnName, name, StringComparison.OrdinalIgnoreCase));

    public ISchemaColumn[] GetColumnsByName(string name) =>
        Columns.Where(column =>
            string.Equals(column.ColumnName, name, StringComparison.OrdinalIgnoreCase)).ToArray();
}

internal sealed class FirstClassEnumBenchmarkLegacyRowSource(FirstClassEnumBenchmarkInput[] rows)
    : RowSourceBase<FirstClassEnumBenchmarkInput>
{
    protected override void CollectChunks(IChunkWriter<FirstClassEnumBenchmarkInput> writer)
    {
        writer.Write(rows);
    }
}

internal sealed class FirstClassEnumBenchmarkQueryRowSource<TRow, TMaterializer>(
    FirstClassEnumBenchmarkInput[] rows,
    IReadOnlyList<QueryRowField> fields) : RowSourceBase<TRow>
    where TMaterializer : struct, IQueryRowMaterializer<TRow>
{
    protected override void CollectChunks(IChunkWriter<TRow> writer)
    {
        var materialized = new TRow[rows.Length];
        for (var index = 0; index < rows.Length; index++)
        {
            var reader = new FirstClassEnumBenchmarkReader(rows[index], fields);
            materialized[index] = TMaterializer.Materialize<FirstClassEnumBenchmarkReader>(ref reader);
        }

        writer.Write(materialized);
    }
}

internal ref struct FirstClassEnumBenchmarkReader(
    FirstClassEnumBenchmarkInput row,
    IReadOnlyList<QueryRowField> fields) : IQuerySourceFieldReader
{
    public T Read<T>(int slot)
    {
        return fields[slot].Name switch
        {
            nameof(FirstClassEnumBenchmarkInput.Id) => Reinterpret<int, T>(row.Id),
            nameof(FirstClassEnumBenchmarkInput.Status) => Reinterpret<short, T>(row.Status),
            nameof(FirstClassEnumBenchmarkInput.Access) => Reinterpret<uint, T>(row.Access),
            _ => throw new ArgumentOutOfRangeException(nameof(slot))
        };
    }

    private static TTo Reinterpret<TFrom, TTo>(TFrom value)
        where TFrom : struct
    {
        if (typeof(TTo) == typeof(TFrom))
            return Unsafe.As<TFrom, TTo>(ref value);

        if (typeof(TTo) == typeof(TFrom?))
        {
            TFrom? nullable = value;
            return Unsafe.As<TFrom?, TTo>(ref nullable);
        }

        throw new InvalidOperationException(
            $"Unexpected benchmark source read '{typeof(TTo)}' for '{typeof(TFrom)}'.");
    }
}

public sealed record FirstClassEnumBenchmarkInput(int Id, short Status, uint Access);
