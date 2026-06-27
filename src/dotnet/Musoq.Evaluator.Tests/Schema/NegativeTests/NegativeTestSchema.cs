using System;
using System.Collections.Generic;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Exceptions;
using Musoq.Schema.Managers;

namespace Musoq.Evaluator.Tests.Schema.NegativeTests;

public class NegativeTestSchema(IReadOnlyDictionary<string, (ISchemaTable Table, object Source)> tables)
    : SchemaBase("test", CachedLibrary.Value)
{
    private static readonly Lazy<MethodsAggregator> CachedLibrary = new(CreateLibrary);

    public override ISchemaTable GetTableByName(string name, SourceMetadataContext metadataContext, params object?[] parameters)
    {
        if (tables.TryGetValue(name, out var entry))
            return entry.Table;

        throw new TableNotFoundException(name);
    }

    public override RowSource<T> GetRowSource<T>(string name, SourceExecutionContext executionContext, params object?[] parameters)
    {
        if (tables.TryGetValue(name, out var entry))
            return EnsureSourceType<T>(name, entry.Source);

        throw new SourceNotFoundException(name);
    }

    private static MethodsAggregator CreateLibrary()
    {
        var methodManager = new MethodsManager();
        var lib = new NegativeTestLibrary();
        methodManager.RegisterLibraries(lib);
        return new MethodsAggregator(methodManager);
    }
}
