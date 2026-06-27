namespace Musoq.Examples.DataSources.Csv;

public sealed class CsvRow
{
    private readonly object?[] _values;

    public CsvRow(IReadOnlyList<object?> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        _values = values.ToArray();
    }

    public object? this[int columnNumber] => _values[columnNumber];
}
