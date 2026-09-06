using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Musoq.Evaluator;
using Musoq.Evaluator.IR.CodeGeneration;
using Musoq.Schema;
using Musoq.Targets.CSharpClr;

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
            new CompilationOptions(),
            CancellationToken.None);
    }

    public static ICompiledTypedQueryArtifact CompileForTypedArtifact<TOut>(
        string script,
        string assemblyName,
        ISchemaProvider schemaProvider,
        ILoggerResolver loggerResolver,
        CancellationToken cancellationToken)
    {
        return CompileForTypedArtifact<TOut>(
            script,
            assemblyName,
            schemaProvider,
            loggerResolver,
            new CompilationOptions(),
            cancellationToken);
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
            CancellationToken.None);
    }

    public static ICompiledTypedQueryArtifact CompileForTypedArtifact<TOut>(
        string script,
        string assemblyName,
        ISchemaProvider schemaProvider,
        ILoggerResolver loggerResolver,
        CompilationOptions compilationOptions,
        CancellationToken cancellationToken)
    {
        return CompileForTypedArtifact<TOut>(
            script,
            assemblyName,
            schemaProvider,
            loggerResolver,
            compilationOptions,
            [],
            [],
            cancellationToken);
    }

    internal static CompiledTypedQueryArtifact CompileForTypedArtifact<TOut>(
        string script,
        string assemblyName,
        ISchemaProvider schemaProvider,
        ILoggerResolver loggerResolver,
        CompilationOptions compilationOptions,
        IReadOnlyList<Type> additionalReferenceTypes,
        IReadOnlyList<InMemorySourceSlot> inMemorySourceSlots,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(inMemorySourceSlots);
        var items = BuildTypedItems<TOut>(
            script,
            assemblyName,
            schemaProvider,
            loggerResolver,
            compilationOptions,
            additionalReferenceTypes,
            CreateExecutableBuildChain,
            cancellationToken: cancellationToken);
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
        return LoadTypedArtifact<TOut>(artifact, schemaProvider, loggerResolver, CancellationToken.None);
    }

    public static CompiledTypedQuery<TOut> LoadTypedArtifact<TOut>(
        ICompiledTypedQueryArtifact artifact,
        ISchemaProvider schemaProvider,
        ILoggerResolver loggerResolver,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(schemaProvider);

        var factory = LoadTypedArtifactFactory<TOut>(artifact, loggerResolver, cancellationToken);
        var result = factory.Create(schemaProvider);
        cancellationToken.ThrowIfCancellationRequested();
        return result;
    }

    internal static TypedRunnableFactory<TOut> LoadTypedArtifactFactory<TOut>(
        ICompiledTypedQueryArtifact artifact,
        ILoggerResolver loggerResolver,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
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

        var activator = RequireClrActivator(ExecutionTargetIds.CSharpClr);
        using var loadedRunnableType = activator.LoadRunnableTypeWithLifetime(
            CSharpClrArtifactCompatibility.CreateAssemblyExecutable(
                dllFile,
                pdbFile,
                artifact.RunnableTypeName));

        cancellationToken.ThrowIfCancellationRequested();

        var product = CreateTypedBuildProduct(artifact, loadedRunnableType.RunnableType);
        var factory = CreateTypedRunnableFactory<TOut>(product, loggerResolver);
        cancellationToken.ThrowIfCancellationRequested();
        loadedRunnableType.RetainForTypeLifetime();
        return factory;
    }
}
