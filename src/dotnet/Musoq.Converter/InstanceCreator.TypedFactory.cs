using System.Collections.Generic;
using System.Threading;
using Musoq.Evaluator;
using Musoq.Schema;

namespace Musoq.Converter;

public static partial class InstanceCreator
{
    internal static TypedRunnableFactory<TOut> CompileForTypedExecutionFactory<TOut>(
        string script,
        string assemblyName,
        ISchemaProvider schemaProvider,
        ILoggerResolver loggerResolver,
        CompilationOptions compilationOptions,
        IReadOnlyList<Type> additionalReferenceTypes,
        CancellationToken cancellationToken = default)
    {
        var items = BuildTypedItems<TOut>(
            script,
            assemblyName,
            schemaProvider,
            loggerResolver,
            compilationOptions,
            additionalReferenceTypes,
            CreateExecutableBuildChain,
            cancellationToken: cancellationToken);

        using var loadedRunnableType = LoadRunnableTypeWithLifetime(items);
        cancellationToken.ThrowIfCancellationRequested();
        var product = CreateTypedBuildProduct(items, loadedRunnableType.RunnableType);
        var factory = CreateTypedRunnableFactory<TOut>(product, loggerResolver);
        cancellationToken.ThrowIfCancellationRequested();
        loadedRunnableType.RetainForTypeLifetime();
        return factory;
    }
}
