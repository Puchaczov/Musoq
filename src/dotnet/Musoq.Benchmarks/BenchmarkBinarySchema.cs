using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;

namespace Musoq.Benchmarks;

/// <summary>
///     Binary schema for benchmarks.
/// </summary>
public class BenchmarkBinarySchema(IEnumerable<IReadOnlyList<BenchmarkBinaryEntity>> chunks) : SchemaBase("test", CreateLibrary())
{
    public override ISchemaTable GetTableByName(string name, SourceMetadataContext metadataContext, params object?[] parameters)
    {
        return new BenchmarkBinaryEntityTable();
    }

    public override RowSource<T> GetRowSource<T>(string name, SourceExecutionContext executionContext, params object?[] parameters)
    {
        return EnsureSourceType<T, BenchmarkBinaryEntity>(name, new BenchmarkEntitySource<BenchmarkBinaryEntity>(
            chunks,
            BenchmarkBinaryEntity.NameToIndexMap,
            BenchmarkBinaryEntity.IndexToObjectAccessMap));
    }

    private static MethodsAggregator CreateLibrary()
    {
        var methodManager = new MethodsManager();
        return new MethodsAggregator(methodManager);
    }
}
