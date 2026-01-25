namespace Musoq.Schema;

public interface IReadOnlyRow
{
    object this[int columnNumber] { get; }
}
