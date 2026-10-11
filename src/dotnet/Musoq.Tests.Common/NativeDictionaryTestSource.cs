using System.Collections.Generic;
using Musoq.Schema;
using Musoq.Schema.DataSources;

namespace Musoq.Tests.Common;

public sealed class NativeDictionaryTestSource<TRow, TMaterializer>(
    IReadOnlyList<IReadOnlyDictionary<string, object?>> rows,
    IReadOnlyList<QueryRowField> fields) : RowSourceBase<TRow>
    where TMaterializer : struct, IQueryRowMaterializer<TRow>
{
    protected override void CollectChunks(IChunkWriter<TRow> writer)
    {
        var materialized = new List<TRow>(rows.Count);
        foreach (var row in rows)
        {
            var reader = new NativeDictionaryTestReader(row, fields);
            materialized.Add(TMaterializer.Materialize<NativeDictionaryTestReader>(ref reader));
        }
        writer.Write(materialized);
    }
}

public ref struct NativeDictionaryTestReader(
    IReadOnlyDictionary<string, object?> row, IReadOnlyList<QueryRowField> fields) : IQuerySourceFieldReader
{
    public T Read<T>(int slot)
    {
        if (!row.TryGetValue(fields[slot].Name, out var value) || value is null)
            return default!;
        return (T)value;
    }
}
