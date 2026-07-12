using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Musoq.Schema;
using Musoq.Schema.Optimization;

namespace Musoq.Targets.Execution;

internal sealed record QueryRuntimeBinding
{
    public QueryRuntimeBinding(
        ISchemaProvider schemaProvider,
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>>? sourceRuntimeSettingsBySourceContextId,
        IReadOnlyDictionary<string, IReadOnlyList<SourceRuntimeSettingDescription>>? sourceRuntimeSettingDescriptionsBySourceContextId,
        IReadOnlyDictionary<string, SourceExecutionPlan>? sourceExecutionPlans)
    {
        ArgumentNullException.ThrowIfNull(schemaProvider);

        SchemaProvider = schemaProvider;
        SourceRuntimeSettingsBySourceContextId = FreezeNestedStringDictionary(sourceRuntimeSettingsBySourceContextId);
        SourceRuntimeSettingDescriptionsBySourceContextId = FreezeNestedList(sourceRuntimeSettingDescriptionsBySourceContextId);
        SourceExecutionPlans = FreezeDictionary(sourceExecutionPlans);
    }

    public ISchemaProvider SchemaProvider { get; }

    public IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> SourceRuntimeSettingsBySourceContextId { get; }

    public IReadOnlyDictionary<string, IReadOnlyList<SourceRuntimeSettingDescription>> SourceRuntimeSettingDescriptionsBySourceContextId { get; }

    public IReadOnlyDictionary<string, SourceExecutionPlan> SourceExecutionPlans { get; }

    private static IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> FreezeNestedStringDictionary(
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>>? values)
    {
        return new ReadOnlyDictionary<string, IReadOnlyDictionary<string, string>>(
            values is null
                ? new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.Ordinal)
                : values.ToDictionary(
                    static item => item.Key,
                    static item => (IReadOnlyDictionary<string, string>)new ReadOnlyDictionary<string, string>(
                        new Dictionary<string, string>(item.Value, StringComparer.Ordinal)),
                    StringComparer.Ordinal));
    }

    private static IReadOnlyDictionary<string, IReadOnlyList<T>> FreezeNestedList<T>(
        IReadOnlyDictionary<string, IReadOnlyList<T>>? values)
    {
        return new ReadOnlyDictionary<string, IReadOnlyList<T>>(
            values is null
                ? new Dictionary<string, IReadOnlyList<T>>(StringComparer.Ordinal)
                : values.ToDictionary(
                    static item => item.Key,
                    static item => (IReadOnlyList<T>)Array.AsReadOnly(item.Value.ToArray()),
                    StringComparer.Ordinal));
    }

    private static IReadOnlyDictionary<string, T> FreezeDictionary<T>(
        IReadOnlyDictionary<string, T>? values)
    {
        return new ReadOnlyDictionary<string, T>(
            values is null
                ? new Dictionary<string, T>(StringComparer.Ordinal)
                : new Dictionary<string, T>(values, StringComparer.Ordinal));
    }
}
