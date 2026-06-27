using System.Collections.Generic;

namespace Musoq.Evaluator.Tables;

internal sealed class DescriptionColumnRow(string name, int index, string typeName) : Row
{
    private object[]? _values;

    public string Name { get; } = name;

    public int Index { get; } = index;

    public string TypeName { get; } = typeName;

    public override int Count => 3;

    public override object this[int columnNumber] => columnNumber switch
    {
        0 => Name,
        1 => Index,
        2 => TypeName,
        _ => throw new ArgumentOutOfRangeException(nameof(columnNumber), columnNumber, "Column index is outside row bounds.")
    };

    public override object this[string name] => name switch
    {
        "Name" => Name,
        "Index" => Index,
        "Type" => TypeName,
        _ => throw new KeyNotFoundException(name)
    };

    public override bool HasColumn(string name) => name is "Name" or "Index" or "Type";

    public override object[] Values => _values ??= [Name, Index, TypeName];
}
