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
using Musoq.Parser.Diagnostics;
using Musoq.Schema;
using Musoq.Schema.Optimization;
using Musoq.Targets.Execution;

namespace Musoq.Converter;

public static partial class InstanceCreator
{
    private const int ExecutionCompilationCacheLimit = 512;
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

    private static long _executionCompilationAccessTick;

    private static long NextExecutionCompilationAccessTick()
    {
        return Interlocked.Increment(ref _executionCompilationAccessTick);
    }

    private static IDisposable AcquireExecutionCompilationFlight(ExecutionCompilationCacheKey cacheKey)
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

        Monitor.Enter(flight.Gate);
        return new ExecutionCompilationFlightLease(cacheKey, flight);
    }

    private static void ReleaseExecutionCompilationFlight(
        ExecutionCompilationCacheKey cacheKey,
        ExecutionCompilationFlight flight)
    {
        Monitor.Exit(flight.Gate);
        lock (ExecutionCompilationFlightSync)
        {
            flight.Waiters--;
            if (flight.Waiters == 0)
                ExecutionCompilationFlights.Remove(cacheKey);
        }
    }

    private static void StoreExecutionCompilation(
        ExecutionCompilationCacheKey cacheKey,
        ExecutableQueryArtifact executableArtifact,
        string semanticContractFingerprint,
        string runnableTypeName,
        CanonicalExecutionArtifactContract? canonicalContract)
    {
        ArgumentNullException.ThrowIfNull(executableArtifact);
        if (cacheKey.ExecutionTarget != executableArtifact.TargetId)
        {
            throw new InvalidOperationException(
                $"Execution compilation cache key targets '{cacheKey.ExecutionTarget}', but executable artifact targets '{executableArtifact.TargetId}'.");
        }

        var template = new PreparedExecutableTemplate(
            executableArtifact,
            cacheKey.ExecutionTarget,
            runnableTypeName,
            semanticContractFingerprint);

        lock (ExecutionCompilationCacheMutationSync)
        {
            if (ExecutionCompilationCache.TryGetValue(cacheKey, out var existing))
            {
                AddCanonicalExecutionAliasLocked(existing, canonicalContract);
                return;
            }

            EnsureExecutionCompilationCapacityLocked();
            var entry = new CachedExecutionCompilation(template);
            ExecutionCompilationEntries.Add(entry);
            ExecutionCompilationCache[cacheKey] = entry;
            AddCanonicalExecutionAliasLocked(entry, canonicalContract);
        }
    }

    private static void StoreCanonicalExecutionAlias(
        ExecutionCompilationCacheKey cacheKey,
        CachedExecutionCompilation cachedCompilation,
        CanonicalExecutionArtifactContract canonicalContract)
    {
        lock (ExecutionCompilationCacheMutationSync)
        {
            if (!ExecutionCompilationEntries.Contains(cachedCompilation))
                return;

            if (ExecutionCompilationCache.TryGetValue(cacheKey, out var existing))
            {
                AddCanonicalExecutionAliasLocked(existing, canonicalContract);
                return;
            }

            EnsureExecutionCompilationCapacityLocked();
            ExecutionCompilationCache[cacheKey] = cachedCompilation;
            AddCanonicalExecutionAliasLocked(cachedCompilation, canonicalContract);
        }
    }

    private static ExecutableQueryArtifact CreateCachedExecutableArtifact(
        ExecutionTargetId targetId,
        Type runnableType)
    {
        return ExecutionTargetCatalog
            .ResolveActivator(targetId)
            .CreateLoadedExecutableArtifact(runnableType);
    }

    private static void EnsureExecutionCompilationCapacityLocked()
    {
        while (ExecutionCompilationEntries.Count >= ExecutionCompilationCacheLimit)
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

        ExecutionCompilationEntries.Remove(entry);
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
        CanonicalExecutionArtifactContract canonicalContract)
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

        Monitor.Enter(flight.Gate);
        return new CanonicalExecutionCompilationFlightLease(canonicalContract, flight);
    }

    private static void ReleaseCanonicalExecutionCompilationFlight(
        CanonicalExecutionArtifactContract canonicalContract,
        ExecutionCompilationFlight flight)
    {
        Monitor.Exit(flight.Gate);
        lock (CanonicalExecutionCompilationFlightSync)
        {
            flight.Waiters--;
            if (flight.Waiters == 0)
                CanonicalExecutionCompilationFlights.Remove(canonicalContract);
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
        TargetRenderProfile renderProfile = TargetRenderProfile.ExecutionFast)
    {
        var providerType = schemaProvider.GetType();

        return new ExecutionCompilationCacheKey(
            script,
            RuntimeV2Contract.ContractSignature,
            ExecutionSemanticsContract.Version1.Fingerprint,
            executionTarget,
            providerType.AssemblyQualifiedName ?? providerType.FullName ?? providerType.Name,
            CreateProviderContractSignature(schemaProvider),
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

    internal static int GetCanonicalExecutionEntryIdentityForTests(
        BuildItems items,
        ISchemaProvider schemaProvider)
    {
        if (!CanUseCanonicalExecutionCompilationCache(items))
            return 0;

        var contract = CreateCanonicalExecutionArtifactContract(
            items,
            schemaProvider,
            items.CompilationOptions);
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
            items.CompilationOptions);
    }

    private static string CreateProviderSignature(ISchemaProvider schemaProvider)
    {
        var builder = new StringBuilder();
        var fields = GetInstanceFields(schemaProvider.GetType())
            .OrderBy(field => field.DeclaringType?.FullName, StringComparer.Ordinal)
            .ThenBy(field => field.Name, StringComparer.Ordinal);

        foreach (var field in fields)
        {
            builder
                .Append(field.DeclaringType?.FullName)
                .Append('.')
                .Append(field.Name)
                .Append('=');

            AppendSignatureValue(builder, field.GetValue(schemaProvider), 0);
            builder.Append(';');
        }

        return builder.ToString();
    }

    private static IEnumerable<FieldInfo> GetInstanceFields(Type type)
    {
        for (var current = type; current != null; current = current.BaseType)
        {
            foreach (var field in current.GetFields(
                         BindingFlags.Instance |
                         BindingFlags.Public |
                         BindingFlags.NonPublic |
                         BindingFlags.DeclaredOnly))
            {
                yield return field;
            }
        }
    }

    private static string CreateSignatureFragment(object? value, int depth)
    {
        var builder = new StringBuilder();
        AppendSignatureValue(builder, value, depth);
        return builder.ToString();
    }

    private static void AppendSignatureValue(StringBuilder builder, object? value, int depth)
    {
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
                AppendDictionarySignature(builder, dictionary, depth);
                return;
            default:
                AppendIdentity(builder, value);
                return;
        }
    }

    private static void AppendDictionarySignature(StringBuilder builder, IDictionary dictionary, int depth)
    {
        var entries = new List<(string Key, string Value)>(dictionary.Count);
        foreach (DictionaryEntry entry in dictionary)
        {
            entries.Add((
                CreateSignatureFragment(entry.Key, depth + 1),
                CreateSignatureFragment(entry.Value, depth + 1)));
        }

        entries.Sort(static (left, right) =>
        {
            var keyCompare = string.CompareOrdinal(left.Key, right.Key);
            return keyCompare != 0
                ? keyCompare
                : string.CompareOrdinal(left.Value, right.Value);
        });

        builder.Append("dict[").Append(entries.Count).Append("]{");
        foreach (var entry in entries)
        {
            builder
                .Append(entry.Key)
                .Append("=>")
                .Append(entry.Value)
                .Append('|');
        }

        builder.Append('}');
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
        public object Gate { get; } = new();

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
    }
}
