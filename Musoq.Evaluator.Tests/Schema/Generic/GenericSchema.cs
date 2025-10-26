using System;
using System.Collections.Generic;
using Musoq.Plugins;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;

namespace Musoq.Evaluator.Tests.Schema.Generic;

public class GenericSchema<TLibrary>(IReadOnlyDictionary<string, (ISchemaTable SchemaTable, RowSource RowSource)> tables, Dictionary<string, Func<object[], RowSource, RowSource>> filterRowsSource = null)
    : SchemaBase("test", CreateLibrary()) where TLibrary : LibraryBase, new()
{
    public override ISchemaTable GetTableByName(string name, RuntimeContext runtimeContext, params object[] parameters)
    {
        if (tables.TryGetValue(name, out var table))
            return table.SchemaTable;
        
        throw new NotSupportedException($"Table {name} is not supported.");
    }

    public override RowSource GetRowSource(string name, RuntimeContext runtimeContext, params object[] parameters)
    {
        if (!tables.TryGetValue(name, out var table))
            throw new NotSupportedException($"Table {name} is not supported.");
        
        if (filterRowsSource.TryGetValue(name, out var filter))
            return filter?.Invoke(parameters, table.RowSource) ?? table.RowSource;
        
        return table.RowSource;
    }

    private static MethodsAggregator CreateLibrary()
    {
        var methodManager = new MethodsManager();

        var lib = new TLibrary();

        methodManager.RegisterLibraries(lib);

        return new MethodsAggregator(methodManager);
    }
}
