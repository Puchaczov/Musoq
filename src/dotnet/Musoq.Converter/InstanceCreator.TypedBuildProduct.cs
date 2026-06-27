using Musoq.Converter.Build;
using Musoq.Evaluator.IR.CodeGeneration;
using Musoq.Schema;

namespace Musoq.Converter;

public static partial class InstanceCreator
{
    private static TypedBuildProduct CreateTypedBuildProduct(
        BuildItems items,
        Type? runnableType,
        TypedQueryProfileMode profileMode = TypedQueryProfileMode.None)
    {
        return new TypedBuildProduct(
            runnableType,
            items.QueryResultMode,
            items.QueryMethodRenderMetadata,
            items.SourceRuntimeSettingsBySourceContextId,
            items.SourceRuntimeSettingDescriptionsBySourceContextId,
            CreateSourceExecutionPlans(items),
            items.ScriptParameterDefinitions,
            TypedQueryDiagnostics.FromMetadata(
                runnableType,
                items.QueryResultMode,
                items.QueryMethodRenderMetadata,
                profileMode));
    }

    private static TypedBuildProduct CreateTypedBuildProduct(
        ICompiledTypedQueryArtifact artifact,
        Type runnableType)
    {
        return new TypedBuildProduct(
            runnableType,
            artifact.ResultMode,
            QueryMethodRenderMetadata.Unknown,
            artifact.SourceRuntimeSettingsBySourceContextId,
            artifact.SourceRuntimeSettingDescriptionsBySourceContextId,
            artifact.SourceExecutionPlans,
            artifact.ParameterDefinitions,
            TypedQueryDiagnostics.FromMetadata(
                runnableType,
                artifact.ResultMode,
                QueryMethodRenderMetadata.Unknown));
    }

    private static TypedRunnableFactory<TOut> CreateTypedRunnableFactory<TOut>(
        TypedBuildProduct product,
        ILoggerResolver loggerResolver)
    {
        return new TypedRunnableFactory<TOut>(
            product.RequireRunnableType(),
            product.SourceRuntimeSettingsBySourceContextId,
            product.SourceRuntimeSettingDescriptionsBySourceContextId,
            product.SourceExecutionPlans,
            product.ParameterDefinitions,
            product.Diagnostics,
            loggerResolver);
    }

    private static TypedProfileRunnableFactory<TOut> CreateTypedProfileRunnableFactory<TOut>(
        TypedBuildProduct product,
        ILoggerResolver loggerResolver)
    {
        return new TypedProfileRunnableFactory<TOut>(
            product.RequireRunnableType(),
            product.SourceRuntimeSettingsBySourceContextId,
            product.SourceRuntimeSettingDescriptionsBySourceContextId,
            product.SourceExecutionPlans,
            product.ParameterDefinitions,
            product.Diagnostics,
            loggerResolver);
    }
}
