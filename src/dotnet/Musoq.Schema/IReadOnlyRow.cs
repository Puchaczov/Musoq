namespace Musoq.Schema;

public interface IReadOnlyRow
{
    int Count { get; }
    T GetValue<T>(int columnIndex);
    void VisitColumns<TVisitor>(ref TVisitor visitor)
        where TVisitor : IColumnValueVisitor, allows ref struct;

}
