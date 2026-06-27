using Musoq.Parser.Nodes;
using Musoq.Parser.Tokens;

namespace Musoq.Parser;

public partial class Parser
{
    private Node ComposeOperations()
    {
        var node = ComposeEqualityOperators();

        while (IsQueryOperator(Current))
            switch (Current.TokenType)
            {
                case TokenType.And:
                    Consume(TokenType.And);
                    node = new AndNode(node, ComposeEqualityOperators());
                    break;
                case TokenType.Or:
                    Consume(TokenType.Or);
                    node = new OrNode(node, ComposeEqualityOperators());
                    break;
                default:
                    throw new NotSupportedException(
                        $"Unrecognized token for ComposeOperations(), the token was {Current.TokenType}");
            }

        return node;
    }


    private Node ComposeArithmeticExpression(int minPrecedence)
    {
        var left = ComposePostfixExpression(minPrecedence);

        if (IsNumericToken(Current)) left = new AddNode(left, ComposePostfixExpression(minPrecedence));

        while (IsArithmeticBinaryOperator(Current) &&
               _precedenceDictionary[Current.TokenType].Precendence >= minPrecedence)
        {
            var curr = Current;
            var op = _precedenceDictionary[Current.TokenType];
            var nextMinPrecedence = op.Associativity == Associativity.Left ? op.Precendence + 1 : op.Precendence;
            Consume(Current.TokenType);


            if (curr.TokenType == TokenType.Dot && IsSqlKeywordToken(Current.TokenType))
                ReplaceCurrentToken(new ColumnToken(Current.Value, Current.Span));

            var right = ComposeArithmeticExpression(nextMinPrecedence);

            left = curr.TokenType switch
            {
                TokenType.Plus => new AddNode(left, right),
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
                _ => throw new NotSupportedException($"{curr.TokenType} is not supported while parsing expression.")
            };
        }

        return left;
    }

}
