namespace Musoq.Evaluator.IR.Bindings;

public sealed record OutputSchema(ColumnSchema[] Columns)
{
    public ColumnSchema? FindByName(string name)
    {
        foreach (var column in Columns)
        {
            if (string.Equals(column.Name, name, StringComparison.OrdinalIgnoreCase))
                return column;
        }

        return null;
    }

    public OutputSchema Merge(OutputSchema other)
    {
        ArgumentNullException.ThrowIfNull(other);
        var merged = new ColumnSchema[Columns.Length + other.Columns.Length];
        var index = 0;

        foreach (var col in Columns)
            merged[index] = new ColumnSchema(
                col.Name,
                col.Type,
                index++,
                col.IntendedTypeName,
                col.SourceReadType,
                col.EnumType)
            {
                Stability = col.Stability
            };

        foreach (var col in other.Columns)
            merged[index] = new ColumnSchema(
                col.Name,
                col.Type,
                index++,
                col.IntendedTypeName,
                col.SourceReadType,
                col.EnumType)
            {
                Stability = col.Stability
            };

        return new OutputSchema(merged);
    }

    public static OutputSchema Empty { get; } = new([]);
}
