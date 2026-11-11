using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Musoq.Plugins;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;
using Musoq.Schema.Optimization;

namespace Musoq.Evaluator.Tests.Schema.Computed;

public enum ComputedSourceBehavior
{
    Accept, Reject, Partial, NoCapability, DeclaredRows, VolatileColumn,
    Unknown, Duplicate, Modified, Unstable, WrongType, MissingResidual, MissingExecution, ExtraResidual, MissingValue
}

public sealed record ComputedSourceRow(int Id, int? Optional);

public sealed class ComputedSourceProvider(ComputedSourceBehavior behavior = ComputedSourceBehavior.Accept) : ISchemaProvider
{
    public ComputedSourceSchema Source { get; } = new(behavior);
    public ISchema GetSchema(string schema) => Source;
}

public sealed class ComputedSourceSchema(ComputedSourceBehavior behavior) : SchemaBase("computedtest", Library()), IQueryScopedRowSourceSchema
{
    private static readonly ComputedSourceRow[] Input = [new(-2, null), new(0, 4), new(3, -1)];
    public SourcePlanRequest? Request { get; private set; }
    public QueryRowShape? Shape { get; private set; }
    public int ComputedReads { get; private set; }

    public override ISchemaTable GetTableByName(string name, SourceMetadataContext context, params object?[] parameters) => new Table(behavior);
    public override SourceDescriptor DescribeSource(string name, SourceDescribeContext context, params object?[] parameters)
        => base.DescribeSource(name, context, parameters) with
        {
            ComputedProjectionCapabilities = behavior == ComputedSourceBehavior.NoCapability
                ? SourceComputedProjectionCapabilities.None : SourceComputedProjectionCapabilities.AllPortable,
            TransferCapabilities = behavior == ComputedSourceBehavior.DeclaredRows
                ? SourceTransferCapabilities.None : SourceTransferCapabilities.QueryScopedRows
        };

    public override SourcePlanResult TryPlanSource(string name, SourcePlanRequest request, params object?[] parameters)
    {
        Request = request;
        if (behavior is ComputedSourceBehavior.Reject or ComputedSourceBehavior.NoCapability or ComputedSourceBehavior.DeclaredRows or ComputedSourceBehavior.VolatileColumn)
            return SourcePlanResult.RejectAll(request);
        var offered = request.RequestedComputedProjections.ToArray();
        if (offered.Length == 0)
            return SourcePlanResult.RejectAll(request);
        var accepted = behavior == ComputedSourceBehavior.Partial ? offered.Take(1).ToArray() : offered;
        var residual = offered.Except(accepted).ToArray();
        accepted = behavior switch
        {
            ComputedSourceBehavior.Unknown => [accepted[0] with { Name = "not-offered" }],
            ComputedSourceBehavior.Duplicate => [accepted[0], accepted[0]],
            ComputedSourceBehavior.Modified => [accepted[0] with { Expression = new SourceScalarLiteral(42L, accepted[0].ResultType) }],
            ComputedSourceBehavior.Unstable => [accepted[0] with { Stability = ColumnStability.Volatile }],
            ComputedSourceBehavior.WrongType => [accepted[0] with { ResultType = typeof(string) }],
            ComputedSourceBehavior.MissingResidual => [],
            _ => accepted
        };
        return SourcePlanResult.RejectAll(request) with
        {
            AcceptedComputedProjections = accepted,
            ResidualComputedProjections = behavior == ComputedSourceBehavior.ExtraResidual ? offered : residual,
            ExecutionPlan = SourceExecutionPlan.Empty(request.Identity) with
            {
                AcceptedComputedProjections = behavior == ComputedSourceBehavior.MissingExecution ? [] : accepted
            }
        };
    }

    public override RowSource<T> GetRowSource<T, TArguments>(string name, SourceExecutionContext context, TArguments arguments)
        => EnsureSourceType<T, ComputedSourceRow>(name, new DeclaredRows());

    public RowSource<TRow> GetQueryScopedRowSource<TRow, TMaterializer, TArguments>(string name,
        QueryScopedRowSourceRequest request, TArguments parameters)
        where TMaterializer : struct, IQueryRowMaterializer<TRow>
        where TArguments : struct, ISourceArguments
    {
        Shape = request.Shape;
        return new Rows<TRow, TMaterializer>(this, request);
    }

    private sealed class Rows<TRow, TMaterializer>(ComputedSourceSchema owner, QueryScopedRowSourceRequest request) : RowSource<TRow>
        where TMaterializer : struct, IQueryRowMaterializer<TRow>
    {
        public override IEnumerable<IReadOnlyList<TRow>> Chunks
        {
            get
            {
                var output = new TRow[Input.Length];
                for (var index = 0; index < Input.Length; index++)
                {
                    var reader = new Reader(owner, Input[index], request.Shape);
                    output[index] = TMaterializer.Materialize(ref reader);
                }
                yield return output;
            }
        }
    }

    private readonly struct Reader(ComputedSourceSchema owner, ComputedSourceRow row, QueryRowShape shape) : IQuerySourceFieldReader
    {
        public T Read<T>(int slot)
        {
            var field = shape.Fields[slot];
            if (field.ComputedProjection is { } computed)
            {
                if (owner.Behavior == ComputedSourceBehavior.MissingValue)
                    throw new InvalidOperationException("Missing accepted computed value.");
                owner.ComputedReads++;
                if (typeof(T) == typeof(long?))
                {
                    var result = Number(computed.Expression, row);
                    return Unsafe.As<long?, T>(ref result);
                }
                if (typeof(T) == typeof(decimal?))
                {
                    decimal? result = Number(computed.Expression, row);
                    return Unsafe.As<decimal?, T>(ref result);
                }
                if (typeof(T) == typeof(bool?))
                {
                    var result = Truth(computed.Expression, row);
                    return Unsafe.As<bool?, T>(ref result);
                }
                if (typeof(T) == typeof(bool))
                {
                    var result = Truth(computed.Expression, row) ?? throw new InvalidOperationException("Null nonnullable computed boolean.");
                    return Unsafe.As<bool, T>(ref result);
                }
            }
            if (field.Name == "Id" && typeof(T) == typeof(int))
            {
                var result = row.Id;
                return Unsafe.As<int, T>(ref result);
            }
            if (field.Name == "Optional" && typeof(T) == typeof(int?))
            {
                var result = row.Optional;
                return Unsafe.As<int?, T>(ref result);
            }
            throw new InvalidOperationException("Unsupported typed test field: " + field.Name);
        }
    }

    private ComputedSourceBehavior Behavior => behavior;

    private static long? Number(SourceScalarExpression expression, ComputedSourceRow row) => expression switch
    {
        SourceScalarColumn { Column.Name: "Id" } => row.Id,
        SourceScalarColumn { Column.Name: "Optional" } => row.Optional,
        SourceScalarLiteral { Value: sbyte value } => value,
        SourceScalarLiteral { Value: byte value } => value,
        SourceScalarLiteral { Value: short value } => value,
        SourceScalarLiteral { Value: ushort value } => value,
        SourceScalarLiteral { Value: int value } => value,
        SourceScalarLiteral { Value: uint value } => value,
        SourceScalarLiteral { Value: long value } => value,
        SourceScalarLiteral { Value: decimal value } => checked((long)value),
        SourceScalarLiteral { Value: null } => null,
        SourceScalarCast cast => Number(cast.Operand, row),
        SourceScalarUnary { Operator: SourceScalarUnaryOperator.Negate } unary => -Number(unary.Operand, row),
        SourceScalarBinary { Operator: SourceScalarBinaryOperator.Multiply } binary => Number(binary.Left, row) * Number(binary.Right, row),
        SourceScalarBinary { Operator: SourceScalarBinaryOperator.Add } binary => Number(binary.Left, row) + Number(binary.Right, row),
        SourceScalarBinary { Operator: SourceScalarBinaryOperator.Subtract } binary => Number(binary.Left, row) - Number(binary.Right, row),
        SourceScalarCoalesce coalesce => coalesce.Expressions.Select(item => Number(item, row)).FirstOrDefault(item => item.HasValue),
        _ => throw new InvalidOperationException("Unsupported numeric test expression: " + expression)
    };

    private static bool? Truth(SourceScalarExpression expression, ComputedSourceRow row) => expression switch
    {
        SourceScalarLiteral { Value: bool value } => value,
        SourceScalarNullCheck check => Number(check.Operand, row).HasValue == check.Negated,
        SourceScalarUnary { Operator: SourceScalarUnaryOperator.Not } unary => !Truth(unary.Operand, row),
        SourceScalarBinary { Operator: SourceScalarBinaryOperator.Or } binary => Truth(binary.Left, row) | Truth(binary.Right, row),
        SourceScalarBinary { Operator: SourceScalarBinaryOperator.And } binary => Truth(binary.Left, row) & Truth(binary.Right, row),
        SourceScalarBinary binary => Compare(binary, row),
        _ => throw new InvalidOperationException("Unsupported boolean test expression.")
    };

    private static bool? Compare(SourceScalarBinary binary, ComputedSourceRow row)
    {
        var left = Number(binary.Left, row);
        var right = Number(binary.Right, row);
        if (binary.UsesSqlNullSemantics && (!left.HasValue || !right.HasValue))
            return null;
        return binary.Operator switch
        {
            SourceScalarBinaryOperator.Equal => left == right,
            SourceScalarBinaryOperator.NotEqual => left != right,
            SourceScalarBinaryOperator.GreaterThan => left > right,
            SourceScalarBinaryOperator.LessThan => left < right,
            SourceScalarBinaryOperator.GreaterThanOrEqual => left >= right,
            SourceScalarBinaryOperator.LessThanOrEqual => left <= right,
            _ => throw new InvalidOperationException("Unsupported comparison.")
        };
    }

    private sealed class Table(ComputedSourceBehavior mode) : ISchemaTable
    {
        public ISchemaColumn[] Columns { get; } =
        [new SchemaColumn("Id", 0, typeof(int), mode == ComputedSourceBehavior.VolatileColumn ? ColumnStability.Volatile : ColumnStability.Stable),
            new SchemaColumn("Optional", 1, typeof(int?))];
        public SchemaTableMetadata Metadata { get; } = new(mode is ComputedSourceBehavior.DeclaredRows or ComputedSourceBehavior.VolatileColumn ? typeof(ComputedSourceRow) : typeof(QueryScopedRowContract))
            { TypeOrigin = TableSchemaTypeOrigin.TableDefined };
        public ISchemaColumn? GetColumnByName(string name) => Columns.SingleOrDefault(item => item.ColumnName == name);
        public ISchemaColumn[] GetColumnsByName(string name) => Columns.Where(item => item.ColumnName == name).ToArray();
    }

    private sealed class DeclaredRows : RowSourceBase<ComputedSourceRow>
    {
        protected override void CollectChunks(IChunkWriter<ComputedSourceRow> writer) => writer.Write(Input);
    }

    private static MethodsAggregator Library()
    {
        var manager = new MethodsManager();
        manager.RegisterLibraries(new LibraryBase());
        return new(manager);
    }
}
