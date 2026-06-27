using System;
using System.Collections.Generic;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;

namespace Musoq.Evaluator.Tests.Schema.PathValue;

public class PathValueSchema(IEnumerable<PathValueEntity> entities) : SchemaBase(SchemaName, CachedLibrary.Value)
{
    private const string SchemaName = "pathvalue";
    private static readonly Lazy<MethodsAggregator> CachedLibrary = new(CreateLibrary);

    public override ISchemaTable GetTableByName(string name, SourceMetadataContext metadataContext, params object?[] parameters)
    {
        return new PathValueSchemaTable();
    }

    public override RowSource<T> GetRowSource<T>(string name, SourceExecutionContext executionContext, params object?[] parameters)
    {
        return EnsureSourceType<T, PathValueEntity>(name, new PathValueRowSource(entities));
    }

    private static MethodsAggregator CreateLibrary()
    {
        var methodsManager = new MethodsManager();
        var library = new PathValueLibrary();

        methodsManager.RegisterLibraries(library);

        return new MethodsAggregator(methodsManager);
    }
}
