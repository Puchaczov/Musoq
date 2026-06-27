using System;
using System.Collections.Generic;
using Musoq.Evaluator;
using Musoq.Evaluator.IR.CodeGeneration;
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
            null);
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
            []);
    }

    internal static CompiledTypedProfileQuery<TOut> CompileForTypedProfile<TOut>(
        string script,
        string assemblyName,
        ISchemaProvider schemaProvider,
        ILoggerResolver loggerResolver,
        CompilationOptions? compilationOptions,
        IReadOnlyList<Type> additionalReferenceTypes)
    {
        ArgumentNullException.ThrowIfNull(additionalReferenceTypes);

        var factory = CompileForTypedProfileFactory<TOut>(
            script,
            assemblyName,
            schemaProvider,
            loggerResolver,
            compilationOptions,
            additionalReferenceTypes);

        return factory.Create(schemaProvider);
    }

    private static CompilationOptions CreateTypedProfileCompilationOptions(CompilationOptions? compilationOptions)
    {
        var options = compilationOptions ?? new CompilationOptions();

        return options.InstrumentationMode == QueryInstrumentationMode.Disabled
            ? options.WithInstrumentationMode(QueryInstrumentationMode.SourceBoundaries)
            : options;
    }
}
