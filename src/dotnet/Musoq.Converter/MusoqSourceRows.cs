using System.Collections;
using System.Collections.Generic;

namespace Musoq.Converter;

public sealed class MusoqSourceRows
{
    private MusoqSourceRows(
        string schemaName,
        string sourceName,
        Type rowType,
        IEnumerable chunks)
    {
        SchemaName = InMemorySourceNames.NormalizeSchemaName(schemaName);
        SourceName = InMemorySourceNames.NormalizeSourceName(sourceName);
        RowType = rowType ?? throw new ArgumentNullException(nameof(rowType));
        Chunks = chunks ?? throw new ArgumentNullException(nameof(chunks));
    }

    public string SchemaName { get; }

    public string SourceName { get; }

    internal Type RowType { get; }

    internal IEnumerable Chunks { get; }

    public static MusoqSourceRows Create<T>(
        string schemaName,
        string sourceName,
        IEnumerable<IReadOnlyList<T>> chunks)
    {
        return new MusoqSourceRows(schemaName, sourceName, typeof(T), chunks);
    }

    internal IEnumerable<IReadOnlyList<T>> GetChunks<T>()
    {
        if (Chunks is IEnumerable<IReadOnlyList<T>> typedChunks)
            return typedChunks;

        throw new InvalidOperationException(
            $"Chunks for source '#{SchemaName}.{SourceName}()' cannot be used as '{typeof(T).FullName}'.");
    }
}
