using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using Musoq.Converter.Build;
using Musoq.Evaluator;
using Musoq.Evaluator.IR.CodeGeneration;
using Musoq.Evaluator.IR.Planning;
using Musoq.Schema;
using Musoq.Targets.CSharpClr;

namespace Musoq.Converter;

public static partial class InstanceCreator
{
    private const int DefaultExecutionCompilationCacheLimit = 512;
    private const int CanonicalExecutionCompilationAliasLimit = 2048;

    private static readonly ConcurrentDictionary<ExecutionCompilationCacheKey, CachedExecutionCompilation>
        ExecutionCompilationCache = new();
    private static readonly ConcurrentDictionary<CanonicalExecutionArtifactContract, CachedExecutionCompilation>
        CanonicalExecutionCompilationCache = new();
    private static readonly object ExecutionCompilationCacheMutationSync = new();
    private static readonly List<CachedExecutionCompilation> ExecutionCompilationEntries = [];
    private static readonly object ExecutionCompilationFlightSync = new();
    private static readonly Dictionary<ExecutionCompilationCacheKey, ExecutionCompilationFlight>
        ExecutionCompilationFlights = new();
    private static readonly object CanonicalExecutionCompilationFlightSync = new();
    private static readonly Dictionary<CanonicalExecutionArtifactContract, ExecutionCompilationFlight>
        CanonicalExecutionCompilationFlights = new();
    private static readonly AsyncLocal<Action?> ExecutionCompilationCommitTestHook = new();

    private static long _executionCompilationAccessTick;
    private static int _executionCompilationCacheLimit = DefaultExecutionCompilationCacheLimit;

    private static long NextExecutionCompilationAccessTick()
    {
        return Interlocked.Increment(ref _executionCompilationAccessTick);
    }

    private static IDisposable AcquireExecutionCompilationFlight(
        ExecutionCompilationCacheKey cacheKey,
        CancellationToken cancellationToken,
        Action? waiterRegistered = null)
    {
        ExecutionCompilationFlight flight;
        lock (ExecutionCompilationFlightSync)
        {
            if (!ExecutionCompilationFlights.TryGetValue(cacheKey, out flight!))
            {
                flight = new ExecutionCompilationFlight();
                ExecutionCompilationFlights.Add(cacheKey, flight);
            }

            flight.Waiters++;
        }

        try
        {
            waiterRegistered?.Invoke();
            flight.Gate.Wait(cancellationToken);
        }
        catch
        {
            lock (ExecutionCompilationFlightSync)
            {
                if (--flight.Waiters == 0)
                {
                    ExecutionCompilationFlights.Remove(cacheKey);
                    flight.Gate.Dispose();
                }
            }

            throw;
        }

        return new ExecutionCompilationFlightLease(cacheKey, flight);
    }

    private static void ReleaseExecutionCompilationFlight(
        ExecutionCompilationCacheKey cacheKey,
        ExecutionCompilationFlight flight)
    {
        flight.Gate.Release();
        lock (ExecutionCompilationFlightSync)
        {
            flight.Waiters--;
            if (flight.Waiters == 0)
            {
                ExecutionCompilationFlights.Remove(cacheKey);
                flight.Gate.Dispose();
            }
        }
    }

    private static ExecutionCompilationCachePublication PrepareExecutionCompilation(
        ExecutionCompilationCacheKey cacheKey,
        ExecutableQueryArtifact executableArtifact,
        string semanticContractFingerprint,
        string runnableTypeName,
        CanonicalExecutionArtifactContract? canonicalContract,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(executableArtifact);
        if (cacheKey.ExecutionTarget != executableArtifact.TargetId)
        {
            throw new InvalidOperationException(
                $"Execution compilation cache key targets '{cacheKey.ExecutionTarget}', but executable artifact targets '{executableArtifact.TargetId}'.");
        }

        return new ExecutionCompilationCachePublication(
            cacheKey,
            new PreparedExecutableTemplate(
                executableArtifact,
                cacheKey.ExecutionTarget,
                runnableTypeName,
                semanticContractFingerprint),
            canonicalContract);
    }

    private static void CommitExecutionCompilation(
        ExecutionCompilationCachePublication publication,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(publication);
        cancellationToken.ThrowIfCancellationRequested();
        CommitExecutionCompilationAfterCancellationCheck(publication);
    }

    private static void CommitExecutionCompilationAfterCancellationCheck(
        ExecutionCompilationCachePublication publication)
    {
        if (!publication.TryBeginCommit())
            return;

        try
        {
            lock (ExecutionCompilationCacheMutationSync)
            {
                if (ExecutionCompilationCache.TryGetValue(publication.CacheKey, out var existing))
                {
                    if (string.Equals(
                            existing.SemanticContractFingerprint,
                            publication.Template!.SemanticContractFingerprint,
                            StringComparison.Ordinal))
                    {
                        AddCanonicalExecutionAliasLocked(existing, publication.CanonicalContract);
                        publication.MarkCommitted(ownsArtifact: false);
                        return;
                    }

                    RemoveExecutionCompilationEntryLocked(existing);
                }

                EnsureExecutionCompilationCapacityLocked();
                var addedEntry = new CachedExecutionCompilation(publication.Template!);
                ExecutionCompilationEntries.Add(addedEntry);
                ExecutionCompilationCache[publication.CacheKey] = addedEntry;
                AddCanonicalExecutionAliasLocked(addedEntry, publication.CanonicalContract);
                publication.MarkCommitted(ownsArtifact: true);
            }
        }
        catch
        {
            publication.ResetPending();
            throw;
        }
    }

    private static ExecutionCompilationCachePublication PrepareCanonicalExecutionAlias(
        ExecutionCompilationCacheKey cacheKey,
        CachedExecutionCompilation cachedCompilation,
        CanonicalExecutionArtifactContract canonicalContract,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return new ExecutionCompilationCachePublication(
            cacheKey,
            cachedCompilation,
            canonicalContract);
    }

    private static void CommitCanonicalExecutionAlias(
        ExecutionCompilationCachePublication publication,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(publication);
        cancellationToken.ThrowIfCancellationRequested();
        CommitCanonicalExecutionAliasAfterCancellationCheck(publication);
    }

    private static void CommitCanonicalExecutionAliasAfterCancellationCheck(
        ExecutionCompilationCachePublication publication)
    {
        ArgumentNullException.ThrowIfNull(publication);
        if (!publication.TryBeginCommit())
            return;

        try
        {
            lock (ExecutionCompilationCacheMutationSync)
            {
                if (!ExecutionCompilationEntries.Contains(publication.CachedCompilation!))
                {
                    publication.MarkCommitted(ownsArtifact: false);
                    return;
                }

                if (ExecutionCompilationCache.TryGetValue(publication.CacheKey, out var existing) &&
                    !ReferenceEquals(existing, publication.CachedCompilation))
                    RemoveExecutionCompilationEntryLocked(existing);

                EnsureExecutionCompilationCapacityLocked();
                ExecutionCompilationCache[publication.CacheKey] = publication.CachedCompilation!;
                AddCanonicalExecutionAliasLocked(
                    publication.CachedCompilation!,
                    publication.CanonicalContract);
                publication.MarkCommitted(ownsArtifact: false);
            }
        }
        catch
        {
            publication.ResetPending();
            throw;
        }
    }

    private static IDisposable? TryAcquireExecutionCompilationReader(
        ExecutionCompilationCacheKey cacheKey,
        out CachedExecutionCompilation? cachedCompilation)
    {
        lock (ExecutionCompilationCacheMutationSync)
        {
            if (!ExecutionCompilationCache.TryGetValue(cacheKey, out var entry) ||
                !entry.TryAcquireReaderLocked())
            {
                cachedCompilation = null;
                return null;
            }

            entry.Touch();
            cachedCompilation = entry;
            return new CachedExecutionCompilationReaderLease(entry);
        }
    }

    private static IDisposable? TryAcquireCanonicalExecutionCompilationReader(
        CanonicalExecutionArtifactContract canonicalContract,
        out CachedExecutionCompilation? cachedCompilation)
    {
        lock (ExecutionCompilationCacheMutationSync)
        {
            if (!CanonicalExecutionCompilationCache.TryGetValue(canonicalContract, out var entry) ||
                !entry.TryAcquireReaderLocked())
            {
                cachedCompilation = null;
                return null;
            }

            entry.Touch();
            cachedCompilation = entry;
            return new CachedExecutionCompilationReaderLease(entry);
        }
    }

    private static void ReleaseExecutionCompilationReader(CachedExecutionCompilation entry)
    {
        lock (ExecutionCompilationCacheMutationSync)
            entry.ReleaseReaderLocked();
    }

    private static void InvokeExecutionCompilationCommitTestHook()
    {
        ExecutionCompilationCommitTestHook.Value?.Invoke();
    }

    private static ExecutableQueryArtifact CreateCachedExecutableArtifact(
        BuildItems items,
        ExecutionTargetId targetId,
        Type? runnableType,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (ContainsQueryScopedRowTransfer(items))
        {
            var executableArtifact = items.ExecutableArtifact ?? throw new InvalidOperationException(
                "Query-scoped execution caching requires the emitted executable artifact.");
            return CSharpClrArtifactCompatibility.TryGetAssemblyExecutable(
                       executableArtifact,
                       out var clrArtifact)
                ? CSharpClrArtifactCompatibility.CreateAssemblyExecutable(
                    clrArtifact.DllFile,
                    clrArtifact.PdbFile,
                    clrArtifact.RunnableTypeName)
                : executableArtifact;
        }

        return ExecutionTargetCatalog
            .ResolveActivator(targetId)
            .CreateLoadedExecutableArtifact(runnableType ?? throw new InvalidOperationException(
                "Declared-row execution caching requires a loaded runnable type."));
    }

    private static bool ContainsQueryScopedRowTransfer(BuildItems items)
    {
        return items.PlanningResult?.ExecutionArtifacts.SourceTransferPlansBySourceId?
            .Values
            .Any(static plan => plan.Mode == SourceTransferMode.QueryScopedRows) == true;
    }

    private static void EnsureExecutionCompilationCapacityLocked()
    {
        while (ExecutionCompilationEntries.Count >= Volatile.Read(ref _executionCompilationCacheLimit))
        {
            var coldest = ExecutionCompilationEntries
                .OrderBy(static entry => entry.LastAccessTick)
                .FirstOrDefault();
            if (coldest is null)
                return;

            RemoveExecutionCompilationEntryLocked(coldest);
        }
    }

    private static void RemoveExecutionCompilationEntryLocked(CachedExecutionCompilation entry)
    {
        foreach (var exact in ExecutionCompilationCache
                     .Where(pair => ReferenceEquals(pair.Value, entry))
                     .Select(static pair => pair.Key)
                     .ToArray())
            ExecutionCompilationCache.TryRemove(exact, out _);

        foreach (var canonical in CanonicalExecutionCompilationCache
                     .Where(pair => ReferenceEquals(pair.Value, entry))
                     .Select(static pair => pair.Key)
                     .ToArray())
            CanonicalExecutionCompilationCache.TryRemove(canonical, out _);

        if (ExecutionCompilationEntries.Remove(entry))
            entry.RetireLocked();
    }

    private static void AddCanonicalExecutionAliasLocked(
        CachedExecutionCompilation entry,
        CanonicalExecutionArtifactContract? canonicalContract)
    {
        if (canonicalContract is null ||
            CanonicalExecutionCompilationCache.ContainsKey(canonicalContract))
            return;

        while (CanonicalExecutionCompilationCache.Count >= CanonicalExecutionCompilationAliasLimit)
        {
            var coldestAlias = CanonicalExecutionCompilationCache
                .OrderBy(static pair => pair.Value.LastAccessTick)
                .Select(static pair => pair.Key)
                .FirstOrDefault();
            if (coldestAlias is null)
                break;

            CanonicalExecutionCompilationCache.TryRemove(coldestAlias, out _);
        }

        CanonicalExecutionCompilationCache[canonicalContract] = entry;
    }

    private static CachedExecutionCompilation? TryGetCanonicalExecutionCompilation(
        CanonicalExecutionArtifactContract canonicalContract)
    {
        return CanonicalExecutionCompilationCache.TryGetValue(canonicalContract, out var cachedCompilation)
            ? cachedCompilation
            : null;
    }

    private static IDisposable AcquireCanonicalExecutionCompilationFlight(
        CanonicalExecutionArtifactContract canonicalContract,
        CancellationToken cancellationToken,
        Action? waiterRegistered = null)
    {
        ExecutionCompilationFlight flight;
        lock (CanonicalExecutionCompilationFlightSync)
        {
            if (!CanonicalExecutionCompilationFlights.TryGetValue(canonicalContract, out flight!))
            {
                flight = new ExecutionCompilationFlight();
                CanonicalExecutionCompilationFlights.Add(canonicalContract, flight);
            }

            flight.Waiters++;
        }

        try
        {
            waiterRegistered?.Invoke();
            flight.Gate.Wait(cancellationToken);
        }
        catch
        {
            lock (CanonicalExecutionCompilationFlightSync)
            {
                if (--flight.Waiters == 0)
                {
                    CanonicalExecutionCompilationFlights.Remove(canonicalContract);
                    flight.Gate.Dispose();
                }
            }

            throw;
        }

        return new CanonicalExecutionCompilationFlightLease(canonicalContract, flight);
    }

    private static void ReleaseCanonicalExecutionCompilationFlight(
        CanonicalExecutionArtifactContract canonicalContract,
        ExecutionCompilationFlight flight)
    {
        flight.Gate.Release();
        lock (CanonicalExecutionCompilationFlightSync)
        {
            flight.Waiters--;
            if (flight.Waiters == 0)
            {
                CanonicalExecutionCompilationFlights.Remove(canonicalContract);
                flight.Gate.Dispose();
            }
        }
    }

    private static bool CanUseExecutionCompilationCache(ISchemaProvider schemaProvider)
    {
        var providerType = schemaProvider.GetType();

        return !Debugger.IsAttached && providerType.IsVisible;
    }

    private static bool CanUseExecutionCompilationCache(BuildItems items)
    {
        return !items.HasDeclaredSourceRuntimeSettings &&
               !items.HasSourceRuntimeSettingValues &&
               items.CompilationOptions.InstrumentationMode == QueryInstrumentationMode.Disabled &&
               items.InterpreterSourceCode is null;
    }

    private static bool CanUseCanonicalExecutionCompilationCache(BuildItems items)
    {
        return CanUseExecutionCompilationCache(items) &&
               !Debugger.IsAttached &&
               items.ExecutionTarget == ExecutionTargetIds.CSharpClr &&
               items.QueryResultMode == QueryResultMode.Table &&
               items.OutputType is null &&
               !items.EmitPdb;
    }

    private static ExecutionCompilationCacheKey CreateExecutionCompilationCacheKey(
        string script,
        ISchemaProvider schemaProvider,
        CompilationOptions options,
        ExecutionTargetId executionTarget,
        TargetRenderProfile renderProfile = TargetRenderProfile.ExecutionFast,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var providerType = schemaProvider.GetType();

        return new ExecutionCompilationCacheKey(
            script,
            RuntimeV2Contract.ContractSignature,
            ExecutionSemanticsContract.Version1.Fingerprint,
            executionTarget,
            providerType.AssemblyQualifiedName ?? providerType.FullName ?? providerType.Name,
            CreateProviderContractSignature(schemaProvider, cancellationToken),
            CompilationOptionsFingerprint.Compute(options),
            renderProfile,
            TargetRenderProfileContract.Version);
    }

    internal static string CreateExecutionCompilationCacheKeyTestSignature(
        string script,
        ISchemaProvider schemaProvider,
        CompilationOptions options)
    {
        return CreateExecutionCompilationCacheKey(
            script,
            schemaProvider,
            options,
            ExecutionTargetIds.CSharpClr).ToString();
    }

    internal static string CreateExecutionCompilationCacheKeyTestSignature(
        string script,
        ISchemaProvider schemaProvider,
        CompilationOptions options,
        ExecutionTargetId executionTarget)
    {
        return CreateExecutionCompilationCacheKey(
            script,
            schemaProvider,
            options,
            executionTarget).ToString();
    }

    internal static string CreateExecutionCompilationCacheKeyTestSignature(
        string script,
        ISchemaProvider schemaProvider,
        CompilationOptions options,
        ExecutionTargetId executionTarget,
        TargetRenderProfile renderProfile)
    {
        return CreateExecutionCompilationCacheKey(
            script,
            schemaProvider,
            options,
            executionTarget,
            renderProfile).ToString();
    }

    internal static string CreateExecutionCompilationCacheKeyTestSignature(
        string script,
        ISchemaProvider schemaProvider,
        CompilationOptions options,
        ExecutionTargetId executionTarget,
        TargetRenderProfile renderProfile,
        CancellationToken cancellationToken)
    {
        return CreateExecutionCompilationCacheKey(
            script,
            schemaProvider,
            options,
            executionTarget,
            renderProfile,
            cancellationToken).ToString();
    }

    internal static IDisposable AcquireExecutionCompilationFlightForTests(
        string script,
        ISchemaProvider schemaProvider,
        CompilationOptions options,
        Action waiterRegistered,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(waiterRegistered);
        return AcquireExecutionCompilationFlight(
            CreateExecutionCompilationCacheKey(
                script,
                schemaProvider,
                options,
                ExecutionTargetIds.CSharpClr,
                cancellationToken: cancellationToken),
            cancellationToken,
            waiterRegistered);
    }

    internal static IDisposable AcquireCanonicalExecutionCompilationFlightForTests(
        CanonicalExecutionArtifactContract canonicalContract,
        Action waiterRegistered,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(canonicalContract);
        ArgumentNullException.ThrowIfNull(waiterRegistered);
        return AcquireCanonicalExecutionCompilationFlight(
            canonicalContract,
            cancellationToken,
            waiterRegistered);
    }

    internal static int GetCanonicalExecutionEntryIdentityForTests(
        BuildItems items,
        ISchemaProvider schemaProvider)
    {
        if (!CanUseCanonicalExecutionCompilationCache(items))
            return 0;

        var contract = CreateCanonicalExecutionArtifactContract(
            items,
            schemaProvider,
            items.CompilationOptions,
            CancellationToken.None);
        return TryGetCanonicalExecutionCompilation(contract) is { } entry
            ? RuntimeHelpers.GetHashCode(entry)
            : 0;
    }

    internal static CanonicalExecutionArtifactContract CreateCanonicalExecutionContractForTests(
        BuildItems items,
        ISchemaProvider schemaProvider)
    {
        return CreateCanonicalExecutionArtifactContract(
            items,
            schemaProvider,
            items.CompilationOptions,
            CancellationToken.None);
    }

    internal static CanonicalExecutionArtifactContract CreateCanonicalExecutionContractForTests(
        BuildItems items,
        ISchemaProvider schemaProvider,
        CancellationToken cancellationToken)
    {
        return CreateCanonicalExecutionArtifactContract(
            items,
            schemaProvider,
            items.CompilationOptions,
            cancellationToken);
    }

    internal static IDisposable SetExecutionCompilationCacheLimitForTests(int limit)
    {
        if (limit <= 0)
            throw new ArgumentOutOfRangeException(nameof(limit));

        int previousLimit;
        lock (ExecutionCompilationCacheMutationSync)
        {
            previousLimit = _executionCompilationCacheLimit;
            _executionCompilationCacheLimit = limit;
            EnsureExecutionCompilationCapacityLocked();
        }

        return new DelegateDisposable(() =>
        {
            lock (ExecutionCompilationCacheMutationSync)
                _executionCompilationCacheLimit = previousLimit;
        });
    }

    internal static IDisposable SetExecutionCompilationCommitHookForTests(Action callback)
    {
        ArgumentNullException.ThrowIfNull(callback);
        var previous = ExecutionCompilationCommitTestHook.Value;
        ExecutionCompilationCommitTestHook.Value = callback;
        return new DelegateDisposable(() => ExecutionCompilationCommitTestHook.Value = previous);
    }

    internal static bool HasExecutionCompilationCacheEntryForTests(
        string script,
        ISchemaProvider schemaProvider,
        CompilationOptions options)
    {
        var key = CreateExecutionCompilationCacheKey(
            script,
            schemaProvider,
            options,
            ExecutionTargetIds.CSharpClr);
        lock (ExecutionCompilationCacheMutationSync)
            return ExecutionCompilationCache.ContainsKey(key);
    }

    internal static IDisposable? AcquireExecutionCompilationReaderForTests(
        string script,
        ISchemaProvider schemaProvider,
        CompilationOptions options)
    {
        var key = CreateExecutionCompilationCacheKey(
            script,
            schemaProvider,
            options,
            ExecutionTargetIds.CSharpClr);
        return TryAcquireExecutionCompilationReader(key, out _);
    }

    internal static void SeedExecutionCompilationCacheForTests(
        string script,
        ISchemaProvider schemaProvider,
        CompilationOptions options,
        ExecutableQueryArtifact artifact,
        string semanticContractFingerprint)
    {
        ArgumentNullException.ThrowIfNull(artifact);
        var key = CreateExecutionCompilationCacheKey(
            script,
            schemaProvider,
            options,
            ExecutionTargetIds.CSharpClr);
        var publication = PrepareExecutionCompilation(
            key,
            artifact,
            semanticContractFingerprint,
            "Musoq.Converter.Tests.CacheRunnable",
            canonicalContract: null,
            CancellationToken.None);
        try
        {
            CommitExecutionCompilationAfterCancellationCheck(publication);
        }
        finally
        {
            publication.Dispose();
        }
    }

    internal static void ClearExecutionCompilationCacheForTests()
    {
        lock (ExecutionCompilationCacheMutationSync)
        {
            foreach (var entry in ExecutionCompilationEntries.ToArray())
                RemoveExecutionCompilationEntryLocked(entry);

            ExecutionCompilationCache.Clear();
            CanonicalExecutionCompilationCache.Clear();
            _executionCompilationCacheLimit = DefaultExecutionCompilationCacheLimit;
        }
    }

    private static string CreateProviderSignature(ISchemaProvider schemaProvider)
    {
        return CreateProviderSignature(schemaProvider, CancellationToken.None);
    }

    internal static string CreateProviderSignatureForTests(
        ISchemaProvider schemaProvider,
        CancellationToken cancellationToken)
    {
        return CreateProviderSignature(schemaProvider, cancellationToken);
    }

    private static string CreateProviderSignature(
        ISchemaProvider schemaProvider,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var builder = new StringBuilder();
        var fields = GetOrderedInstanceFields(schemaProvider.GetType(), cancellationToken);

        foreach (var field in fields)
        {
            cancellationToken.ThrowIfCancellationRequested();
            builder
                .Append(field.DeclaringType?.FullName)
                .Append('.')
                .Append(field.Name)
                .Append('=');

            AppendSignatureValue(builder, field.GetValue(schemaProvider), 0, cancellationToken);
            builder.Append(';');
        }

        cancellationToken.ThrowIfCancellationRequested();
        return builder.ToString();
    }

    private static IEnumerable<FieldInfo> GetInstanceFields(Type type)
    {
        return GetInstanceFields(type, CancellationToken.None);
    }

    private static IEnumerable<FieldInfo> GetInstanceFields(
        Type type,
        CancellationToken cancellationToken)
    {
        for (var current = type; current != null; current = current.BaseType)
        {
            cancellationToken.ThrowIfCancellationRequested();
            foreach (var field in current.GetFields(
                         BindingFlags.Instance |
                         BindingFlags.Public |
                         BindingFlags.NonPublic |
                         BindingFlags.DeclaredOnly))
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return field;
            }
        }
    }

    private static IReadOnlyList<FieldInfo> GetOrderedInstanceFields(
        Type type,
        CancellationToken cancellationToken,
        bool useAssemblyQualifiedDeclaringType = false)
    {
        var fields = GetInstanceFields(type, cancellationToken).ToList();
        cancellationToken.ThrowIfCancellationRequested();
        fields.Sort((left, right) =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            var declaringTypeComparison = StringComparer.Ordinal.Compare(
                useAssemblyQualifiedDeclaringType
                    ? left.DeclaringType?.AssemblyQualifiedName
                    : left.DeclaringType?.FullName,
                useAssemblyQualifiedDeclaringType
                    ? right.DeclaringType?.AssemblyQualifiedName
                    : right.DeclaringType?.FullName);
            return declaringTypeComparison != 0
                ? declaringTypeComparison
                : StringComparer.Ordinal.Compare(left.Name, right.Name);
        });
        cancellationToken.ThrowIfCancellationRequested();
        return fields;
    }

    private static string CreateSignatureFragment(object? value, int depth)
    {
        return CreateSignatureFragment(value, depth, CancellationToken.None);
    }

    private static string CreateSignatureFragment(
        object? value,
        int depth,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var builder = new StringBuilder();
        AppendSignatureValue(builder, value, depth, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        return builder.ToString();
    }

    private static void AppendSignatureValue(StringBuilder builder, object? value, int depth)
    {
        AppendSignatureValue(builder, value, depth, CancellationToken.None);
    }

    private static void AppendSignatureValue(
        StringBuilder builder,
        object? value,
        int depth,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (value is null)
        {
            builder.Append("<null>");
            return;
        }

        if (depth > 2)
        {
            AppendIdentity(builder, value);
            return;
        }

        switch (value)
        {
            case string text:
                builder.Append('"').Append(text).Append('"');
                return;
            case Type type:
                builder.Append("type:").Append(type.AssemblyQualifiedName ?? type.FullName ?? type.Name);
                return;
            case Enum enumValue:
                builder
                    .Append(enumValue.GetType().FullName)
                    .Append(':')
                    .Append(enumValue);
                return;
            case bool boolean:
                builder.Append(boolean ? "true" : "false");
                return;
            case IFormattable formattable:
                builder.Append(formattable.ToString(null, CultureInfo.InvariantCulture));
                return;
            case IDictionary dictionary:
                AppendDictionarySignature(builder, dictionary, depth, cancellationToken);
                return;
            default:
                AppendIdentity(builder, value);
                return;
        }
    }

    private static void AppendDictionarySignature(
        StringBuilder builder,
        IDictionary dictionary,
        int depth,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var entries = new List<(string Key, string Value)>(dictionary.Count);
        foreach (DictionaryEntry entry in dictionary)
        {
            cancellationToken.ThrowIfCancellationRequested();
            entries.Add((
                CreateSignatureFragment(entry.Key, depth + 1, cancellationToken),
                CreateSignatureFragment(entry.Value, depth + 1, cancellationToken)));
        }

        entries.Sort((left, right) =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            var keyCompare = string.CompareOrdinal(left.Key, right.Key);
            return keyCompare != 0
                ? keyCompare
                : string.CompareOrdinal(left.Value, right.Value);
        });

        cancellationToken.ThrowIfCancellationRequested();
        builder.Append("dict[").Append(entries.Count).Append("]{");
        foreach (var entry in entries)
        {
            cancellationToken.ThrowIfCancellationRequested();
            builder
                .Append(entry.Key)
                .Append("=>")
                .Append(entry.Value)
                .Append('|');
        }

        builder.Append('}');
        cancellationToken.ThrowIfCancellationRequested();
    }

    private static void AppendIdentity(StringBuilder builder, object value)
    {
        builder
            .Append(value.GetType().AssemblyQualifiedName ?? value.GetType().FullName ?? value.GetType().Name)
            .Append('#')
            .Append(RuntimeHelpers.GetHashCode(value));
    }

    private readonly record struct ExecutionCompilationCacheKey(
        string Script,
        string RuntimeV2ContractSignature,
        string ExecutionSemanticsFingerprint,
        ExecutionTargetId ExecutionTarget,
        string ProviderType,
        string ProviderContractBucket,
        string CompilationOptionsFingerprint,
        TargetRenderProfile RenderProfile,
        int RenderProfileVersion);

    private sealed class ExecutionCompilationFlight
    {
        public SemaphoreSlim Gate { get; } = new(1, 1);

        public int Waiters { get; set; }
    }

    private sealed class ExecutionCompilationFlightLease(
        ExecutionCompilationCacheKey cacheKey,
        ExecutionCompilationFlight flight) : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            ReleaseExecutionCompilationFlight(cacheKey, flight);
        }
    }

    private sealed class CanonicalExecutionCompilationFlightLease(
        CanonicalExecutionArtifactContract canonicalContract,
        ExecutionCompilationFlight flight) : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            ReleaseCanonicalExecutionCompilationFlight(canonicalContract, flight);
        }
    }

    private sealed class CachedExecutionCompilation
    {
        private long _lastAccessTick;
        private int _activeReaders;
        private int _retired;
        private int _artifactDisposed;

        public CachedExecutionCompilation(
            PreparedExecutableTemplate template)
        {
            Template = template ?? throw new ArgumentNullException(nameof(template));
            SemanticContractFingerprint = template.SemanticContractFingerprint ?? string.Empty;
            TargetId = template.TargetId;
            Touch();
        }

        public ExecutionTargetId TargetId { get; }

        public PreparedExecutableTemplate Template { get; }

        public string SemanticContractFingerprint { get; }

        public long LastAccessTick => Volatile.Read(ref _lastAccessTick);

        public void Touch() => Volatile.Write(ref _lastAccessTick, NextExecutionCompilationAccessTick());

        public bool TryAcquireReaderLocked()
        {
            if (Volatile.Read(ref _retired) != 0)
                return false;

            _activeReaders++;
            return true;
        }

        public void ReleaseReaderLocked()
        {
            if (_activeReaders == 0)
                return;

            _activeReaders--;
            DisposeArtifactIfReadyLocked();
        }

        public void RetireLocked()
        {
            if (Interlocked.Exchange(ref _retired, 1) == 0)
                DisposeArtifactIfReadyLocked();
        }

        private void DisposeArtifactIfReadyLocked()
        {
            if (Volatile.Read(ref _retired) == 0 ||
                _activeReaders != 0 ||
                Interlocked.Exchange(ref _artifactDisposed, 1) != 0)
                return;

            (Template.ExecutableArtifact as IDisposable)?.Dispose();
        }
    }

    private sealed class CachedExecutionCompilationReaderLease(
        CachedExecutionCompilation entry) : IDisposable
    {
        private int _disposed;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
                ReleaseExecutionCompilationReader(entry);
        }
    }

    private sealed class DelegateDisposable(Action dispose) : IDisposable
    {
        private Action? _dispose = dispose ?? throw new ArgumentNullException(nameof(dispose));

        public void Dispose()
        {
            Interlocked.Exchange(ref _dispose, null)?.Invoke();
        }
    }

    private sealed class ExecutionCompilationCachePublication : IDisposable
    {
        private const int Pending = 0;
        private const int Committing = 1;
        private const int Committed = 2;
        private const int Disposed = 3;
        private int _state = Pending;
        private bool _ownsArtifact;

        public ExecutionCompilationCachePublication(
            ExecutionCompilationCacheKey cacheKey,
            PreparedExecutableTemplate template,
            CanonicalExecutionArtifactContract? canonicalContract)
        {
            CacheKey = cacheKey;
            Template = template ?? throw new ArgumentNullException(nameof(template));
            CanonicalContract = canonicalContract;
        }

        public ExecutionCompilationCachePublication(
            ExecutionCompilationCacheKey cacheKey,
            CachedExecutionCompilation cachedCompilation,
            CanonicalExecutionArtifactContract canonicalContract)
        {
            CacheKey = cacheKey;
            CachedCompilation = cachedCompilation ?? throw new ArgumentNullException(nameof(cachedCompilation));
            CanonicalContract = canonicalContract;
        }

        public ExecutionCompilationCacheKey CacheKey { get; }

        public PreparedExecutableTemplate? Template { get; }

        public CachedExecutionCompilation? CachedCompilation { get; }

        public CanonicalExecutionArtifactContract? CanonicalContract { get; }

        public bool TryBeginCommit()
        {
            return Interlocked.CompareExchange(ref _state, Committing, Pending) == Pending;
        }

        public void MarkCommitted(bool ownsArtifact)
        {
            _ownsArtifact = ownsArtifact;
            Interlocked.CompareExchange(ref _state, Committed, Committing);
        }

        public void ResetPending()
        {
            Interlocked.CompareExchange(ref _state, Pending, Committing);
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _state, Disposed) == Disposed || _ownsArtifact)
                return;

            (Template?.ExecutableArtifact as IDisposable)?.Dispose();
        }
    }
}
