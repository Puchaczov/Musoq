using System;
using System.Collections.Generic;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;

namespace Musoq.Evaluator.Tests.Schema.Dynamic;

public class DynamicSchema : SchemaBase
{
    private const string SchemaName = "Dynamic";
    private readonly IReadOnlyDictionary<string, Type> _tableSchema;
    private readonly IEnumerable<dynamic> _values;

    public DynamicSchema(IReadOnlyDictionary<string, Type> tableSchema, IEnumerable<dynamic> values) 
        : base(SchemaName, CreateLibrary())
    {
        _tableSchema = tableSchema;
        _values = values;
    }
    
    public override ISchemaTable GetTableByName(string name, RuntimeContext runtimeContext, params object[] parameters)
    {
        return new DynamicTable(_tableSchema);
    }
    
    public override RowSource GetRowSource(string name, RuntimeContext runtimeContext, params object[] parameters)
    {
        return new DynamicSource(_values);
    }

    private static MethodsAggregator CreateLibrary()
    {
        var methodManager = new MethodsManager();

        var lib = new DynamicLibrary();

        methodManager.RegisterLibraries(lib);

        return new MethodsAggregator(methodManager);
    }
}
