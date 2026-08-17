using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Musoq.Evaluator.Runtime;
using Musoq.Evaluator.Tables;
using Musoq.Plugins;
using Musoq.Schema;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Helpers;

public static partial class EvaluationHelper
{
    private static readonly WeakTypeRuntimeCache<Func<object, IReadOnlyList<object>>> ObjectChunkAdapters =
        new(RuntimeCacheOptions.ObjectChunkAdapterCacheSize);

    internal static void ClearObjectChunkAdapterCache() => ObjectChunkAdapters.Clear();

    public static Type GetRequiredType(string assemblyQualifiedName)
    {
        if (string.IsNullOrWhiteSpace(assemblyQualifiedName))
            throw new ArgumentException(@"Type name must be provided.", nameof(assemblyQualifiedName));

        return Type.GetType(assemblyQualifiedName, throwOnError: false)
               ?? throw new InvalidOperationException($"Type '{assemblyQualifiedName}' could not be resolved.");
    }

    public static IEnumerable<IReadOnlyList<object>> GetRowSourceChunks(
        ISchema schema,
        Type entityType,
        string name,
        SourceExecutionContext executionContext,
        object?[] parameters)
    {
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(entityType);
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(executionContext);
        ArgumentNullException.ThrowIfNull(parameters);
        var method = typeof(ISchema)
            .GetMethods()
            .Single(candidate => candidate is { Name: nameof(ISchema.GetRowSource), IsGenericMethodDefinition: true });
        object? source;
        try
        {
            source = method
                .MakeGenericMethod(entityType)
                .Invoke(schema, [name, executionContext, parameters]);
        }
        catch (TargetInvocationException exception) when (exception.InnerException != null)
        {
            ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
            throw;
        }

        if (source is null)
            throw new InvalidOperationException($"Schema '{schema.Name}' returned null row source for '{name}'.");

        var chunks = source.GetType().GetProperty(nameof(RowSource<object>.Chunks))?.GetValue(source);
        if (chunks is not IEnumerable enumerable)
        {
            throw new InvalidOperationException(
                $"Schema '{schema.Name}' returned row source '{source.GetType().FullName}' without enumerable chunks.");
        }

        var objectChunkAdapter = ObjectChunkAdapters.GetOrAdd(entityType, CreateObjectChunkAdapter);
        foreach (var chunk in enumerable)
        {
            if (chunk is null)
                continue;

            if (chunk is IReadOnlyList<object> objectChunk)
            {
                if (objectChunk.Count > 0)
                    yield return objectChunk;
                continue;
            }

            var objectChunkView = objectChunkAdapter(chunk);
            if (objectChunkView.Count > 0)
                yield return objectChunkView;
        }
    }

    public static IEnumerable<IReadOnlyList<T>> ConvertEnumerableOutputToChunks<T>(IEnumerable<T>? enumerable)
    {
        return RowChunking.FromEnumerableOutput(enumerable);
    }

    public static IReadOnlyList<T> MaterializeRows<T>(IEnumerable<T>? enumerable)
    {
        if (enumerable is null)
            return Array.Empty<T>();

        if (enumerable is IReadOnlyList<T> readOnlyList)
            return readOnlyList;

        if (enumerable is ICollection<T> collection)
        {
            var list = new List<T>(collection.Count);
            list.AddRange(collection);
            return list;
        }

        return enumerable.ToList();
    }

    public static List<T> MaterializeRowsList<T>(IEnumerable<T>? enumerable)
    {
        if (enumerable is null)
            return [];

        if (enumerable is List<T> existingList)
            return existingList;

        if (enumerable is ICollection<T> collection)
        {
            var materializedList = new List<T>(collection.Count);
            materializedList.AddRange(collection);
            return materializedList;
        }

        return enumerable.ToList();
    }

    public static IReadOnlyList<T> MaterializeChunkedRows<T>(IEnumerable<IReadOnlyList<T>>? chunks)
    {
        return MaterializeChunkedRowsList(chunks);
    }

    public static List<T> MaterializeChunkedRowsList<T>(IEnumerable<IReadOnlyList<T>>? chunks)
    {
        if (chunks is null)
            return [];

        var result = new List<T>();
        foreach (var chunk in chunks)
        {
            if (chunk is null || chunk.Count == 0)
                continue;

            result.EnsureCapacity(checked(result.Count + chunk.Count));
            if (chunk is ICollection<T> collection)
            {
                result.AddRange(collection);
                continue;
            }

            for (var index = 0; index < chunk.Count; index++)
                result.Add(chunk[index]);
        }

        return result;
    }

    public static IReadOnlyList<T> MaterializeGeneratedRows<T>(IEnumerable<Row>? rows)
        where T : Row
    {
        if (rows is null)
            return Array.Empty<T>();

        if (rows is IReadOnlyList<T> readOnlyList)
            return readOnlyList;

        var result = CreateGeneratedRowList<T>(rows);

        foreach (var row in rows)
            result.Add((T)row);

        return result;
    }

    public static IReadOnlyList<T> MaterializeGeneratedRows<T>(IEnumerable<T>? rows)
    {
        return MaterializeRows(rows);
    }

    public static IReadOnlyList<T> MaterializeGeneratedChunkedRows<T>(IEnumerable<IReadOnlyList<T>>? chunks)
    {
        return MaterializeChunkedRows(chunks);
    }

    public static IReadOnlyList<T> CastGeneratedRows<T>(IEnumerable<Row>? rows)
        where T : Row
    {
        if (rows is null)
            return Array.Empty<T>();

        if (rows is IReadOnlyList<T> typedRows)
            return typedRows;

        var result = CreateGeneratedRowList<T>(rows);
        foreach (var row in rows)
            result.Add((T)row);

        return result;
    }

    public static IReadOnlyList<T> MaterializeFilteredGeneratedRows<T>(IEnumerable<Row>? rows, Func<T, bool> predicate)
        where T : Row
    {
        ArgumentNullException.ThrowIfNull(predicate);
        if (rows is null)
            return Array.Empty<T>();

        var result = CreateGeneratedRowList<T>(rows);

        foreach (var row in rows)
        {
            var generatedRow = (T)row;
            if (predicate(generatedRow))
                result.Add(generatedRow);
        }

        return result;
    }

    public static IReadOnlyList<T> MaterializeFilteredGeneratedRows<T>(IEnumerable<T>? rows, Func<T, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        if (rows is null)
            return Array.Empty<T>();

        var result = rows is ICollection<T> collection
            ? new List<T>(collection.Count)
            : new List<T>();

        foreach (var row in rows)
        {
            if (predicate(row))
                result.Add(row);
        }

        return result;
    }

    public static IReadOnlyList<T> MaterializeFilteredGeneratedChunkedRows<T>(
        IEnumerable<IReadOnlyList<T>>? chunks,
        Func<T, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        if (chunks is null)
            return Array.Empty<T>();

        var result = new List<T>();

        foreach (var chunk in chunks)
        {
            if (chunk is null)
                continue;

            result.EnsureCapacity(checked(result.Count + chunk.Count));
            for (var index = 0; index < chunk.Count; index++)
            {
                var row = chunk[index];
                if (predicate(row))
                    result.Add(row);
            }
        }

        return result;
    }

    private static List<T> CreateGeneratedRowList<T>(IEnumerable<Row> rows)
        where T : Row
    {
        return rows is ICollection<Row> collection
            ? new List<T>(collection.Count)
            : new List<T>();
    }


    public static IEnumerable<IReadOnlyList<PrimitiveTypeEntity<T>>> ConvertScalarEnumerableToTypedChunks<T>(IEnumerable<T>? enumerable)
    {
        return enumerable is null
            ? []
            : CreateScalarBufferedChunks(enumerable);
    }

    public const int DefaultSourceChunkSize = RowChunking.DefaultChunkSize;

    private static Func<object, IReadOnlyList<object>> CreateObjectChunkAdapter(Type entityType)
    {
        var method = typeof(EvaluationHelper)
            .GetMethod(nameof(CreateObjectChunkView), BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new MissingMethodException(nameof(EvaluationHelper), nameof(CreateObjectChunkView));

        return (Func<object, IReadOnlyList<object>>)method
            .MakeGenericMethod(entityType)
            .CreateDelegate(typeof(Func<object, IReadOnlyList<object>>));
    }

    private static IReadOnlyList<object> CreateObjectChunkView<T>(object chunk)
    {
        if (chunk is not IReadOnlyList<T> typedChunk)
        {
            throw new InvalidOperationException(
                $"Schema returned chunk '{chunk.GetType().FullName}' that is not assignable to IReadOnlyList<{typeof(T).FullName}>.");
        }

        return new ObjectChunkView<T>(typedChunk);
    }

    private sealed class ObjectChunkView<T>(IReadOnlyList<T> source) : IReadOnlyList<object>
    {
        public int Count => source.Count;

        public object this[int index] => source[index]!;

        public IEnumerator<object> GetEnumerator()
        {
            for (var index = 0; index < source.Count; index++)
                yield return source[index]!;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    private static IEnumerable<IReadOnlyList<PrimitiveTypeEntity<T>>> CreateScalarBufferedChunks<T>(IEnumerable<T> rows)
    {
        return RowChunking.FromEnumerableOutput(rows.Select(static row => new PrimitiveTypeEntity<T>(row)));
    }
}
