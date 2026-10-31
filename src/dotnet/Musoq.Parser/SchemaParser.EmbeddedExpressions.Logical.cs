using Musoq.Parser.Nodes;
using Musoq.Parser.Tokens;

namespace Musoq.Parser;

public partial class SchemaParser
{
    private Node ComposeComparisonExpression()
    {
        using var sourceRegion = _sourceCapture?.Begin(Syntax.SyntaxKind.Predicate, Current);
        return ComposeLogicalOrExpression();
    }

    private Node ComposeLogicalOrExpression()
    {
        var left = ComposeLogicalAndExpression();

        while (Current.TokenType == TokenType.Or)
        {
            Consume(TokenType.Or);
            var right = ComposeLogicalAndExpression();
            left = new OrNode(left, right);
        }

        return left;
    }

    private Node ComposeLogicalAndExpression()
    {
        var left = ComposeLogicalNotExpression();

        while (Current.TokenType == TokenType.And)
        {
            Consume(TokenType.And);
            var right = ComposeLogicalNotExpression();
            left = new AndNode(left, right);
        }

        return left;
    }

    private Node ComposeRelationalExpression()
    {
        var left = ComposeBitwiseOrExpression();

        switch (Current.TokenType)
        {
            case TokenType.Equality:
                Consume(TokenType.Equality);
                return new EqualityNode(left, ComposeBitwiseOrExpression());
            case TokenType.Diff:
                Consume(TokenType.Diff);
                return new DiffNode(left, ComposeBitwiseOrExpression());
            case TokenType.Greater:
                Consume(TokenType.Greater);
                return new GreaterNode(left, ComposeBitwiseOrExpression());
            case TokenType.GreaterEqual:
                Consume(TokenType.GreaterEqual);
                return new GreaterOrEqualNode(left, ComposeBitwiseOrExpression());
            case TokenType.Less:
                Consume(TokenType.Less);
                return new LessNode(left, ComposeBitwiseOrExpression());
            case TokenType.LessEqual:
                Consume(TokenType.LessEqual);
                return new LessOrEqualNode(left, ComposeBitwiseOrExpression());
            default:
                return left;
        }
    }

    private Node ComposeLogicalNotExpression()
    {
        if (Current.TokenType != TokenType.Not)
            return ComposeRelationalExpression();
        var token = ConsumeAndGetToken(TokenType.Not);
        return new NotNode(ComposeLogicalNotExpression()).WithSpan(token.Span);
    }

    private Node ComposeBitwiseOrExpression()
    {
        var left = ComposeShiftExpression();
        while (Current.TokenType is TokenType.Pipe or TokenType.Caret or TokenType.Ampersand or TokenType.NullCoalescing)
        {
            var operation = Current.TokenType;
            Consume(operation);
            Node right;
            if (operation == TokenType.NullCoalescing)
                right = ComposeBitwiseOrExpression();
            else
                right = ComposeShiftExpression();
            left = operation switch
            {
                TokenType.NullCoalescing => new CoalesceNode(left, right),
                TokenType.Pipe => new BitwiseOrNode(left, right),
                TokenType.Caret => new BitwiseXorNode(left, right),
                _ => new BitwiseAndNode(left, right)
            };
        }
        return left;
    }

    private Node ComposeShiftExpression()
    {
        var left = ComposeAdditiveExpression();

        while (Current.TokenType is TokenType.LeftShift or TokenType.RightShift)
        {
            var op = Current.TokenType;
            Consume(op);
            var right = ComposeAdditiveExpression();

            left = op == TokenType.LeftShift
                ? new LeftShiftNode(left, right)
                : new RightShiftNode(left, right);
        }

        return left;
    }

    private static bool IsNegativeConstantSizeExpression(Node sizeExpr, out string valueStr)
    {
        if (sizeExpr is IntegerNode intNode)
        {
            var value = Convert.ToInt64(intNode.ObjValue, System.Globalization.CultureInfo.InvariantCulture);
            if (value < 0)
            {
                valueStr = intNode.ObjValue.ToString() ?? value.ToString(System.Globalization.CultureInfo.InvariantCulture);
                return true;
            }
        }
        else if (sizeExpr is HyphenNode { Left: IntegerNode leftInt, Right: IntegerNode rightInt })
        {
            var leftVal = Convert.ToInt64(leftInt.ObjValue, System.Globalization.CultureInfo.InvariantCulture);
            var rightVal = Convert.ToInt64(rightInt.ObjValue, System.Globalization.CultureInfo.InvariantCulture);
            if (leftVal == 0 && rightVal > 0)
            {
                valueStr = $"-{rightVal}";
                return true;
            }
        }

        valueStr = string.Empty;
        return false;
    }
}
