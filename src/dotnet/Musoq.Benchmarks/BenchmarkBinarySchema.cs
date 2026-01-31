using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;

namespace Musoq.Benchmarks;

/// <summary>
///     Binary schema for benchmarks.
/// </summary>
public class BenchmarkBinarySchema : SchemaBase
{
    private readonly IEnumerable<BenchmarkBinaryEntity> _entities;

    public BenchmarkBinarySchema(IEnumerable<BenchmarkBinaryEntity> entities)
        : base("test", CreateLibrary())
    {
        _entities = entities;
    }

    public override ISchemaTable GetTableByName(string name, RuntimeContext runtimeContext, params object[] parameters)
    {
        return new BenchmarkBinaryEntityTable();
    }

    public override RowSource GetRowSource(string name, RuntimeContext runtimeContext, params object[] parameters)
    {
        return new BenchmarkEntitySource<BenchmarkBinaryEntity>(
            _entities,
            BenchmarkBinaryEntity.NameToIndexMap,
            BenchmarkBinaryEntity.IndexToObjectAccessMap);
    }

    private static MethodsAggregator CreateLibrary()
    {
        var methodManager = new MethodsManager();
        return new MethodsAggregator(methodManager);
    }
}
