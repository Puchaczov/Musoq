using System;
using System.Collections.Generic;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;

namespace Musoq.Evaluator.Tests.Schema.Multi;

public class MultiSchema : SchemaBase
{
    private readonly IReadOnlyDictionary<string, (ISchemaTable SchemaTable, RowSource RowSource)> _tables;
    
    public MultiSchema(IReadOnlyDictionary<string, (ISchemaTable SchemaTable, RowSource RowSource)> tables) 
        : base("test", CreateLibrary())
    {
        _tables = tables;
    }

    public override ISchemaTable GetTableByName(string name, RuntimeContext runtimeContext, params object[] parameters)
    {
        return name switch
        {
            "first" => _tables[name].SchemaTable,
            "second" => _tables[name].SchemaTable,
            _ => throw new NotSupportedException($"Table {name} is not supported.")
        };
    }

    public override RowSource GetRowSource(string name, RuntimeContext runtimeContext, params object[] parameters)
    {
        return name switch
        {
            "first" => _tables[name].RowSource,
            "second" => _tables[name].RowSource,
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