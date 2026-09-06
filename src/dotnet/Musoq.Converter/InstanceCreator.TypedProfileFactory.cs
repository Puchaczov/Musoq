using System.Collections.Generic;
using System.Threading;
using Musoq.Evaluator;
using Musoq.Evaluator.IR.CodeGeneration;
using Musoq.Schema;

namespace Musoq.Converter;

public static partial class InstanceCreator
{
    internal static TypedProfileRunnableFactory<TOut> CompileForTypedProfileFactory<TOut>(
        string script,
        string assemblyName,
        ISchemaProvider schemaProvider,
        ILoggerResolver loggerResolver,
        CompilationOptions? compilationOptions,
        IReadOnlyList<Type> additionalReferenceTypes,
        CancellationToken cancellationToken = default)
    {
        var items = BuildTypedItems<TOut>(
            script,
            assemblyName,
            schemaProvider,
            loggerResolver,
            CreateTypedProfileCompilationOptions(compilationOptions),
            additionalReferenceTypes,
            CreateExecutableBuildChain,
            QueryResultMode.Table,
            cancellationToken: cancellationToken);

        using var loadedRunnableType = LoadRunnableTypeWithLifetime(items);
        cancellationToken.ThrowIfCancellationRequested();
        var product = CreateTypedBuildProduct(
            items,
            loadedRunnableType.RunnableType,
            TypedQueryProfileMode.TableBacked);
        var factory = CreateTypedProfileRunnableFactory<TOut>(product, loggerResolver);
        cancellationToken.ThrowIfCancellationRequested();
        loadedRunnableType.RetainForTypeLifetime();
        return factory;
    }
}
