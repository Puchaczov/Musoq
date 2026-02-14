using System;
using System.Collections.Generic;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;

namespace Musoq.Evaluator.Tests.Schema.Multi;

public class MultiSchema(IReadOnlyDictionary<string, (ISchemaTable SchemaTable, RowSource RowSource)> tables)
    : SchemaBase("test", CachedLibrary.Value)
{
    private static readonly Lazy<MethodsAggregator> CachedLibrary = new(CreateLibrary);
    
    public override ISchemaTable GetTableByName(string name, RuntimeContext runtimeContext, params object[] parameters)
    {
        return name switch
        {
            "first" => tables[name].SchemaTable,
            "second" => tables[name].SchemaTable,
            _ => throw new NotSupportedException($"Table {name} is not supported.")
        };
    }

    public override RowSource GetRowSource(string name, RuntimeContext runtimeContext, params object[] parameters)
    {
        return name switch
        {
            "first" => tables[name].RowSource,
            "second" => tables[name].RowSource,
            _ => throw new NotSupportedException($"Table {name} is not supported.")
        };
    }

    private static MethodsAggregator CreateLibrary()
    {
        var methodManager = new MethodsManager();

        var lib = new MultiLibrary();

        methodManager.RegisterLibraries(lib);

        return new MethodsAggregator(methodManager);
    }
}
