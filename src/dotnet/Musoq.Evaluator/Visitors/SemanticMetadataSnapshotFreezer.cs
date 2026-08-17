using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

internal static class SemanticMetadataSnapshotFreezer
{
    public static SemanticResultShapeSnapshot BuildResultShape(SemanticResultShapeSnapshotInput input)
    {
        return new SemanticResultShapeSnapshot
        {
            GeneratedAliases = Array.AsReadOnly(input.GeneratedAliases.ToArray()),
            GeneratedColumns = new ReadOnlyDictionary<string, IReadOnlyList<FieldNode>>(
                input.GeneratedColumns.ToDictionary(
                    pair => pair.Key,
                    pair => (IReadOnlyList<FieldNode>)Array.AsReadOnly(pair.Value.ToArray()),
                    StringComparer.Ordinal)),
            SelectFieldAliases = new ReadOnlyDictionary<string, Node>(
                input.SelectFieldAliases.ToDictionary(
                    pair => pair.Key,
                    pair => pair.Value,
                    StringComparer.OrdinalIgnoreCase)),
            TheMostInnerIdentifier = input.TheMostInnerIdentifier
        };
    }

    public static IReadOnlyDictionary<TKey, TValue> FreezeDictionary<TKey, TValue>(
        IReadOnlyDictionary<TKey, TValue> values)
        where TKey : notnull
    {
        return new ReadOnlyDictionary<TKey, TValue>(values.ToDictionary(pair => pair.Key, pair => pair.Value));
    }

    public static IReadOnlyList<T> FreezeList<T>(IEnumerable<T> values)
    {
        return Array.AsReadOnly(values.ToArray());
    }

    public static IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> FreezeRuntimeSettings(
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> values)
    {
        var copied = values.ToDictionary(
            pair => pair.Key,
            pair => (IReadOnlyDictionary<string, string>)new ReadOnlyDictionary<string, string>(
                pair.Value.ToDictionary(setting => setting.Key, setting => setting.Value)),
            StringComparer.Ordinal);

        return new ReadOnlyDictionary<string, IReadOnlyDictionary<string, string>>(copied);
    }

    public static IReadOnlyDictionary<string, IReadOnlyList<SourceRuntimeSettingDescription>>
        FreezeRuntimeSettingDescriptions(
            IReadOnlyDictionary<string, IReadOnlyList<SourceRuntimeSettingDescription>> values)
    {
        var copied = values.ToDictionary(
            pair => pair.Key,
            pair => (IReadOnlyList<SourceRuntimeSettingDescription>)Array.AsReadOnly(pair.Value.ToArray()),
            StringComparer.Ordinal);

        return new ReadOnlyDictionary<string, IReadOnlyList<SourceRuntimeSettingDescription>>(copied);
    }

    public static IReadOnlyDictionary<string, IReadOnlyList<T>> FreezeArrays<T>(
        IEnumerable<KeyValuePair<string, T[]>> values)
    {
        var copied = values.ToDictionary(
            pair => pair.Key,
            pair => (IReadOnlyList<T>)Array.AsReadOnly(pair.Value.ToArray()),
            StringComparer.Ordinal);

        return new ReadOnlyDictionary<string, IReadOnlyList<T>>(copied);
    }
}
