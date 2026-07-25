using System.Collections.Generic;
using System.IO;
using System;
using System.Reflection;
using System.Runtime.Loader;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Musoq.Evaluator;
using Musoq.Evaluator.Diagnostics;
using Musoq.Evaluator.Tables;
using Musoq.Schema;
using Musoq.Schema.Optimization;

namespace Musoq.Targets.CSharpClr;

internal sealed class ClrAssemblyExecutableActivator : IClrExecutableQueryActivator
{
    private static readonly ConditionalWeakTable<Type, AssemblyLoadContextLifetime> TypeLifetimes = new();

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
        return executable switch
        {
            ClrLoadedExecutableArtifact loadedArtifact => ActivateTable(loadedArtifact.RunnableType, binding),
            ClrAssemblyExecutableArtifact assemblyArtifact => ActivateAssemblyTable(assemblyArtifact, binding),
            _ => throw CreateUnsupportedArtifactException(executable)
        };
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
            ClrAssemblyExecutableArtifact assemblyArtifact => LoadRunnableTypeAndRetainLifetime(assemblyArtifact),
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

    private static Type LoadRunnableTypeAndRetainLifetime(ClrAssemblyExecutableArtifact artifact)
    {
        var loadContext = new RuntimeQueryAssemblyLoadContext($"musoq-query-type-{Guid.NewGuid():N}");
        try
        {
            var assembly = LoadAssembly(artifact, loadContext);
            var type = assembly.GetType(artifact.RunnableTypeName) ??
                       throw new InvalidOperationException(
                           $"Type {artifact.RunnableTypeName} was not found in assembly {assembly.FullName}.");
            TypeLifetimes.Add(type, new AssemblyLoadContextLifetime(loadContext));
            return type;
        }
        catch
        {
            loadContext.Unload();
            throw;
        }
    }

    private static Assembly LoadAssembly(
        ClrAssemblyExecutableArtifact artifact,
        AssemblyLoadContext loadContext)
    {
        using var assemblyStream = new MemoryStream(artifact.DllFile, writable: false);
        if (artifact.PdbFile is not { Length: > 0 } pdbFile)
            return loadContext.LoadFromStream(assemblyStream);

        using var symbolsStream = new MemoryStream(pdbFile, writable: false);
        return loadContext.LoadFromStream(assemblyStream, symbolsStream);
    }

    private ITableRunnable ActivateAssemblyTable(
        ClrAssemblyExecutableArtifact artifact,
        QueryRuntimeBinding binding)
    {
        var loadContext = new RuntimeQueryAssemblyLoadContext($"musoq-query-{Guid.NewGuid():N}");
        try
        {
            var assembly = LoadAssembly(artifact, loadContext);

            var runnableType = assembly.GetType(artifact.RunnableTypeName) ??
                               throw new InvalidOperationException(
                                   $"Type {artifact.RunnableTypeName} was not found in assembly {assembly.FullName}.");
            var runnable = ActivateTable(runnableType, binding);
            return OwnedTableRunnable.Wrap(runnable, new AssemblyLoadContextLifetime(loadContext));
        }
        catch
        {
            loadContext.Unload();
            throw;
        }
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

    private sealed class RuntimeQueryAssemblyLoadContext(string name) : AssemblyLoadContext(name, isCollectible: true)
    {
        protected override Assembly? Load(AssemblyName assemblyName)
        {
            foreach (var assembly in Default.Assemblies)
            {
                if (AssemblyName.ReferenceMatchesDefinition(assembly.GetName(), assemblyName))
                    return assembly;
            }

            return null;
        }
    }

    private sealed class AssemblyLoadContextLifetime(AssemblyLoadContext loadContext) : IDisposable
    {
        private AssemblyLoadContext? _loadContext = loadContext;

        ~AssemblyLoadContextLifetime()
        {
            Dispose();
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _loadContext, null) is { } context)
            {
                context.Unload();
                GC.SuppressFinalize(this);
            }
        }
    }

    private abstract class OwnedTableRunnableBase(ITableRunnable inner, IDisposable lifetimeOwner)
        : ITableRunnable, IParameterizedRunnable, IProfiledRunnable, IDisposable
    {
        private readonly IParameterizedRunnable? _parameterized = inner as IParameterizedRunnable;
        private readonly Dictionary<string, object?> _fallbackParameters = new(StringComparer.Ordinal);
        private int _disposed;

        ~OwnedTableRunnableBase()
        {
            Dispose();
        }

        protected ITableRunnable Inner { get; } = inner ?? throw new ArgumentNullException(nameof(inner));

        private IDisposable LifetimeOwner { get; } = lifetimeOwner ?? throw new ArgumentNullException(nameof(lifetimeOwner));

        public ISchemaProvider Provider
        {
            get => Inner.Provider;
            set => Inner.Provider = value;
        }

        public IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> SourceRuntimeSettingsBySourceContextId
        {
            get => Inner.SourceRuntimeSettingsBySourceContextId;
            set => Inner.SourceRuntimeSettingsBySourceContextId = value;
        }

        public IReadOnlyDictionary<string, IReadOnlyList<SourceRuntimeSettingDescription>> SourceRuntimeSettingDescriptionsBySourceContextId
        {
            get => Inner.SourceRuntimeSettingDescriptionsBySourceContextId;
            set => Inner.SourceRuntimeSettingDescriptionsBySourceContextId = value;
        }

        public IReadOnlyDictionary<string, SourceExecutionPlan> SourceExecutionPlans
        {
            get => Inner.SourceExecutionPlans;
            set => Inner.SourceExecutionPlans = value;
        }

        public ILogger Logger
        {
            get => Inner.Logger;
            set => Inner.Logger = value;
        }

        public IDictionary<string, object?> Parameters => _parameterized?.Parameters ?? _fallbackParameters;

        public IReadOnlyList<ScriptParameterDefinition> ParameterDefinitions =>
            _parameterized?.ParameterDefinitions ?? Array.Empty<ScriptParameterDefinition>();

        public IReadOnlyList<ScriptParameterContract> ParameterContracts =>
            _parameterized?.ParameterContracts ?? Array.Empty<ScriptParameterContract>();

        public event QueryPhaseEventHandler PhaseChanged
        {
            add => Inner.PhaseChanged += value;
            remove => Inner.PhaseChanged -= value;
        }

        public event DataSourceEventHandler DataSourceProgress
        {
            add => Inner.DataSourceProgress += value;
            remove => Inner.DataSourceProgress -= value;
        }

        public abstract Table Run(CancellationToken token);

        public Table RunWithProfile(CancellationToken token, QueryProfileRecorder profileRecorder)
        {
            return Inner is IProfiledRunnable profiled
                ? profiled.RunWithProfile(token, profileRecorder)
                : throw new InvalidOperationException("Query was not compiled with profiling instrumentation.");
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
                return;

            try
            {
                (Inner as IDisposable)?.Dispose();
            }
            finally
            {
                LifetimeOwner.Dispose();
                GC.SuppressFinalize(this);
            }
        }
    }

    private sealed class OwnedLegacyTableRunnable(ITableRunnable inner, IDisposable lifetimeOwner)
        : OwnedTableRunnableBase(inner, lifetimeOwner)
    {
        public override Table Run(CancellationToken token) => Inner.Run(token);
    }

    private class OwnedContextTableRunnable(ITableRunnable inner, IDisposable lifetimeOwner)
        : OwnedTableRunnableBase(inner, lifetimeOwner), IContextTableRunnable
    {
        public override Table Run(CancellationToken token) => Inner.Run(token);

        public Table Run(QueryRunContext context)
        {
            return Inner is IContextTableRunnable contextual
                ? contextual.Run(context)
                : Inner.Run(context.CancellationToken);
        }
    }

    private sealed class OwnedContextAsyncTableRunnable(ITableRunnable inner, IDisposable lifetimeOwner)
        : OwnedContextTableRunnable(inner, lifetimeOwner), IContextAsyncTableRunnable
    {
        public async ValueTask<Table> RunAsync(QueryRunContext context)
        {
            return Inner is IContextAsyncTableRunnable contextual
                ? await contextual.RunAsync(context).ConfigureAwait(false)
                : await Task.Run(() => Inner.Run(context.CancellationToken), context.CancellationToken)
                    .ConfigureAwait(false);
        }
    }

    private sealed class OwnedLegacyAsyncTableRunnable(ITableRunnable inner, IDisposable lifetimeOwner)
        : OwnedTableRunnableBase(inner, lifetimeOwner), IAsyncTableRunnable
    {
        public override Table Run(CancellationToken token) => Inner.Run(token);

        public ValueTask<Table> RunAsync(CancellationToken token)
        {
            return Inner is IAsyncTableRunnable asyncRunnable
                ? asyncRunnable.RunAsync(token)
                : new ValueTask<Table>(Task.Run(() => Inner.Run(token), token));
        }
    }

    private static class OwnedTableRunnable
    {
        public static ITableRunnable Wrap(ITableRunnable runnable, IDisposable lifetimeOwner)
        {
            if (runnable is IContextAsyncTableRunnable)
                return new OwnedContextAsyncTableRunnable(runnable, lifetimeOwner);
            if (runnable is IContextTableRunnable)
                return new OwnedContextTableRunnable(runnable, lifetimeOwner);
            if (runnable is IAsyncTableRunnable)
                return new OwnedLegacyAsyncTableRunnable(runnable, lifetimeOwner);
            return new OwnedLegacyTableRunnable(runnable, lifetimeOwner);
        }
    }
}
