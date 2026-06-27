using Musoq.Schema.DataSources;

namespace Musoq.Benchmarks;

public enum BenchmarkChunkShape
{
    Chunk512,
    Chunk4096,
    SingleGiant
}

internal static class BenchmarkSourceChunks
{
    public const int SmallChunkSize = 512;
    public const int ProductionChunkSize = RowChunking.DefaultChunkSize;

    public static Dictionary<string, IEnumerable<IReadOnlyList<T>>> Single<T>(
        string sourceName,
        IEnumerable<T> rows,
        BenchmarkChunkShape chunkShape = BenchmarkChunkShape.Chunk4096)
    {
        return new Dictionary<string, IEnumerable<IReadOnlyList<T>>>
        {
            [sourceName] = Create(rows, chunkShape)
        };
    }

    public static Dictionary<string, IEnumerable<IReadOnlyList<T>>> Single<T>(
        string sourceName,
        IEnumerable<T> rows,
        int chunkSize)
    {
        return new Dictionary<string, IEnumerable<IReadOnlyList<T>>>
        {
            [sourceName] = CreateFixedSizeChunks(rows, chunkSize)
        };
    }

    public static Dictionary<string, IEnumerable<IReadOnlyList<T>>> FromRows<T>(
        IDictionary<string, IEnumerable<T>> sources,
        BenchmarkChunkShape chunkShape = BenchmarkChunkShape.Chunk4096)
    {
        var chunks = new Dictionary<string, IEnumerable<IReadOnlyList<T>>>(sources.Count, StringComparer.OrdinalIgnoreCase);
        foreach (var (name, rows) in sources)
            chunks[name] = Create(rows, chunkShape);

        return chunks;
    }

    public static Dictionary<string, IEnumerable<IReadOnlyList<T>>> FromRows<T>(
        IDictionary<string, IEnumerable<T>> sources,
        int chunkSize)
    {
        var chunks = new Dictionary<string, IEnumerable<IReadOnlyList<T>>>(sources.Count, StringComparer.OrdinalIgnoreCase);
        foreach (var (name, rows) in sources)
            chunks[name] = CreateFixedSizeChunks(rows, chunkSize);

        return chunks;
    }

    public static IReadOnlyList<IReadOnlyList<T>> Create<T>(
        IEnumerable<T> rows,
        BenchmarkChunkShape chunkShape)
    {
        return chunkShape switch
        {
            BenchmarkChunkShape.Chunk512 => CreateFixedSizeChunks(rows, SmallChunkSize),
            BenchmarkChunkShape.Chunk4096 => CreateFixedSizeChunks(rows, ProductionChunkSize),
            BenchmarkChunkShape.SingleGiant => CreateSingleGiantChunk(rows),
            _ => throw new ArgumentOutOfRangeException(nameof(chunkShape), chunkShape, null)
        };
    }

    public static IReadOnlyList<IReadOnlyList<T>> Create<T>(IEnumerable<T> rows)
    {
        return Create(rows, BenchmarkChunkShape.Chunk4096);
    }

    private static IReadOnlyList<IReadOnlyList<T>> CreateSingleGiantChunk<T>(IEnumerable<T> rows)
    {
        var list = rows as IReadOnlyList<T> ?? rows.ToArray();
        if (list.Count == 0)
            return Array.Empty<IReadOnlyList<T>>();

        return [list];
    }

    private static IReadOnlyList<IReadOnlyList<T>> CreateFixedSizeChunks<T>(
        IEnumerable<T> rows,
        int chunkSize)
    {
        return RowChunking.FromEnumerableOutput(rows, chunkSize).ToArray();
    }

}
