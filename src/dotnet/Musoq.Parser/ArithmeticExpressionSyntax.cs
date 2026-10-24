using Musoq.Parser.Nodes;
using Musoq.Parser.Tokens;

namespace Musoq.Parser;

internal static class ArithmeticExpressionSyntax
{
    internal static Node Create(TokenType operation, Node left, Node right) => operation switch
    {
        TokenType.Plus => new AddNode(left, right),
        TokenType.StringConcatenation => new StringConcatenationNode(left, right),
        TokenType.Hyphen => new HyphenNode(left, right),
        TokenType.Star => new StarNode(left, right),
        TokenType.FSlash => new FSlashNode(left, right),
        TokenType.Mod => new ModuloNode(left, right),
        TokenType.Dot => new DotNode(left, right, string.Empty),
        TokenType.Ampersand => new BitwiseAndNode(left, right),
        TokenType.Pipe => new BitwiseOrNode(left, right),
        TokenType.Caret => new BitwiseXorNode(left, right),
        TokenType.LeftShift => new LeftShiftNode(left, right),
        TokenType.RightShift => new RightShiftNode(left, right),
        TokenType.NullCoalescing => new CoalesceNode(left, right),
        _ => throw new ArgumentOutOfRangeException(nameof(operation), operation, "Expected an arithmetic operator.")
    };
}
