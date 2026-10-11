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

    public string Name { get; } = name;

    public bool Required { get; } = required;

    public bool Secret { get; } = secret;

    public string Phases { get; } = phases;

    public string Status { get; } = status;

    public string Description { get; } = description;

    public override int Count => 6;

    public override void VisitColumns<TVisitor>(ref TVisitor visitor) => TypedColumnVisit.All(this, ref visitor);

    public override void VisitColumn<TVisitor>(int columnIndex, ref TVisitor visitor)
    {
        switch (columnIndex)
        {
            case 0: TypedColumnVisit.Apply(columnIndex, Name, ref visitor); return;
            case 1: TypedColumnVisit.Apply(columnIndex, Required, ref visitor); return;
            case 2: TypedColumnVisit.Apply(columnIndex, Secret, ref visitor); return;
            case 3: TypedColumnVisit.Apply(columnIndex, Phases, ref visitor); return;
            case 4: TypedColumnVisit.Apply(columnIndex, Status, ref visitor); return;
            case 5: TypedColumnVisit.Apply(columnIndex, Description, ref visitor); return;
            default: throw new ArgumentOutOfRangeException(nameof(columnIndex));
        }
    }





    public override bool HasColumn(string name) =>
        name is "Name" or "Required" or "Secret" or "Phases" or "Status" or "Description";


}
