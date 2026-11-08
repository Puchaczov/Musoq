using System;
using System.Collections.Generic;
using Musoq.Evaluator.Tests.Schema.Dynamic;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;

namespace Musoq.Evaluator.Tests.Schema.Unknown;

public class UnknownSchema : SchemaBase, IQueryScopedRowSourceSchema
{
    private const string SchemaName = "Unknown";
    private static readonly Lazy<MethodsAggregator> CachedLibrary = new(CreateLibrary);
    private readonly IEnumerable<dynamic> _values;

    public UnknownSchema(IEnumerable<dynamic> values)
        : base(SchemaName, CachedLibrary.Value)
    {
        _values = values;
    }

    public UnknownSchema()
        : base(SchemaName, CachedLibrary.Value)
    {
        _values = [];
    }

    public override ISchemaTable GetTableByName(string name, SourceMetadataContext metadataContext, params object?[] parameters)
    {
        return new UnknownTable(metadataContext);
    }

    public override RowSource<T> GetRowSource<T, TArguments>(string name, SourceExecutionContext executionContext, TArguments nativeArguments)
    {
        return EnsureSourceType<T, IReadOnlyDictionary<string, object?>>(name, new DynamicSource(_values));
    }

    public override SourceDescriptor DescribeSource(string name, SourceDescribeContext context, params object?[] parameters)
        => base.DescribeSource(name, context, parameters) with { TransferCapabilities = SourceTransferCapabilities.QueryScopedRows };

    public RowSource<TRow> GetQueryScopedRowSource<TRow, TMaterializer, TArguments>(
        string name, QueryScopedRowSourceRequest request, TArguments parameters)
        where TMaterializer : struct, IQueryRowMaterializer<TRow>
        where TArguments : struct, ISourceArguments
        => new NativeDynamicTestSource<TRow, TMaterializer>(_values, request.Shape.Fields);

    private static MethodsAggregator CreateLibrary()
    {
        var methodManager = new MethodsManager();

        var lib = new DynamicLibrary();

        methodManager.RegisterLibraries(lib);

        return new MethodsAggregator(methodManager);
    }
}
