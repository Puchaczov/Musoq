namespace Musoq.Evaluator.Tables;

internal sealed class DescriptionConstructorRow(RowLayout layout, string[] values) : Row
{
    private readonly RowLayout _layout = layout ?? throw new ArgumentNullException(nameof(layout));
    private readonly string[] _values = values ?? throw new ArgumentNullException(nameof(values));

    public override int Count => _values.Length;

    public override void VisitColumns<TVisitor>(ref TVisitor visitor) => TypedColumnVisit.All(this, ref visitor);

    public override void VisitColumn<TVisitor>(int columnIndex, ref TVisitor visitor)
    {
        if ((uint)columnIndex >= (uint)_values.Length)
            throw new ArgumentOutOfRangeException(nameof(columnIndex));

        TypedColumnVisit.Apply(columnIndex, _values[columnIndex], ref visitor);
    }





    public override bool HasColumn(string name) => _layout.HasColumn(name);


}
