using Musoq.Plugins;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;

namespace Musoq.Benchmarks;

public class CseTestSchema : SchemaBase
{
    private readonly IReadOnlyCollection<CseTestEntity> _data;

    public CseTestSchema(IReadOnlyCollection<CseTestEntity> data)
        : base("test", CreateMethods())
    {
        _data = data;
    }

    private static MethodsAggregator CreateMethods()
    {
        var manager = new MethodsManager();
        manager.RegisterLibraries(new LibraryBase());
        manager.RegisterLibraries(new CseTestLibrary());
        return new MethodsAggregator(manager);
    }

    public override ISchemaTable GetTableByName(string name, RuntimeContext runtimeContext, params object[] parameters)
    {
        return new CseTestTable();
    }

    public override RowSource GetRowSource(string name, RuntimeContext runtimeContext, params object[] parameters)
    {
        return new CseTestRowSource(_data);
    }
}
