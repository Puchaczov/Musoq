using Musoq.Parser.Nodes;
using Musoq.Parser.Tokens;

namespace Musoq.Parser;

public partial class SchemaParser
{
    private Node ComposeComparisonExpression()
    {
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
        var left = ComposeRelationalExpression();

        while (Current.TokenType == TokenType.And)
        {
            Consume(TokenType.And);
            var right = ComposeRelationalExpression();
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

    private Node ComposeBitwiseOrExpression()
    {
        var left = ComposeBitwiseXorExpression();

        while (Current.TokenType == TokenType.Pipe)
        {
            Consume(TokenType.Pipe);
            var right = ComposeBitwiseXorExpression();
            left = new BitwiseOrNode(left, right);
        }

        return left;
    }

    private Node ComposeBitwiseXorExpression()
    {
        var left = ComposeBitwiseAndExpression();

        while (Current.TokenType == TokenType.Caret)
        {
            Consume(TokenType.Caret);
            var right = ComposeBitwiseAndExpression();
            left = new BitwiseXorNode(left, right);
        }

        return left;
    }

    private Node ComposeBitwiseAndExpression()
    {
        var left = ComposeShiftExpression();

        while (Current.TokenType == TokenType.Ampersand)
        {
            Consume(TokenType.Ampersand);
            var right = ComposeShiftExpression();
            left = new BitwiseAndNode(left, right);
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
