using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Musoq.Plugins;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;
using Musoq.Schema.Optimization;

namespace Musoq.Evaluator.Tests.Schema.LibraryExecution;

public sealed class LibraryExecutionQueryRowProvider : ISchemaProvider
{
    public ISchema GetSchema(string schema) => new LibraryExecutionQueryRowSchema();
}

public sealed class LibraryExecutionQueryRowSchema : SchemaBase, IQueryScopedRowSourceSchema
{
    public LibraryExecutionQueryRowSchema() : base("typedrows", CreateMethods()) { }

    public override ISchemaTable GetTableByName(string name, SourceMetadataContext context, params object?[] parameters)
        => new Musoq.Evaluator.Tests.Schema.QueryRows.GeneratedQueryRowSampleTable(
            [new SchemaColumn("Id", 0, typeof(int)), new SchemaColumn("Amount", 1, typeof(decimal?))]);

    public override SourceDescriptor DescribeSource(string name, SourceDescribeContext context, params object?[] parameters)
        => base.DescribeSource(name, context, parameters) with
        {
            TransferCapabilities = SourceTransferCapabilities.QueryScopedRows | SourceTransferCapabilities.LogicalScalarReads
        };

    public override RowSource<T> GetRowSource<T, TArguments>(string name, SourceExecutionContext context, TArguments parameters)
        => throw new InvalidOperationException("Typed provider must not use legacy row transfer.");

    public RowSource<TRow> GetQueryScopedRowSource<TRow, TMaterializer, TArguments>(string name,
        QueryScopedRowSourceRequest request, TArguments parameters)
        where TMaterializer : struct, IQueryRowMaterializer<TRow>
        where TArguments : struct, global::Musoq.Schema.ISourceArguments
        => new Source<TRow, TMaterializer>(request.Shape);

    private static MethodsAggregator CreateMethods()
    {
        var manager = new MethodsManager();
        manager.RegisterLibraries(new LibraryBase());
        return new MethodsAggregator(manager);
    }

    private sealed class Source<TRow, TMaterializer>(QueryRowShape shape) : RowSourceBase<TRow>
        where TMaterializer : struct, IQueryRowMaterializer<TRow>
    {
        protected override void CollectChunks(IChunkWriter<TRow> writer)
        {
            var rows = new List<TRow>(3);
            for (var id = 1; id <= 3; id++)
            {
                var reader = new Reader(shape, id, id == 3 ? null : id * 1.25m);
                rows.Add(TMaterializer.Materialize(ref reader));
            }
            writer.Write(rows);
        }
    }

    private struct Reader(QueryRowShape shape, int id, decimal? amount) : IQuerySourceFieldReader
    {
        public T Read<T>(int slot)
        {
            if (shape.Fields[slot].SourceColumnIndex == 0 && typeof(T) == typeof(int))
                return Unsafe.As<int, T>(ref id);
            if (shape.Fields[slot].SourceColumnIndex == 1 && typeof(T) == typeof(decimal?))
                return Unsafe.As<decimal?, T>(ref amount);
            throw new InvalidOperationException("The generated read must preserve the provider's exact field type.");
        }
    }
}
