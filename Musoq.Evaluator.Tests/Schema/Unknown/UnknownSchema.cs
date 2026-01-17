using System.Collections.Generic;
using Musoq.Evaluator.Tests.Schema.Dynamic;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;

namespace Musoq.Evaluator.Tests.Schema.Unknown;

public class UnknownSchema : SchemaBase
{
    private const string SchemaName = "Unknown";
    private readonly IEnumerable<dynamic> _values;

    public UnknownSchema(IEnumerable<dynamic> values)
        : base(SchemaName, CreateLibrary())
    {
        _values = values;
    }

    public UnknownSchema()
        : base(SchemaName, CreateLibrary())
    {
        _values = [];
    }

    public override ISchemaTable GetTableByName(string name, RuntimeContext runtimeContext, params object[] parameters)
    {
        return new UnknownTable(runtimeContext);
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