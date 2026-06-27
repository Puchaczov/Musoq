using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Tokens;

namespace Musoq.Parser;

public partial class Parser
{
    private ExpressionFromNode ComposeJoinOrApply(FromNode from)
    {
        if (!IsJoinOrApplyToken(Current.TokenType)) return new ExpressionFromNode(from);

        while (IsJoinOrApplyToken(Current.TokenType))
            switch (Current.TokenType)
            {
                case TokenType.InnerJoin:
                    Consume(TokenType.InnerJoin);
                    from = ComposeConditionJoin(from, JoinType.Inner);
                    break;
                case TokenType.OuterJoin:
                    var outerToken = (OuterJoinToken)Current;
                    Consume(TokenType.OuterJoin);
                    from = ComposeConditionJoin(from, outerToken.Type switch
                    {
                        OuterJoinType.Left => JoinType.OuterLeft,
                        OuterJoinType.Right => JoinType.OuterRight,
                        OuterJoinType.Full => JoinType.OuterFull,
                        _ => throw new ArgumentOutOfRangeException(nameof(outerToken.Type), outerToken.Type, "Unsupported outer join type.")
                    });
                    break;
                case TokenType.SemiJoin:
                    Consume(TokenType.SemiJoin);
                    from = ComposeConditionJoin(from, JoinType.LeftSemi);
                    break;
                case TokenType.AntiJoin:
                    Consume(TokenType.AntiJoin);
                    from = ComposeConditionJoin(from, JoinType.LeftAntiSemi);
                    break;
                case TokenType.CrossJoin:
                    Consume(TokenType.CrossJoin);
                    from = new JoinFromNode(from,
                        Compose(parser => parser.ComposeFrom(false)),
                        new BooleanNode(true),
                        JoinType.Cross);
                    break;
                case TokenType.CrossApply:
                    Consume(TokenType.CrossApply);
                    from = ComposeApplyFrom(from, ApplyType.Cross);
                    break;
                case TokenType.OuterApply:
                    Consume(TokenType.OuterApply);
                    from = ComposeApplyFrom(from, ApplyType.Outer);
                    break;
                case TokenType.AsOfJoin:
                    var asOfToken = (AsOfJoinToken)Current;
                    Consume(TokenType.AsOfJoin);
                    from = ComposeConditionJoin(from,
                        asOfToken.IsLeft
                            ? JoinType.AsOfLeft
                            : JoinType.AsOf);
                    break;
            }

        if (from is JoinFromNode joinFrom) from = new JoinNode(joinFrom);

        if (from is ApplyFromNode applyFrom) from = new ApplyNode(applyFrom);

        return new ExpressionFromNode(from);
    }

    private static bool IsJoinOrApplyToken(TokenType currentTokenType) =>
        currentTokenType is TokenType.InnerJoin or TokenType.OuterJoin or TokenType.SemiJoin
            or TokenType.AntiJoin or TokenType.CrossJoin or TokenType.CrossApply
            or TokenType.OuterApply or TokenType.AsOfJoin;

    private ApplyFromNode ComposeApplyFrom(FromNode from, ApplyType applyType)
    {
        var with = Compose(parser => parser.ComposeFrom(false, true));
        var withOrdinality = ConsumeWithOrdinalityIfPresent();

        return new ApplyFromNode(from, with, applyType, withOrdinality);
    }

    private bool ConsumeWithOrdinalityIfPresent()
    {
        if (Current.TokenType != TokenType.With)
            return false;

        var withToken = ConsumeAndGetToken(TokenType.With);
        if (Current.TokenType is not (TokenType.Identifier or TokenType.Word) ||
            !string.Equals(Current.Value, "ordinality", StringComparison.OrdinalIgnoreCase))
        {
            throw new SyntaxException(
                "Expected ORDINALITY after WITH in APPLY source.",
                _lexer.AlreadyResolvedQueryPart,
                DiagnosticCode.MQ2002_MissingToken,
                withToken.Span);
        }

        Consume(Current.TokenType);
        return true;
    }

    private JoinFromNode ComposeConditionJoin(FromNode from, JoinType joinType)
    {
        var with = ComposeAndSkip(parser => parser.ComposeFrom(false), TokenType.On);
        var expression = ComposeOperations();
        var tieBreak = ComposeAsOfTieBreakIfPresent(joinType);

        return new JoinFromNode(from, with, expression, joinType, tieBreak);
    }

    private FieldOrderedNode? ComposeAsOfTieBreakIfPresent(JoinType joinType)
    {
        if (!IsContextualKeyword("tie"))
            return null;

        var tieToken = Current;
        if (joinType is not (JoinType.AsOf or JoinType.AsOfLeft))
        {
            throw new SyntaxException(
                "TIE BREAK BY is only supported for ASOF JOIN.",
                _lexer.AlreadyResolvedQueryPart,
                DiagnosticCode.MQ2030_UnsupportedSyntax,
                tieToken.Span);
        }

        Consume(Current.TokenType);

        if (!IsContextualKeyword("break"))
        {
            throw new SyntaxException(
                "Expected BREAK after TIE in ASOF JOIN tie-break clause.",
                _lexer.AlreadyResolvedQueryPart,
                DiagnosticCode.MQ2002_MissingToken,
                Current.Span);
        }

        Consume(Current.TokenType);

        if (!IsContextualKeyword("by"))
        {
            throw new SyntaxException(
                "Expected BY after TIE BREAK in ASOF JOIN tie-break clause.",
                _lexer.AlreadyResolvedQueryPart,
                DiagnosticCode.MQ2002_MissingToken,
                Current.Span);
        }

        Consume(Current.TokenType);

        var expression = ComposeOperations();
        var ordering = ComposeOrdering(allowClauseBoundaries: true);
        return new FieldOrderedNode(expression, 0, string.Empty, ordering.Order, ordering.NullOrdering);
    }

    private SchemaMethodFromNode ComposeSchemaMethod()
    {
        if (Current.TokenType == TokenType.MethodAccess)
        {
            var sourceAlias = Current.Value;
            var schemaName = EnsureHashPrefix(sourceAlias);
            var accessMethod = ComposeAccessMethod(sourceAlias);
            var (alias, _) = ComposeAlias();

            return new SchemaMethodFromNode(alias, schemaName, accessMethod.Name);
        }

        var schemaNode = ComposeSchemaName();
        ConsumeAsColumn(TokenType.Dot);
        var identifier = (IdentifierNode)ComposeBaseTypes();
        var (composeAlias, _) = ComposeAlias();

        return new SchemaMethodFromNode(composeAlias, schemaNode, identifier.Name);
    }
}
