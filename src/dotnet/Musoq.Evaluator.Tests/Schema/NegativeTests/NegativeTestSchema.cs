using System;
using System.Collections.Generic;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Exceptions;
using Musoq.Schema.Managers;

namespace Musoq.Evaluator.Tests.Schema.NegativeTests;

public class NegativeTestSchema : SchemaBase
{
    private static readonly Lazy<MethodsAggregator> CachedLibrary = new(CreateLibrary);
    private readonly IReadOnlyDictionary<string, (ISchemaTable Table, RowSource Source)> _tables;

    public NegativeTestSchema(IReadOnlyDictionary<string, (ISchemaTable Table, RowSource Source)> tables)
        : base("test", CachedLibrary.Value)
    {
        _tables = tables;
    }

    public override ISchemaTable GetTableByName(string name, RuntimeContext runtimeContext, params object[] parameters)
    {
        if (_tables.TryGetValue(name, out var entry))
            return entry.Table;

        throw new TableNotFoundException(name);
    }

    public override RowSource GetRowSource(string name, RuntimeContext runtimeContext, params object[] parameters)
    {
        if (_tables.TryGetValue(name, out var entry))
            return entry.Source;

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
