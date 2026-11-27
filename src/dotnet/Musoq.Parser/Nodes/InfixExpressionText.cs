using Musoq.Parser.Tokens;

namespace Musoq.Parser.Nodes;

internal static class InfixExpressionText
{
    private const int ComparisonPrecedence = -1;

    internal static string FormatPredicateOperand(Node operand) => FormatOperand(operand, ComparisonPrecedence, false);

    internal static string Format(BinaryNode node, string operation)
    {
        var precedence = GetPrecedence(node);
        var left = FormatOperand(node.Left, precedence, node.Left is CoalesceNode);
        var right = FormatOperand(node.Right, precedence, node is not CoalesceNode);
        return $"{left} {operation} {right}";
    }

    private static string FormatOperand(Node operand, int parentPrecedence, bool parenthesizeEqual)
    {
        if (operand is ConstantValueNode or IdentifierNode or AccessColumnNode)
            return NumericLiteralNode.FormatOperand(operand);

        var text = operand.ToString();
        var precedence = GetPrecedence(operand);
        return precedence < parentPrecedence || precedence == parentPrecedence && parenthesizeEqual
            ? $"({text})"
            : text;
    }

    private static int GetPrecedence(Node node) =>
        node switch
        {
            OrNode => -4,
            AndNode => -3,
            NotNode => -2,
            EqualityNode
            or DiffNode
            or GreaterNode
            or GreaterOrEqualNode
            or LessNode
            or LessOrEqualNode
            or LikeNode
            or RLikeNode
            or IsNullNode
            or IsDistinctFromNode
            or RowPresenceNode
            or BetweenNode
            or InNode
            or InQueryNode
            or CollectionInNode
            or ContainsNode
            or ExistsQueryNode => ComparisonPrecedence,
            BitwiseOrNode or BitwiseAndNode or BitwiseXorNode or CoalesceNode => Parser.GetArithmeticPrecedence(
                TokenType.Pipe
            ),
            LeftShiftNode or RightShiftNode => Parser.GetArithmeticPrecedence(TokenType.LeftShift),
            AddNode or HyphenNode or StringConcatenationNode => Parser.GetArithmeticPrecedence(TokenType.Plus),
            StarNode or FSlashNode or ModuloNode => Parser.GetArithmeticPrecedence(TokenType.Star),
            _ => int.MaxValue,
        };
}
