namespace Musoq.Evaluator.IR.Planning;

internal readonly record struct PhysicalNodeId(int Value)
{
    public override string ToString() => $"physical:{Value}";
}
