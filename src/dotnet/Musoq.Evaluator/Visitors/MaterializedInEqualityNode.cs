using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

// A materialized IN lookup can match NaN. An explicit SQL equality in the same join cannot.
internal sealed class MaterializedInEqualityNode(Node left, Node right) : EqualityNode(left, right)
{
    internal static Func<Node, Node, EqualityNode> GetRebindFactory(EqualityNode original) =>
        original is MaterializedInEqualityNode
            ? static (left, right) => new MaterializedInEqualityNode(left, right)
            : static (left, right) => new EqualityNode(left, right);
}
