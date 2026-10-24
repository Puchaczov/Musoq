using Musoq.Parser.Tokens;

namespace Musoq.Parser.Nodes;

public sealed class QuantifiedSubqueryNode(Node left, TokenType comparison, TokenType quantifier, Node subquery)
    : ExistsQueryNode(subquery)
{
    public Node Left { get; } = left;
    public TokenType Comparison { get; } = comparison;
    public TokenType Quantifier { get; } = quantifier;
    public override Type ReturnType => typeof(bool?);
    public override string Id { get; } = $"Quantified:{left.Id}:{comparison}:{quantifier}:{subquery.Id}";
    public override string ToString() => $"{Left} {Comparison} {Quantifier} ({Subquery})";
}
