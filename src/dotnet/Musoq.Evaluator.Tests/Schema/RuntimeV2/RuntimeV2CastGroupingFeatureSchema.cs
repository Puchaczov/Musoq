using System.Collections.Generic;
using Musoq.Plugins;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;

namespace Musoq.Evaluator.Tests.Schema.RuntimeV2;

public sealed class RuntimeV2CastGroupingFeatureSchema(IReadOnlyList<RuntimeV2CastGroupingFeatureEntity> rows)
    : SchemaBase("features", CreateLibrary())
{
    public override ISchemaTable GetTableByName(
        string name,
        SourceMetadataContext metadataContext,
        params object?[] parameters)
    {
        return new RuntimeV2CastGroupingFeatureTable();
    }

    public override RowSource<T> GetRowSource<T>(
        string name,
        SourceExecutionContext executionContext,
        params object?[] parameters)
    {
        return EnsureSourceType<T, RuntimeV2CastGroupingFeatureEntity>(
            name,
            new RuntimeV2CastGroupingFeatureRowSource(rows));
    }

    private static MethodsAggregator CreateLibrary()
    {
        var methodsManager = new MethodsManager();
        methodsManager.RegisterLibraries(new LibraryBase());
        return new MethodsAggregator(methodsManager);
    }
}
