using System.Collections.Generic;
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
        IReadOnlyList<Type> additionalReferenceTypes)
    {
        var items = BuildTypedItems<TOut>(
            script,
            assemblyName,
            schemaProvider,
            loggerResolver,
            compilationOptions,
            additionalReferenceTypes,
            CreateExecutableBuildChain);

        var product = CreateTypedBuildProduct(items, LoadRunnableType(items));
        return CreateTypedRunnableFactory<TOut>(product, loggerResolver);
    }
}
