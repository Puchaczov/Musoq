using Musoq.Plugins;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;

namespace Musoq.Benchmarks;

public class OptBenchSchema : SchemaBase
{
    private readonly List<OptBenchEntity> _data;

    public OptBenchSchema(List<OptBenchEntity> data)
        : base("test", CreateMethods())
    {
        _data = data;
    }

    private static MethodsAggregator CreateMethods()
    {
        var manager = new MethodsManager();
        manager.RegisterLibraries(new LibraryBase());
        manager.RegisterLibraries(new OptBenchLibrary());
        return new MethodsAggregator(manager);
    }

    public override ISchemaTable GetTableByName(string name, RuntimeContext runtimeContext, params object[] parameters)
    {
        return new OptBenchTable();
    }

    public override RowSource GetRowSource(string name, RuntimeContext runtimeContext, params object[] parameters)
    {
        return new OptBenchRowSource(_data);
    }
}
