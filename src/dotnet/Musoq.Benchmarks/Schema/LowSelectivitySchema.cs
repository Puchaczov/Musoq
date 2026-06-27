using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;

namespace Musoq.Benchmarks.Schema;

public class LowSelectivitySchema(IEnumerable<NonEquiEntity> entitiesA, IEnumerable<NonEquiEntity> entitiesB)
    : SchemaBase("test", CreateLibrary())
{
    private readonly IReadOnlyList<NonEquiEntity> _entitiesA = entitiesA as IReadOnlyList<NonEquiEntity> ?? entitiesA.ToArray();
    private readonly IReadOnlyList<NonEquiEntity> _entitiesB = entitiesB as IReadOnlyList<NonEquiEntity> ?? entitiesB.ToArray();

    public override ISchemaTable GetTableByName(string name, SourceMetadataContext metadataContext, params object?[] parameters)
    {
        return new NonEquiTable();
    }

    public override RowSource<T> GetRowSource<T>(string name, SourceExecutionContext executionContext, params object?[] parameters)
    {
        var entities = name.Equals("A", StringComparison.OrdinalIgnoreCase) ? _entitiesA : _entitiesB;

        return EnsureSourceType<T, NonEquiEntity>(name, new EntitySource<NonEquiEntity>(BenchmarkSourceChunks.Create(entities), new Dictionary<string, int>
        {
            { nameof(NonEquiEntity.Id), 0 },
            { nameof(NonEquiEntity.Name), 1 },
            { nameof(NonEquiEntity.Population), 2 }
        }, new Dictionary<int, Func<NonEquiEntity, object?>>
        {
            { 0, e => e.Id },
            { 1, e => e.Name },
            { 2, e => e.Population }
        }));
    }

    private static MethodsAggregator CreateLibrary()
    {
        var methodManager = new MethodsManager();
        var lib = new Library();
        methodManager.RegisterLibraries(lib);
        return new MethodsAggregator(methodManager);
    }
}
