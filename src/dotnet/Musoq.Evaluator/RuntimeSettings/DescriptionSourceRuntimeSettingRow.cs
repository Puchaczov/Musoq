using System.Collections.Generic;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator.RuntimeSettings;

internal sealed class DescriptionSourceRuntimeSettingRow(
    string name,
    bool required,
    bool secret,
    string phases,
    string status,
    string description) : Row
{
    private object[]? _values;

    public string Name { get; } = name;

    public bool Required { get; } = required;

    public bool Secret { get; } = secret;

    public string Phases { get; } = phases;

    public string Status { get; } = status;

    public string Description { get; } = description;

    public override int Count => 6;

    public override object this[int columnNumber] => columnNumber switch
    {
        0 => Name,
        1 => Required,
        2 => Secret,
        3 => Phases,
        4 => Status,
        5 => Description,
        _ => throw new ArgumentOutOfRangeException(nameof(columnNumber), columnNumber, "Column index is outside row bounds.")
    };

    public override object this[string name] => name switch
    {
        "Name" => Name,
        "Required" => Required,
        "Secret" => Secret,
        "Phases" => Phases,
        "Status" => Status,
        "Description" => Description,
        _ => throw new KeyNotFoundException(name)
    };

    public override bool HasColumn(string name) =>
        name is "Name" or "Required" or "Secret" or "Phases" or "Status" or "Description";

    public override object[] Values => _values ??= [Name, Required, Secret, Phases, Status, Description];
}
