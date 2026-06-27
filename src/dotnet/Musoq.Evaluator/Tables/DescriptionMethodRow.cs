using System.Collections.Generic;

namespace Musoq.Evaluator.Tables;

internal sealed class DescriptionMethodRow(string method, string description, string category, string source)
    : Row
{
    private object[]? _values;

    public string Method { get; } = method;

    public string Description { get; } = description;

    public string Category { get; } = category;

    public string Source { get; } = source;

    public override int Count => 4;

    public override object this[int columnNumber] => columnNumber switch
    {
        0 => Method,
        1 => Description,
        2 => Category,
        3 => Source,
        _ => throw new ArgumentOutOfRangeException(nameof(columnNumber), columnNumber, "Column index is outside row bounds.")
    };

    public override object this[string name] => name switch
    {
        "Method" => Method,
        "Description" => Description,
        "Category" => Category,
        "Source" => Source,
        _ => throw new KeyNotFoundException(name)
    };

    public override bool HasColumn(string name) => name is "Method" or "Description" or "Category" or "Source";

    public override object[] Values => _values ??= [Method, Description, Category, Source];
}
