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
using Musoq.Parser.Diagnostics;
using Musoq.Schema;
using Musoq.Schema.Optimization;

namespace Musoq.Converter;

public static partial class InstanceCreator
{
    private const int ExecutionCompilationCacheLimit = 512;

    private static readonly ConcurrentDictionary<ExecutionCompilationCacheKey, CachedExecutionCompilation>
        ExecutionCompilationCache = new();

    private static long _executionCompilationAccessTick;

    private static long NextExecutionCompilationAccessTick()
    {
        return Interlocked.Increment(ref _executionCompilationAccessTick);
    }

    private static void StoreExecutionCompilation(
        ExecutionCompilationCacheKey cacheKey,
        Type runnableType)
    {
        EvictColdestExecutionCompilations();
        ExecutionCompilationCache.TryAdd(cacheKey, new CachedExecutionCompilation(runnableType));
    }

    private static void EvictColdestExecutionCompilations()
    {
        while (ExecutionCompilationCache.Count >= ExecutionCompilationCacheLimit)
        {
            var coldestKey = default(ExecutionCompilationCacheKey);
            var coldestTick = long.MaxValue;
            var foundColdest = false;

            foreach (var entry in ExecutionCompilationCache)
            {
                var tick = entry.Value.LastAccessTick;
                if (foundColdest && tick >= coldestTick)
                    continue;

                coldestKey = entry.Key;
                coldestTick = tick;
                foundColdest = true;
            }

            if (!foundColdest)
                return;

            ExecutionCompilationCache.TryRemove(coldestKey, out _);
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
               !items.HasSourceRuntimeSettingValues;
    }

    private static ExecutionCompilationCacheKey CreateExecutionCompilationCacheKey(
        string script,
        ISchemaProvider schemaProvider,
        CompilationOptions options)
    {
        var providerType = schemaProvider.GetType();

        return new ExecutionCompilationCacheKey(
            script,
            RuntimeV2Contract.ContractSignature,
            providerType.AssemblyQualifiedName ?? providerType.FullName ?? providerType.Name,
            CreateProviderSignature(schemaProvider),
            options.ParallelizationMode,
            options.UseHashJoin,
            options.UseSortMergeJoin,
            options.UseCommonSubexpressionElimination,
            options.UseConstantFolding,
            options.UsePrimitiveTypeValidation,
            options.UseCteParallelization,
            options.UseCteSidecarIndexes,
            options.MaxDegreeOfParallelismOverride,
            options.InstrumentationMode,
            options.ForceTableResultMaterialization);
    }

    internal static string CreateExecutionCompilationCacheKeyTestSignature(
        string script,
        ISchemaProvider schemaProvider,
        CompilationOptions options)
    {
        return CreateExecutionCompilationCacheKey(script, schemaProvider, options).ToString();
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
        string ProviderType,
        string ProviderSignature,
        ParallelizationMode ParallelizationMode,
        bool UseHashJoin,
        bool UseSortMergeJoin,
        bool UseCommonSubexpressionElimination,
        bool UseConstantFolding,
        bool UsePrimitiveTypeValidation,
        bool UseCteParallelization,
        bool UseCteSidecarIndexes,
        int? MaxDegreeOfParallelismOverride,
        QueryInstrumentationMode InstrumentationMode,
        bool ForceTableResultMaterialization);

    private sealed class CachedExecutionCompilation
    {
        private long _lastAccessTick;

        public CachedExecutionCompilation(Type runnableType)
        {
            RunnableType = runnableType;
            Touch();
        }

        public Type RunnableType { get; }

        public long LastAccessTick => Volatile.Read(ref _lastAccessTick);

        public void Touch() => Volatile.Write(ref _lastAccessTick, NextExecutionCompilationAccessTick());
    }
}
