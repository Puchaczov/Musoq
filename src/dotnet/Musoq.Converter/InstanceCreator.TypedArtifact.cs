using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Musoq.Evaluator;
using Musoq.Evaluator.IR.CodeGeneration;
using Musoq.Schema;

namespace Musoq.Converter;

public static partial class InstanceCreator
{
    public static ICompiledTypedQueryArtifact CompileForTypedArtifact<TOut>(
        string script,
        string assemblyName,
        ISchemaProvider schemaProvider,
        ILoggerResolver loggerResolver)
    {
        return CompileForTypedArtifact<TOut>(
            script,
            assemblyName,
            schemaProvider,
            loggerResolver,
            new CompilationOptions());
    }

    public static ICompiledTypedQueryArtifact CompileForTypedArtifact<TOut>(
        string script,
        string assemblyName,
        ISchemaProvider schemaProvider,
        ILoggerResolver loggerResolver,
        CompilationOptions compilationOptions)
    {
        return CompileForTypedArtifact<TOut>(
            script,
            assemblyName,
            schemaProvider,
            loggerResolver,
            compilationOptions,
            [],
            []);
    }

    internal static CompiledTypedQueryArtifact CompileForTypedArtifact<TOut>(
        string script,
        string assemblyName,
        ISchemaProvider schemaProvider,
        ILoggerResolver loggerResolver,
        CompilationOptions compilationOptions,
        IReadOnlyList<Type> additionalReferenceTypes,
        IReadOnlyList<InMemorySourceSlot> inMemorySourceSlots)
    {
        ArgumentNullException.ThrowIfNull(inMemorySourceSlots);
        var items = BuildTypedItems<TOut>(
            script,
            assemblyName,
            schemaProvider,
            loggerResolver,
            compilationOptions,
            additionalReferenceTypes,
            CreateExecutableBuildChain);
        var product = CreateTypedBuildProduct(items, null);

        return new CompiledTypedQueryArtifact(
            items.DllFile ?? throw new InvalidOperationException("Compilation did not produce a DLL file."),
            items.PdbFile,
            items.AccessToClassPath,
            product.ResultMode,
            typeof(TOut),
            product.SourceRuntimeSettingsBySourceContextId,
            product.SourceRuntimeSettingDescriptionsBySourceContextId,
            product.SourceExecutionPlans,
            product.ParameterDefinitions,
            inMemorySourceSlots.ToArray());
    }

    public static CompiledTypedQuery<TOut> LoadTypedArtifact<TOut>(
        ICompiledTypedQueryArtifact artifact,
        ISchemaProvider schemaProvider,
        ILoggerResolver loggerResolver)
    {
        ArgumentNullException.ThrowIfNull(schemaProvider);

        var factory = LoadTypedArtifactFactory<TOut>(artifact, loggerResolver);
        return factory.Create(schemaProvider);
    }

    internal static TypedRunnableFactory<TOut> LoadTypedArtifactFactory<TOut>(
        ICompiledTypedQueryArtifact artifact,
        ILoggerResolver loggerResolver)
    {
        ArgumentNullException.ThrowIfNull(artifact);
        ArgumentNullException.ThrowIfNull(loggerResolver);

        var dllFile = artifact is CompiledTypedQueryArtifact ownedArtifact
            ? ownedArtifact.DllFileUnsafe
            : artifact.DllFile;
        var pdbFile = artifact is CompiledTypedQueryArtifact ownedPdbArtifact
            ? ownedPdbArtifact.PdbFileUnsafe
            : artifact.PdbFile;

        if (artifact.ArtifactVersion != CompiledTypedQueryArtifact.CurrentArtifactVersion)
        {
            throw new InvalidOperationException(
                $"Typed query artifact version '{artifact.ArtifactVersion}' is not supported. Expected '{CompiledTypedQueryArtifact.CurrentArtifactVersion}'.");
        }

        if (dllFile.Length == 0)
            throw new InvalidOperationException("Cannot load typed query artifact because the DLL file is empty.");
        if (artifact.ResultMode != QueryResultMode.TypedEnumerable)
            throw new InvalidOperationException($"Typed query artifacts must use {QueryResultMode.TypedEnumerable} result mode.");
        if (artifact.OutputType != typeof(TOut))
        {
            throw new InvalidOperationException(
                $"Typed query artifact output type '{artifact.OutputType.FullName}' cannot be loaded as '{typeof(TOut).FullName}'.");
        }

        var assembly = pdbFile is { Length: > 0 }
            ? Assembly.Load(dllFile, pdbFile)
            : Assembly.Load(dllFile);
        var runnableType = assembly.GetType(artifact.RunnableTypeName);
        if (runnableType == null)
            throw new InvalidOperationException($"Type {artifact.RunnableTypeName} was not found in artifact assembly {assembly.FullName}.");

        var product = CreateTypedBuildProduct(artifact, runnableType);
        return CreateTypedRunnableFactory<TOut>(product, loggerResolver);
    }
}
