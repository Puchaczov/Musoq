using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;

namespace Musoq.Playground;

internal sealed class NonEquiSchema(IReadOnlyList<NonEquiEntity> entities, int simulatedWorkIterations = 0)
    : SchemaBase("test", CreateLibrary())
{
    public override ISchemaTable GetTableByName(string name, SourceMetadataContext metadataContext, params object?[] parameters)
    {
        return new NonEquiTable();
    }

    public override RowSource<T> GetRowSource<T>(string name, SourceExecutionContext executionContext, params object?[] parameters)
    {
        return EnsureSourceType<T, NonEquiEntity>(name, new ExpensiveRowSource(entities, simulatedWorkIterations));
    }

    private static MethodsAggregator CreateLibrary()
    {
        var methodManager = new MethodsManager();
        var lib = new Library();
        methodManager.RegisterLibraries(lib);
        return new MethodsAggregator(methodManager);
    }
}
