using System.Collections.Generic;
using Musoq.Evaluator.IR.Bindings;

namespace Musoq.Evaluator.Tables;

internal sealed class DescriptionArgumentRow(
    StructuralArgumentDescription description) : Row
{
    private object[]? _values;

    public override int Count => 11;

    public override object this[int columnNumber] => columnNumber switch
    {
        0 => description.Overload,
        1 => description.Path,
        2 => description.Kind,
        3 => description.Type,
        4 => description.Required!,
        5 => description.Nullable,
        6 => description.HasDefault,
        7 => description.Default!,
        8 => description.MaxDepth!,
        9 => description.MaxNodes!,
        10 => description.MaxStringBytes!,
        _ => throw new ArgumentOutOfRangeException(nameof(columnNumber), columnNumber,
            "Column index is outside row bounds.")
    };

    public override object this[string name] => name switch
    {
        "Overload" => description.Overload,
        "Path" => description.Path,
        "Kind" => description.Kind,
        "Type" => description.Type,
        "Required" => description.Required!,
        "Nullable" => description.Nullable,
        "HasDefault" => description.HasDefault,
        "Default" => description.Default!,
        "MaxDepth" => description.MaxDepth!,
        "MaxNodes" => description.MaxNodes!,
        "MaxStringBytes" => description.MaxStringBytes!,
        _ => throw new KeyNotFoundException(name)
    };

    public override bool HasColumn(string name) => name is
        "Overload" or "Path" or "Kind" or "Type" or "Required" or "Nullable" or
        "HasDefault" or "Default" or "MaxDepth" or "MaxNodes" or "MaxStringBytes";

    public override object[] Values => _values ??= [
        description.Overload,
        description.Path,
        description.Kind,
        description.Type,
        description.Required!,
        description.Nullable,
        description.HasDefault,
        description.Default!,
        description.MaxDepth!,
        description.MaxNodes!,
        description.MaxStringBytes!
    ];
}
