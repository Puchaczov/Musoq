using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;

namespace Musoq.Benchmarks.Schema;

public class LowSelectivitySchema : SchemaBase
{
    private readonly IEnumerable<NonEquiEntity> _entitiesA;
    private readonly IEnumerable<NonEquiEntity> _entitiesB;

    public LowSelectivitySchema(IEnumerable<NonEquiEntity> entitiesA, IEnumerable<NonEquiEntity> entitiesB) : base(
        "test", CreateLibrary())
    {
        _entitiesA = entitiesA;
        _entitiesB = entitiesB;
    }

    public override ISchemaTable GetTableByName(string name, RuntimeContext runtimeContext, params object[] parameters)
    {
        return new NonEquiTable();
    }

    public override RowSource GetRowSource(string name, RuntimeContext runtimeContext, params object[] parameters)
    {
        var entities = name.Equals("A", StringComparison.OrdinalIgnoreCase) ? _entitiesA : _entitiesB;

        return new EntitySource<NonEquiEntity>(entities, new Dictionary<string, int>
        {
            { nameof(NonEquiEntity.Id), 0 },
            { nameof(NonEquiEntity.Name), 1 },
            { nameof(NonEquiEntity.Population), 2 }
        }, new Dictionary<int, Func<NonEquiEntity, object>>
        {
            { 0, e => e.Id },
            { 1, e => e.Name },
            { 2, e => e.Population }
        });
    }

    private static MethodsAggregator CreateLibrary()
    {
        var methodManager = new MethodsManager();
        var lib = new Library();
        methodManager.RegisterLibraries(lib);
        return new MethodsAggregator(methodManager);
    }
}