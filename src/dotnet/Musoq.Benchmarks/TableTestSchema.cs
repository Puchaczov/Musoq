using Musoq.Plugins;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;

namespace Musoq.Benchmarks;

public class TableTestSchema : SchemaBase
{
    private readonly List<TableTestEntity> _entities;

    public TableTestSchema(List<TableTestEntity> entities) : base("test", CreateMethods())
    {
        _entities = entities;
    }

    public override ISchemaTable GetTableByName(string name, RuntimeContext runtimeContext, params object[] parameters)
    {
        return new TableTestTable();
    }

    public override RowSource GetRowSource(string name, RuntimeContext runtimeContext, params object[] parameters)
    {
        return new TableTestRowSource(_entities);
    }

    private static MethodsAggregator CreateMethods()
    {
        var methodManager = new MethodsManager();
        methodManager.RegisterLibraries(new LibraryBase());
        methodManager.RegisterLibraries(new BenchmarkLibrary());
        return new MethodsAggregator(methodManager);
    }
}
