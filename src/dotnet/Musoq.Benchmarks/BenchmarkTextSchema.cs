using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;

namespace Musoq.Benchmarks;

/// <summary>
///     Text schema for benchmarks.
/// </summary>
public class BenchmarkTextSchema : SchemaBase
{
    private readonly IEnumerable<BenchmarkTextEntity> _entities;

    public BenchmarkTextSchema(IEnumerable<BenchmarkTextEntity> entities)
        : base("test", CreateLibrary())
    {
        _entities = entities;
    }

    public override ISchemaTable GetTableByName(string name, RuntimeContext runtimeContext, params object[] parameters)
    {
        return new BenchmarkTextEntityTable();
    }

    public override RowSource GetRowSource(string name, RuntimeContext runtimeContext, params object[] parameters)
    {
        return new BenchmarkEntitySource<BenchmarkTextEntity>(
            _entities,
            BenchmarkTextEntity.NameToIndexMap,
            BenchmarkTextEntity.IndexToObjectAccessMap);
    }

    private static MethodsAggregator CreateLibrary()
    {
        var methodManager = new MethodsManager();
        return new MethodsAggregator(methodManager);
    }
}
