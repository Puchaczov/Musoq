using Musoq.Plugins;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;

namespace Musoq.Benchmarks;

public class CseTestSchema(IReadOnlyList<CseTestEntity> data) : SchemaBase("test", CreateMethods())
{
    private static MethodsAggregator CreateMethods()
    {
        var manager = new MethodsManager();
        manager.RegisterLibraries(new LibraryBase());
        manager.RegisterLibraries(new CseTestLibrary());
        return new MethodsAggregator(manager);
    }

    public override ISchemaTable GetTableByName(string name, SourceMetadataContext metadataContext, params object?[] parameters)
    {
        return new CseTestTable();
    }

    public override RowSource<T> GetRowSource<T>(string name, SourceExecutionContext executionContext, params object?[] parameters)
    {
        return EnsureSourceType<T, CseTestEntity>(name, new CseTestRowSource(data));
    }
}
