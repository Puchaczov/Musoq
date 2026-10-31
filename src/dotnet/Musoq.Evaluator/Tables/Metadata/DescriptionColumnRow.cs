
namespace Musoq.Evaluator.Tables;

internal sealed class DescriptionColumnRow(string name, int index, string typeName) : Row
{

    public string Name { get; } = name;

    public int Index { get; } = index;

    public string TypeName { get; } = typeName;

    public override int Count => 3;

    public override void VisitColumns<TVisitor>(ref TVisitor visitor) => TypedColumnVisit.All(this, ref visitor);

    public override void VisitColumn<TVisitor>(int columnIndex, ref TVisitor visitor)
    {
        switch (columnIndex)
        {
            case 0: TypedColumnVisit.Apply(columnIndex, Name, ref visitor); return;
            case 1: TypedColumnVisit.Apply(columnIndex, Index, ref visitor); return;
            case 2: TypedColumnVisit.Apply(columnIndex, TypeName, ref visitor); return;
            default: throw new ArgumentOutOfRangeException(nameof(columnIndex));
        }
    }





    public override bool HasColumn(string name) => name is "Name" or "Index" or "Type";


}
