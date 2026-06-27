namespace Musoq.Evaluator.Helpers;

public readonly struct IndexedHashJoinRow<T>
{
    public IndexedHashJoinRow(T row, int index)
    {
        Row = row;
        Index = index;
    }

    public T Row { get; }

    public int Index { get; }
}
