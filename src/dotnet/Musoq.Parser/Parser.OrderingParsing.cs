using Musoq.Parser.Diagnostics;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Nodes;
using Musoq.Parser.Tokens;

namespace Musoq.Parser;

public partial class Parser
{
    private readonly record struct OrderingSpec(Order Order, NullOrdering NullOrdering);

    private OrderingSpec ComposeOrdering(bool allowClauseBoundaries = false)
    {
        var order = ComposeOrder(allowClauseBoundaries);
        var nullOrdering = ComposeNullOrdering();
        return new OrderingSpec(order, nullOrdering);
    }

    private Order ComposeOrder(bool allowClauseBoundaries)
    {
        switch (Current.TokenType)
        {
            case TokenType.Asc:
                Consume(TokenType.Asc);
                return Order.Ascending;
            case TokenType.Desc:
                Consume(TokenType.Desc);
                return Order.Descending;
            case TokenType.Comma:
            case TokenType.EndOfFile:
            case TokenType.Semicolon:
            case TokenType.RightParenthesis:
            case TokenType.Skip:
            case TokenType.Take:
            case TokenType.Where when allowClauseBoundaries:
            case TokenType.GroupBy when allowClauseBoundaries:
            case TokenType.Having when allowClauseBoundaries:
            case TokenType.OrderBy when allowClauseBoundaries:
            case TokenType.Window when allowClauseBoundaries:
            case TokenType.InnerJoin when allowClauseBoundaries:
            case TokenType.OuterJoin when allowClauseBoundaries:
            case TokenType.SemiJoin when allowClauseBoundaries:
            case TokenType.AntiJoin when allowClauseBoundaries:
            case TokenType.CrossJoin when allowClauseBoundaries:
            case TokenType.CrossApply when allowClauseBoundaries:
            case TokenType.OuterApply when allowClauseBoundaries:
            case TokenType.AsOfJoin when allowClauseBoundaries:
                return Order.Ascending;
            default:
                if (IsOrderingBoundary(allowClauseBoundaries) || IsContextualKeyword("nulls"))
                    return Order.Ascending;
                throw new SyntaxException(
                    $"Unrecognized token for ComposeOrder(), the token was {Current.TokenType}.",
                    _lexer.AlreadyResolvedQueryPart,
                    DiagnosticCode.MQ2009_InvalidOrderByExpression,
                    Current.Span);
        }
    }

    private NullOrdering ComposeNullOrdering()
    {
        if (!IsContextualKeyword("nulls"))
            return NullOrdering.Default;

        Consume(Current.TokenType);

        if (IsContextualKeyword("first"))
        {
            Consume(Current.TokenType);
            return NullOrdering.First;
        }

        if (IsContextualKeyword("last"))
        {
            Consume(Current.TokenType);
            return NullOrdering.Last;
        }

        throw new SyntaxException(
            "Expected FIRST or LAST after NULLS in ORDER BY.",
            _lexer.AlreadyResolvedQueryPart,
            DiagnosticCode.MQ2009_InvalidOrderByExpression,
            Current.Span);
    }

    private bool IsOrderingBoundary(bool allowClauseBoundaries)
    {
        return IsContextualKeyword("rows") ||
               IsContextualKeyword("range") ||
               allowClauseBoundaries && IsContextualKeyword("qualify");
    }
}
