using System.Collections.Generic;
using System.Threading;
using Musoq.Evaluator;
using Musoq.Schema;

namespace Musoq.Converter;

public static partial class InstanceCreator
{
    public static CompiledTypedProfileQuery<TOut> CompileForTypedProfile<TOut>(
        string script,
        string assemblyName,
        ISchemaProvider schemaProvider,
        ILoggerResolver loggerResolver)
    {
        return CompileForTypedProfile<TOut>(
            script,
            assemblyName,
            schemaProvider,
            loggerResolver,
            null,
            CancellationToken.None);
    }

    public static CompiledTypedProfileQuery<TOut> CompileForTypedProfile<TOut>(
        string script,
        string assemblyName,
        ISchemaProvider schemaProvider,
        ILoggerResolver loggerResolver,
        CancellationToken cancellationToken)
    {
        return CompileForTypedProfile<TOut>(
            script,
            assemblyName,
            schemaProvider,
            loggerResolver,
            null,
            cancellationToken);
    }

    public static CompiledTypedProfileQuery<TOut> CompileForTypedProfile<TOut>(
        string script,
        string assemblyName,
        ISchemaProvider schemaProvider,
        ILoggerResolver loggerResolver,
        CompilationOptions? compilationOptions)
    {
        return CompileForTypedProfile<TOut>(
            script,
            assemblyName,
            schemaProvider,
            loggerResolver,
            compilationOptions,
            CancellationToken.None);
    }

    public static CompiledTypedProfileQuery<TOut> CompileForTypedProfile<TOut>(
        string script,
        string assemblyName,
        ISchemaProvider schemaProvider,
        ILoggerResolver loggerResolver,
        CompilationOptions? compilationOptions,
        CancellationToken cancellationToken)
    {
        return CompileForTypedProfile<TOut>(
            script,
            assemblyName,
            schemaProvider,
            loggerResolver,
            compilationOptions,
            [],
            cancellationToken);
    }

    internal static CompiledTypedProfileQuery<TOut> CompileForTypedProfile<TOut>(
        string script,
        string assemblyName,
        ISchemaProvider schemaProvider,
        ILoggerResolver loggerResolver,
        CompilationOptions? compilationOptions,
        IReadOnlyList<Type> additionalReferenceTypes,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(additionalReferenceTypes);

        var factory = CompileForTypedProfileFactory<TOut>(
            script,
            assemblyName,
            schemaProvider,
            loggerResolver,
            compilationOptions,
            additionalReferenceTypes,
            cancellationToken);

        var result = factory.Create(schemaProvider);
        cancellationToken.ThrowIfCancellationRequested();
        return result;
    }

    private static CompilationOptions CreateTypedProfileCompilationOptions(CompilationOptions? compilationOptions)
    {
        var options = compilationOptions ?? new CompilationOptions();

        return options.InstrumentationMode == QueryInstrumentationMode.Disabled
            ? options.WithInstrumentationMode(QueryInstrumentationMode.SourceBoundaries)
            : options;
    }
}
