namespace Musoq.Schema;

/// <summary>Consumes a column value with its concrete type and without an object-valued cell.</summary>
public interface IColumnValueVisitor
{
    /// <summary>Visits one value. Value-type visitors are passed by reference by the row contract.</summary>
    void Visit<T>(int columnIndex, in T value);
}
