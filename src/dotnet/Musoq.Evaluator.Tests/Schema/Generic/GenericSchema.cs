using System;
using System.Collections.Concurrent;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Musoq.Plugins;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;

namespace Musoq.Evaluator.Tests.Schema.Generic;

public class GenericSchema<TLibrary>(
    IReadOnlyDictionary<string, (ISchemaTable SchemaTable, object RowSource)> tables,
    IReadOnlyDictionary<string, Func<object?[], RowSourceFilterInput, object?>?>? filterRowsSource = null)
    : SchemaBase("test", GetOrCreateLibrary()) where TLibrary : LibraryBase, new()
{
    private static readonly ConcurrentDictionary<Type, MethodsAggregator> LibraryCache = new();

    public override ISchemaTable GetTableByName(string name, SourceMetadataContext metadataContext, params object?[] parameters)
    {
        if (tables.TryGetValue(name, out var table))
            return table.SchemaTable;

        throw new NotSupportedException($"Table {name} is not supported.");
    }

    public override RowSource<T> GetRowSource<T>(string name, SourceExecutionContext executionContext, params object?[] parameters)
    {
        if (!tables.TryGetValue(name, out var table))
            throw new NotSupportedException($"Table {name} is not supported.");

        if (filterRowsSource == null)
            return EnsureSourceType<T>(name, table.RowSource);

        if (filterRowsSource.TryGetValue(name, out var filter))
            return CreateSourceFromFilterResult<T>(
                name,
                filter?.Invoke(parameters, new RowSourceFilterInput(table.RowSource)) ?? table.RowSource);

        return EnsureSourceType<T>(name, table.RowSource);
    }

    private static RowSource<T> CreateSourceFromFilterResult<T>(string name, object source)
    {
        if (source is RowSource<T> typedSource)
            return typedSource;

        if (source is IEnumerable<IReadOnlyList<T>> chunks)
            return new EntitySource<T>(chunks, new Dictionary<string, int>(), new Dictionary<int, Func<T, object?>>());

        if (source is IEnumerable enumerable and not string)
            return new EntitySource<T>(CastChunks<T>(enumerable), new Dictionary<string, int>(), new Dictionary<int, Func<T, object?>>());

        return EnsureSourceType<T>(name, source);
    }

    private static IEnumerable<IReadOnlyList<T>> CastChunks<T>(IEnumerable chunks)
    {
        foreach (var chunk in chunks)
        {
            if (chunk is IReadOnlyList<T> typedChunk)
            {
                yield return typedChunk;
                continue;
            }

            if (chunk is IEnumerable<T> typedEnumerableChunk)
            {
                yield return typedEnumerableChunk.ToArray();
                continue;
            }

            if (chunk is IEnumerable enumerableChunk and not string)
            {
                yield return enumerableChunk.Cast<T>().ToArray();
                continue;
            }

            throw new InvalidOperationException(
                $"Filter returned '{chunk?.GetType().FullName ?? "<null>"}' instead of a chunk compatible with '{typeof(T).FullName}'.");
        }
    }

    private static MethodsAggregator GetOrCreateLibrary()
    {
        return LibraryCache.GetOrAdd(typeof(TLibrary), static _ => CreateLibrary());
    }

    private static MethodsAggregator CreateLibrary()
    {
        var methodManager = new MethodsManager();

        var lib = new TLibrary();

        methodManager.RegisterLibraries(lib);

        return new MethodsAggregator(methodManager);
    }
}

public sealed class RowSourceFilterInput(object rowSource)
{
    public IEnumerable<IReadOnlyList<dynamic>> Chunks => GetChunks(rowSource);

    public IEnumerable<IReadOnlyList<dynamic>> Filter(Func<dynamic, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        foreach (var chunk in Chunks)
        {
            var filtered = new List<dynamic>(chunk.Count);
            for (var index = 0; index < chunk.Count; index++)
            {
                var row = chunk[index];
                if (predicate(row))
                    filtered.Add(row);
            }

            if (filtered.Count > 0)
                yield return filtered;
        }
    }

    private static IEnumerable<IReadOnlyList<dynamic>> GetChunks(object rowSource)
    {
        var chunksProperty = rowSource.GetType().GetProperty(nameof(RowSource<object>.Chunks));
        if (chunksProperty?.GetValue(rowSource) is not IEnumerable chunks)
            yield break;

        foreach (var chunk in chunks)
        {
            if (chunk is not IEnumerable rows)
                continue;

            var typedChunk = new List<dynamic>();
            foreach (var row in rows)
                typedChunk.Add(row);

            if (typedChunk.Count > 0)
                yield return typedChunk;
        }
    }
}
