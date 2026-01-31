using Musoq.Plugins;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;

namespace Musoq.Benchmarks;

public class CteBenchSchema : SchemaBase
{
    private readonly List<CteBenchEntity> _entities;
    private readonly int _simulatedWorkIterations;

    public CteBenchSchema(List<CteBenchEntity> entities, int simulatedWorkIterations = 0)
        : base("test", CreateLibrary())
    {
        _entities = entities;
        _simulatedWorkIterations = simulatedWorkIterations;
    }

    private static MethodsAggregator CreateLibrary()
    {
        var methodsManager = new MethodsManager();

        methodsManager.RegisterLibraries(new LibraryBase());
        return new MethodsAggregator(methodsManager);
    }

    public override ISchemaTable GetTableByName(string name, RuntimeContext runtimeContext, params object[] parameters)
    {
        return new CteBenchTable();
    }

    public override RowSource GetRowSource(string name, RuntimeContext runtimeContext, params object[] parameters)
    {
        return new CteBenchRowSource(_entities, _simulatedWorkIterations);
    }
}
