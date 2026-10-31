using Musoq.Evaluator.IR.Bindings;

namespace Musoq.Evaluator.Tables;

internal sealed class DescriptionArgumentRow(
    StructuralArgumentDescription description) : Row
{

    public override int Count => 11;

    public override void VisitColumns<TVisitor>(ref TVisitor visitor) => TypedColumnVisit.All(this, ref visitor);

    public override void VisitColumn<TVisitor>(int columnIndex, ref TVisitor visitor)
    {
        switch (columnIndex)
        {
            case 0: TypedColumnVisit.Apply(columnIndex, description.Overload, ref visitor); return;
            case 1: TypedColumnVisit.Apply(columnIndex, description.Path, ref visitor); return;
            case 2: TypedColumnVisit.Apply(columnIndex, description.Kind, ref visitor); return;
            case 3: TypedColumnVisit.Apply(columnIndex, description.Type, ref visitor); return;
            case 4: TypedColumnVisit.Apply(columnIndex, description.Required, ref visitor); return;
            case 5: TypedColumnVisit.Apply(columnIndex, description.Nullable, ref visitor); return;
            case 6: TypedColumnVisit.Apply(columnIndex, description.HasDefault, ref visitor); return;
            case 7: TypedColumnVisit.Apply(columnIndex, description.Default, ref visitor); return;
            case 8: TypedColumnVisit.Apply(columnIndex, description.MaxDepth, ref visitor); return;
            case 9: TypedColumnVisit.Apply(columnIndex, description.MaxNodes, ref visitor); return;
            case 10: TypedColumnVisit.Apply(columnIndex, description.MaxStringBytes, ref visitor); return;
            default: throw new ArgumentOutOfRangeException(nameof(columnIndex));
        }
    }





    public override bool HasColumn(string name) => name is
        "Overload" or "Path" or "Kind" or "Type" or "Required" or "Nullable" or
        "HasDefault" or "Default" or "MaxDepth" or "MaxNodes" or "MaxStringBytes";


}
