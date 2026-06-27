using Musoq.Plugins;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;

namespace Musoq.Benchmarks;

public class CteBenchSchema(List<CteBenchEntity> entities, int simulatedWorkIterations = 0)
    : SchemaBase("test", CreateLibrary())
{
    private static MethodsAggregator CreateLibrary()
    {
        var methodsManager = new MethodsManager();

        methodsManager.RegisterLibraries(new LibraryBase());
        return new MethodsAggregator(methodsManager);
    }

    public override ISchemaTable GetTableByName(string name, SourceMetadataContext metadataContext, params object?[] parameters)
    {
        return new CteBenchTable();
    }

    public override RowSource<T> GetRowSource<T>(string name, SourceExecutionContext executionContext, params object?[] parameters)
    {
        return EnsureSourceType<T, CteBenchEntity>(name, new CteBenchRowSource(entities, simulatedWorkIterations));
    }
}
