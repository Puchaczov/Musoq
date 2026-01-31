using Musoq.Plugins;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;

namespace Musoq.Benchmarks;

public class TestSchema : SchemaBase
{
    private readonly List<TestEntity> _entities;

    public TestSchema(List<TestEntity> entities) : base("test", CreateMethods())
    {
        _entities = entities;
    }

    public override ISchemaTable GetTableByName(string name, RuntimeContext runtimeContext, params object[] parameters)
    {
        return new TestTable();
    }

    public override RowSource GetRowSource(string name, RuntimeContext runtimeContext, params object[] parameters)
    {
        return new TestRowSource(_entities);
    }

    private static MethodsAggregator CreateMethods()
    {
        var methodManager = new MethodsManager();
        methodManager.RegisterLibraries(new LibraryBase());
        return new MethodsAggregator(methodManager);
    }
}
