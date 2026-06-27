using Musoq.Plugins;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;

namespace Musoq.Benchmarks;

public class TestSchema(List<TestEntity> entities) : SchemaBase("test", CreateMethods())
{
    public override ISchemaTable GetTableByName(string name, SourceMetadataContext metadataContext, params object?[] parameters)
    {
        return new TestTable();
    }

    public override RowSource<T> GetRowSource<T>(string name, SourceExecutionContext executionContext, params object?[] parameters)
    {
        return EnsureSourceType<T, TestEntity>(name, new TestRowSource(entities));
    }

    private static MethodsAggregator CreateMethods()
    {
        var methodManager = new MethodsManager();
        methodManager.RegisterLibraries(new LibraryBase());
        return new MethodsAggregator(methodManager);
    }
}
