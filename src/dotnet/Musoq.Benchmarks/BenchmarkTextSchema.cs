using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;

namespace Musoq.Benchmarks;

/// <summary>
///     Text schema for benchmarks.
/// </summary>
public class BenchmarkTextSchema(IEnumerable<IReadOnlyList<BenchmarkTextEntity>> chunks) : SchemaBase("test", CreateLibrary())
{
    public override ISchemaTable GetTableByName(string name, SourceMetadataContext metadataContext, params object?[] parameters)
    {
        return new BenchmarkTextEntityTable();
    }

    public override RowSource<T> GetRowSource<T>(string name, SourceExecutionContext executionContext, params object?[] parameters)
    {
        return EnsureSourceType<T, BenchmarkTextEntity>(name, new BenchmarkEntitySource<BenchmarkTextEntity>(
            chunks,
            BenchmarkTextEntity.NameToIndexMap,
            BenchmarkTextEntity.IndexToObjectAccessMap));
    }

    private static MethodsAggregator CreateLibrary()
    {
        var methodManager = new MethodsManager();
        return new MethodsAggregator(methodManager);
    }
}
