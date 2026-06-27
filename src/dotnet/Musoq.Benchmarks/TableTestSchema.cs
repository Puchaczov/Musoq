using Musoq.Plugins;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;

namespace Musoq.Benchmarks;

public class TableTestSchema(List<TableTestEntity> entities) : SchemaBase("test", CreateMethods())
{
    public override ISchemaTable GetTableByName(string name, SourceMetadataContext metadataContext, params object?[] parameters)
    {
        return new TableTestTable();
    }

    public override RowSource<T> GetRowSource<T>(string name, SourceExecutionContext executionContext, params object?[] parameters)
    {
        return EnsureSourceType<T, TableTestEntity>(name, new TableTestRowSource(entities));
    }

    private static MethodsAggregator CreateMethods()
    {
        var methodManager = new MethodsManager();
        methodManager.RegisterLibraries(new LibraryBase());
        methodManager.RegisterLibraries(new BenchmarkLibrary());
        return new MethodsAggregator(methodManager);
    }
}
