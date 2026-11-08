using System.Collections.Generic;
using Musoq.Schema;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Tests.Schema.Dynamic;

internal sealed class NativeDynamicTestSource<TRow, TMaterializer>(
    IEnumerable<dynamic> values, IReadOnlyList<QueryRowField> fields) : RowSourceBase<TRow>
    where TMaterializer : struct, IQueryRowMaterializer<TRow>
{
    protected override void CollectChunks(IChunkWriter<TRow> writer)
    {
        var rows = new List<TRow>();
        foreach (object value in values)
        {
            var reader = new NativeDynamicTestReader(DynamicSource.ToDictionary(value), fields);
            rows.Add(TMaterializer.Materialize<NativeDynamicTestReader>(ref reader));
        }
        writer.Write(rows);
    }
}

internal ref struct NativeDynamicTestReader(
    IReadOnlyDictionary<string, object?> row, IReadOnlyList<QueryRowField> fields) : IQuerySourceFieldReader
{
    public T Read<T>(int slot)
    {
        var found = row.TryGetValue(fields[slot].Name, out var value);
        return !found || value is null ? default! : (T)value;
    }
}
