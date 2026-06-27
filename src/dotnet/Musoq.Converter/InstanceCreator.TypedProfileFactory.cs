using System.Collections.Generic;
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
        IReadOnlyList<Type> additionalReferenceTypes)
    {
        var items = BuildTypedItems<TOut>(
            script,
            assemblyName,
            schemaProvider,
            loggerResolver,
            CreateTypedProfileCompilationOptions(compilationOptions),
            additionalReferenceTypes,
            CreateExecutableBuildChain,
            QueryResultMode.Table);

        var product = CreateTypedBuildProduct(items, LoadRunnableType(items), TypedQueryProfileMode.TableBacked);
        return CreateTypedProfileRunnableFactory<TOut>(product, loggerResolver);
    }
}
