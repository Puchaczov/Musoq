using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;

namespace Musoq.Playground;

public class NonEquiSchema : SchemaBase
{
    private readonly IEnumerable<NonEquiEntity> _entities;
    private readonly int _simulatedWorkIterations;

    public NonEquiSchema(IEnumerable<NonEquiEntity> entities, int simulatedWorkIterations = 0)
        : base("test", CreateLibrary())
    {
        _entities = entities;
        _simulatedWorkIterations = simulatedWorkIterations;
    }

    public override ISchemaTable GetTableByName(string name, RuntimeContext runtimeContext, params object[] parameters)
    {
        return new NonEquiTable();
    }

    public override RowSource GetRowSource(string name, RuntimeContext runtimeContext, params object[] parameters)
    {
        return new ExpensiveRowSource(_entities, _simulatedWorkIterations);
    }

    private static MethodsAggregator CreateLibrary()
    {
        var methodManager = new MethodsManager();
        var lib = new Library();
        methodManager.RegisterLibraries(lib);
        return new MethodsAggregator(methodManager);
    }
}
