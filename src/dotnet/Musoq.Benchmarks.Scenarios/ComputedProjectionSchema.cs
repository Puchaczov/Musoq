using System.Runtime.CompilerServices;
using Musoq.Plugins;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;
using Musoq.Schema.Optimization;

namespace Musoq.Benchmarks.Scenarios;

public sealed class ComputedProjectionSchema(int[][] chunks) : SchemaBase("computed", Library()), IQueryScopedRowSourceSchema
{
    public SourcePlanRequest? LastRequest { get; private set; }
    public SourcePlanResult? LastResult { get; private set; }
    public QueryRowShape? LastShape { get; private set; }

    public override ISchemaTable GetTableByName(string name, SourceMetadataContext context, params object?[] parameters)
        => name is "accepted" or "residual" ? new ComputedTable() : throw new InvalidOperationException($"Unknown computed source '{name}'.");

    public override SourceDescriptor DescribeSource(string name, SourceDescribeContext context, params object?[] parameters)
        => base.DescribeSource(name, context, parameters) with
        {
            TransferCapabilities = SourceTransferCapabilities.QueryScopedRows,
            ComputedProjectionCapabilities = name == "accepted" ?
                SourceComputedProjectionCapabilities.Columns | SourceComputedProjectionCapabilities.Literals |
                SourceComputedProjectionCapabilities.Cast | SourceComputedProjectionCapabilities.Binary : SourceComputedProjectionCapabilities.None
        };

    public override SourcePlanResult TryPlanSource(string name, SourcePlanRequest request, params object?[] parameters)
    {
        LastRequest = request;
        var rejected = SourcePlanResult.RejectAll(request);
        var accepted = name == "accepted" ? request.RequestedComputedProjections.Where(IsIdTimesTwo).ToArray() : [];
        var names = accepted.Select(item => item.Name).ToHashSet(StringComparer.Ordinal);
        LastResult = rejected with
        {
            AcceptedComputedProjections = accepted,
            ResidualComputedProjections = request.RequestedComputedProjections.Where(item => !names.Contains(item.Name)).ToArray(),
            ExecutionPlan = rejected.ExecutionPlan with { AcceptedComputedProjections = accepted }
        };
        return LastResult;
    }

    public override RowSource<T> GetRowSource<T, TArguments>(string name, SourceExecutionContext context, TArguments arguments)
        => throw new InvalidOperationException("The computed fixture requires typed query-scoped transfer.");

    public RowSource<TRow> GetQueryScopedRowSource<TRow, TMaterializer, TArguments>(string name,
        QueryScopedRowSourceRequest request, TArguments arguments)
        where TMaterializer : struct, IQueryRowMaterializer<TRow>
        where TArguments : struct, ISourceArguments
    {
        LastShape = request.Shape;
        foreach (var field in request.Shape.Fields)
            if (field.ComputedProjection is { } projection && (!IsIdTimesTwo(projection) ||
                !request.ExecutionContext.Plan.AcceptedComputedProjections.Any(item => item.Name == projection.Name)))
                throw new InvalidOperationException("An unnegotiated computed field was requested.");
        return new ComputedRows<TRow, TMaterializer>(chunks, request);
    }

    private static bool IsIdTimesTwo(SourceComputedProjection projection) => projection.Expression is
        SourceScalarBinary { Operator: SourceScalarBinaryOperator.Multiply,
            Left: SourceScalarCast { Operand: SourceScalarColumn { Column.Name: "Id" } },
            Right: SourceScalarLiteral { Value: 2 or 2L } };

    private static MethodsAggregator Library()
    {
        var manager = new MethodsManager();
        manager.RegisterLibraries(new LibraryBase());
        return new(manager);
    }

    private sealed class ComputedTable : ISchemaTable
    {
        public ISchemaColumn[] Columns { get; } = [new SchemaColumn("Id", 0, typeof(int))];
        public SchemaTableMetadata Metadata { get; } = new(typeof(QueryScopedRowContract)) { TypeOrigin = TableSchemaTypeOrigin.TableDefined };
        public ISchemaColumn? GetColumnByName(string name) => name == "Id" ? Columns[0] : null;
        public ISchemaColumn[] GetColumnsByName(string name) => name == "Id" ? Columns : [];
    }

    private sealed class ComputedRows<TRow, TMaterializer>(int[][] input, QueryScopedRowSourceRequest request) : RowSource<TRow>
        where TMaterializer : struct, IQueryRowMaterializer<TRow>
    {
        public override IEnumerable<IReadOnlyList<TRow>> Chunks
        {
            get
            {
                foreach (var chunk in input)
                {
                    request.ExecutionContext.EndWorkToken.ThrowIfCancellationRequested();
                    var output = new TRow[chunk.Length];
                    for (var index = 0; index < chunk.Length; index++)
                    {
                        var reader = new ComputedReader(chunk[index], request.Shape);
                        output[index] = TMaterializer.Materialize(ref reader);
                    }
                    yield return output;
                }
            }
        }
    }

    private readonly struct ComputedReader(int id, QueryRowShape shape) : IQuerySourceFieldReader
    {
        public T Read<T>(int slot)
        {
            var field = shape.Fields[slot];
            if (field.Origin == QueryRowFieldOrigin.SourceColumn && field.SourceColumnIndex == 0 && typeof(T) == typeof(int))
            {
                var value = id;
                return Unsafe.As<int, T>(ref value);
            }
            if (field.ComputedProjection is not null && typeof(T) == typeof(long?))
            {
                long? value = (long)id * 2;
                return Unsafe.As<long?, T>(ref value);
            }
            if (field.ComputedProjection is not null && typeof(T) == typeof(long))
            {
                var value = (long)id * 2;
                return Unsafe.As<long, T>(ref value);
            }
            throw new InvalidOperationException($"Missing accepted typed field '{field.Name}'.");
        }
    }
}
