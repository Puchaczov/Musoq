using System.Collections.Frozen;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Musoq.Evaluator.Diagnostics;
using Musoq.Evaluator.Tables;
using Musoq.Schema;

namespace Musoq.Targets.CSharpClr;

internal sealed record ClrBatchTableActivationRequest(
    string RunnableTypeName,
    QueryRuntimeBinding Binding);

internal sealed record ClrBatchTableActivationResult(
    ITableRunnable? Runnable,
    Exception? Exception)
{
    public bool Succeeded => Runnable is not null && Exception is null;
}

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

    internal IReadOnlyList<ClrBatchTableActivationResult> ActivateTableBatch(
        ExecutableQueryArtifact executable,
        IReadOnlyList<ClrBatchTableActivationRequest> requests)
    {
        ArgumentNullException.ThrowIfNull(executable);
        ArgumentNullException.ThrowIfNull(requests);
        if (requests.Count == 0)
            return Array.Empty<ClrBatchTableActivationResult>();

        var artifact = executable as ClrAssemblyExecutableArtifact ??
                       throw CreateUnsupportedArtifactException(executable);
        var loadContext = new RuntimeQueryAssemblyLoadContext($"musoq-query-batch-{Guid.NewGuid():N}");
        var lifetime = new SharedAssemblyLoadContextLifetime(loadContext);
        try
        {
            var assembly = LoadAssembly(artifact, loadContext);
            var results = new ClrBatchTableActivationResult[requests.Count];
            for (var index = 0; index < requests.Count; index++)
            {
                var request = requests[index];
                IDisposable? lease = null;
                ITableRunnable? runnable = null;
                try
                {
                    if (string.IsNullOrWhiteSpace(request.RunnableTypeName))
                        throw new ArgumentException("Runnable type name cannot be empty.", nameof(requests));

                    var runnableType = assembly.GetType(request.RunnableTypeName) ??
                                       throw new InvalidOperationException(
                                           $"Type {request.RunnableTypeName} was not found in assembly {assembly.FullName}.");
                    runnable = ActivateTable(runnableType, request.Binding);
                    lease = lifetime.Acquire();
                    runnable = OwnedTableRunnable.Wrap(runnable, lease);
                    lease = null;
                    results[index] = new ClrBatchTableActivationResult(runnable, null);
                    runnable = null;
                }
                catch (Exception exception)
                {
                    (runnable as IDisposable)?.Dispose();
                    lease?.Dispose();
                    results[index] = new ClrBatchTableActivationResult(null, exception);
                }
            }

            return results;
        }
        finally
        {
            lifetime.Dispose();
        }
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
        var assemblyStream = artifact.OpenDllStream(out var disposeAssemblyStream);
        var symbolsStream = artifact.OpenPdbStream(out var disposeSymbolsStream);
        try
        {
            return symbolsStream is null
                ? loadContext.LoadFromStream(assemblyStream)
                : loadContext.LoadFromStream(assemblyStream, symbolsStream);
        }
        finally
        {
            if (disposeSymbolsStream)
                symbolsStream?.Dispose();
            if (disposeAssemblyStream)
                assemblyStream.Dispose();
        }
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
        private static readonly FrozenDictionary<string, Assembly> DefaultAssembliesByName =
            Default.Assemblies
                .Where(static assembly => assembly.GetName().Name is not null)
                .GroupBy(static assembly => assembly.GetName().Name!, StringComparer.OrdinalIgnoreCase)
                .ToFrozenDictionary(
                    static group => group.Key,
                    static group => group.First(),
                    StringComparer.OrdinalIgnoreCase);

        protected override Assembly? Load(AssemblyName assemblyName)
        {
            if (assemblyName.Name is { } simpleName &&
                DefaultAssembliesByName.TryGetValue(simpleName, out var assembly) &&
                AssemblyName.ReferenceMatchesDefinition(assembly.GetName(), assemblyName))
            {
                return assembly;
            }

            try
            {
                return Default.LoadFromAssemblyName(assemblyName);
            }
            catch (FileNotFoundException)
            {
                return null;
            }
            catch (FileLoadException)
            {
                return null;
            }
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

    private sealed class SharedAssemblyLoadContextLifetime(AssemblyLoadContext loadContext) : IDisposable
    {
        private readonly object _gate = new();
        private AssemblyLoadContext? _loadContext = loadContext;
        private int _referenceCount = 1;

        public IDisposable Acquire()
        {
            lock (_gate)
            {
                if (_loadContext is null)
                    throw new ObjectDisposedException(nameof(SharedAssemblyLoadContextLifetime));

                _referenceCount++;
                return new SharedAssemblyLoadContextLease(this);
            }
        }

        public void Dispose()
        {
            AssemblyLoadContext? context = null;
            lock (_gate)
            {
                if (_referenceCount == 0)
                    return;

                _referenceCount--;
                if (_referenceCount == 0)
                {
                    context = _loadContext;
                    _loadContext = null;
                }
            }

            context?.Unload();
        }

        private sealed class SharedAssemblyLoadContextLease(SharedAssemblyLoadContextLifetime owner) : IDisposable
        {
            private SharedAssemblyLoadContextLifetime? _owner = owner;

            ~SharedAssemblyLoadContextLease()
            {
                Dispose();
            }

            public void Dispose()
            {
                if (Interlocked.Exchange(ref _owner, null) is { } current)
                {
                    current.Dispose();
                    GC.SuppressFinalize(this);
                }
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
