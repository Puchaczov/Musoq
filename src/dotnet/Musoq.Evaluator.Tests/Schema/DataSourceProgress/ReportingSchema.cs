using System.Collections.Generic;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;

namespace Musoq.Evaluator.Tests.Schema.DataSourceProgress;

public class ReportingSchema<T> : SchemaBase where T : BasicEntity
{
    private readonly string _schemaName;
    private readonly IEnumerable<T> _sources;

    public ReportingSchema(string schemaName, IEnumerable<T> sources)
        : base(schemaName, CreateLibrary())
    {
        _schemaName = schemaName;
        _sources = sources;
        AddTable<BasicEntityTable>("entities");
    }

    public override RowSource GetRowSource(string name, RuntimeContext runtimeContext, params object[] parameters)
    {
        return new ReportingEntitySource<BasicEntity>(
            _sources,
            BasicEntity.TestNameToIndexMap,
            BasicEntity.TestIndexToObjectAccessMap,
            runtimeContext,
            $"{_schemaName}.{name}");
    }

    private static MethodsAggregator CreateLibrary()
    {
        var methodManager = new MethodsManager();
        var lib = new Library();
        methodManager.RegisterLibraries(lib);
        return new MethodsAggregator(methodManager);
    }
}
