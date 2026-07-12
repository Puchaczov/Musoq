using System;
using System.Reflection;
using Musoq.Evaluator;

namespace Musoq.Targets.CSharpClr;

internal sealed class ClrAssemblyExecutableActivator : IClrExecutableQueryActivator
{
    public ExecutionTargetId TargetId => ExecutionTargetIds.CSharpClr;

    public ExecutableQueryArtifact CreateLoadedExecutableArtifact(
        Type runnableType,
        IDisposable? lifetimeOwner = null)
    {
        ArgumentNullException.ThrowIfNull(runnableType);

        return new ClrLoadedExecutableArtifact(runnableType, lifetimeOwner);
    }

    public ITableRunnable ActivateTable(ExecutableQueryArtifact executable, QueryRuntimeBinding binding)
    {
        return ActivateTable(LoadRunnableType(executable), binding);
    }

    public ITableRunnable ActivateTable(
        ExecutableQueryArtifact executable,
        QueryRuntimeBinding binding,
        Func<Assembly> loadAssembly)
    {
        return ActivateTable(LoadRunnableType(executable, loadAssembly), binding);
    }

    public ITypedRunnable<TOut> ActivateTyped<TOut>(ExecutableQueryArtifact executable, QueryRuntimeBinding binding)
    {
        return ActivateTyped<TOut>(LoadRunnableType(executable), binding);
    }

    internal ITableRunnable ActivateTable(Type runnableType, QueryRuntimeBinding binding)
    {
        ArgumentNullException.ThrowIfNull(runnableType);
        ArgumentNullException.ThrowIfNull(binding);

        var runnable = Activator.CreateInstance(runnableType) as ITableRunnable;
        if (runnable is null)
            throw new InvalidOperationException($"Could not create instance of type {runnableType.FullName}.");

        ApplyBinding(runnable, binding);
        return runnable;
    }

    internal ITypedRunnable<TOut> ActivateTyped<TOut>(Type runnableType, QueryRuntimeBinding binding)
    {
        ArgumentNullException.ThrowIfNull(runnableType);
        ArgumentNullException.ThrowIfNull(binding);

        var runnable = Activator.CreateInstance(runnableType) as ITypedRunnable<TOut>;
        if (runnable is null)
            throw new InvalidOperationException($"Could not create typed instance of type {runnableType.FullName}.");

        ApplyBinding(runnable, binding);
        return runnable;
    }

    internal Type LoadRunnableType(ExecutableQueryArtifact executable)
    {
        ArgumentNullException.ThrowIfNull(executable);

        return executable switch
        {
            ClrLoadedExecutableArtifact loadedArtifact => loadedArtifact.RunnableType,
            ClrAssemblyExecutableArtifact assemblyArtifact => LoadRunnableType(assemblyArtifact, () =>
                assemblyArtifact.PdbFile is { Length: > 0 } pdbFile
                    ? Assembly.Load(assemblyArtifact.DllFile, pdbFile)
                    : Assembly.Load(assemblyArtifact.DllFile)),
            _ => throw CreateUnsupportedArtifactException(executable)
        };
    }

    internal Type LoadRunnableType(ExecutableQueryArtifact executable, Func<Assembly> loadAssembly)
    {
        ArgumentNullException.ThrowIfNull(executable);

        return executable switch
        {
            ClrLoadedExecutableArtifact loadedArtifact => loadedArtifact.RunnableType,
            ClrAssemblyExecutableArtifact assemblyArtifact => LoadRunnableType(assemblyArtifact, loadAssembly),
            _ => throw CreateUnsupportedArtifactException(executable)
        };
    }

    private static Type LoadRunnableType(ClrAssemblyExecutableArtifact artifact, Func<Assembly> loadAssembly)
    {
        ArgumentNullException.ThrowIfNull(loadAssembly);

        var assembly = loadAssembly();
        var type = assembly.GetType(artifact.RunnableTypeName);

        if (type is null)
            throw new InvalidOperationException(
                $"Type {artifact.RunnableTypeName} was not found in assembly {assembly.FullName}.");

        return type;
    }

    private static void ApplyBinding(IQueryRunnable runnable, QueryRuntimeBinding binding)
    {
        runnable.Provider = binding.SchemaProvider;
        runnable.SourceRuntimeSettingsBySourceContextId = binding.SourceRuntimeSettingsBySourceContextId;
        runnable.SourceRuntimeSettingDescriptionsBySourceContextId = binding.SourceRuntimeSettingDescriptionsBySourceContextId;
        runnable.SourceExecutionPlans = binding.SourceExecutionPlans;
    }

    private static InvalidOperationException CreateUnsupportedArtifactException(ExecutableQueryArtifact executable)
    {
        return new InvalidOperationException(
            $"CLR assembly activation requires a CLR executable artifact, but got '{executable.TargetId}'.");
    }
}
