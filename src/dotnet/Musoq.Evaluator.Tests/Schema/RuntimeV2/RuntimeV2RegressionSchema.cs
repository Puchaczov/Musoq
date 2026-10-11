using System.Collections.Generic;
using Musoq.Plugins;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;
using Musoq.Schema.Optimization;

namespace Musoq.Evaluator.Tests.Schema.RuntimeV2;

public sealed class RuntimeV2RegressionSchema(IReadOnlyList<RuntimeV2RegressionEntity> rows)
    : SchemaBase("test", CreateLibrary())
{
    public override SourceDescriptor DescribeSource(string name, SourceDescribeContext context, params object?[] parameters)
        => base.DescribeSource(name, context, parameters) with
        {
            ExecutionPermissions = SourceExecutionPermissions.ConcurrentAccess | SourceExecutionPermissions.ConcurrentRowProcessing
        };

    public override ISchemaTable GetTableByName(
        string name,
        SourceMetadataContext metadataContext,
        params object?[] parameters)
    {
        return new RuntimeV2RegressionTable();
    }

    public override RowSource<T> GetRowSource<T, TArguments>(string name, SourceExecutionContext executionContext, TArguments nativeArguments)
    {
        return EnsureSourceType<T, RuntimeV2RegressionEntity>(name, new RuntimeV2RegressionRowSource(rows));
    }

    private static MethodsAggregator CreateLibrary()
    {
        var methodsManager = new MethodsManager();
        methodsManager.RegisterLibraries(new LibraryBase());
        methodsManager.RegisterLibraries(new RuntimeV2RegressionLibrary());
        return new MethodsAggregator(methodsManager);
    }
}
