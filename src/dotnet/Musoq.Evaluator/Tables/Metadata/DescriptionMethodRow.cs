using System.Collections.Generic;

namespace Musoq.Evaluator.Tables;

internal sealed class DescriptionMethodRow(string method, string description, string category, string source)
    : Row
{

    public string Method { get; } = method;

    public string Description { get; } = description;

    public string Category { get; } = category;

    public string Source { get; } = source;

    public override int Count => 4;

    public override void VisitColumns<TVisitor>(ref TVisitor visitor) => TypedColumnVisit.All(this, ref visitor);

    public override void VisitColumn<TVisitor>(int columnIndex, ref TVisitor visitor)
    {
        switch (columnIndex)
        {
            case 0: TypedColumnVisit.Apply(columnIndex, Method, ref visitor); return;
            case 1: TypedColumnVisit.Apply(columnIndex, Description, ref visitor); return;
            case 2: TypedColumnVisit.Apply(columnIndex, Category, ref visitor); return;
            case 3: TypedColumnVisit.Apply(columnIndex, Source, ref visitor); return;
            default: throw new ArgumentOutOfRangeException(nameof(columnIndex));
        }
    }





    public override bool HasColumn(string name) => name is "Method" or "Description" or "Category" or "Source";


}
