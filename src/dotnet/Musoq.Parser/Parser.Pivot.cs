using System.Collections.Generic;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Nodes;
using Musoq.Parser.Tokens;
namespace Musoq.Parser;
public partial class Parser
{
    private QueryNode ComposePivotQuery()
    {
        PushFromAliasesScope();
        try
        {
            _fromPosition += 1;
            Consume(TokenType.Pivot);
            var fromNode = ComposeSource(SourceParseContext.Primary);
            var fromExpression = ComposeJoinOrApply(fromNode);
            var keys = ComposePivotKeys();
            var values = ComposePivotValues(keys.Length);
            ConsumePivotUsingKeyword();
            var measures = ComposePivotMeasures();
            var groupBy = ComposeGroupByNode();
            var orderBy = ComposeOrderBy();
            var skip = ComposeSkip();
            var take = ComposeTake();
            var select = PivotQueryBuilder.BuildSelectNode(keys, values, measures, groupBy, _lexer.AlreadyResolvedQueryPart);
            return new QueryNode(select, fromExpression, null, groupBy, orderBy, skip, take, null, null, default);
        }
        finally
        {
            PopFromAliasesScope();
        }
    }
    private SyntaxException PivotSyntax(string message, DiagnosticCode code, TextSpan span)
        => new(message, _lexer.AlreadyResolvedQueryPart, code, span);
    private Node[] ComposePivotKeys()
    {
        if (Current.TokenType != TokenType.On)
            throw PivotSyntax(
                "PIVOT requires an ON clause after the source. Example: pivot #sales.orders() on Quarter in ('Q1' as Q1) using Sum(Amount) as Sales.",
                DiagnosticCode.MQ2002_MissingToken,
                Current.Span);
        Consume(TokenType.On);
        var keys = new List<Node> { ComposeArithmeticExpression(0) };
        while (Current.TokenType == TokenType.Comma)
        {
            Consume(TokenType.Comma);
            keys.Add(ComposeArithmeticExpression(0));
        }
        if (Current.TokenType != TokenType.In)
            throw PivotSyntax(
                "PIVOT ON requires a static IN (...) list so output columns are known at compile time.",
                DiagnosticCode.MQ2002_MissingToken,
                Current.Span);
        Consume(TokenType.In);
        return keys.ToArray();
    }
    private PivotValue[] ComposePivotValues(int keyCount)
    {
        Consume(TokenType.LeftParenthesis);
        var values = new List<PivotValue>();
        if (Current.TokenType == TokenType.RightParenthesis)
            throw PivotSyntax(
                "PIVOT IN requires at least one constant value.",
                DiagnosticCode.MQ2003_InvalidExpression,
                Current.Span);

        do
        {
            var expressions = ComposePivotValueExpressions(keyCount);
            ValidatePivotValueExpressions(expressions, keyCount);
            var aliasResult = ComposeAlias(AliasContext.Pivot);
            EnsureAliasSyntax(aliasResult, AliasContext.Pivot);
            var alias = aliasResult.Alias;
            var aliasSpan = aliasResult.Span;
            alias = string.IsNullOrWhiteSpace(alias)
                ? PivotQueryBuilder.CreateValueAlias(expressions)
                : alias;
            if (string.IsNullOrWhiteSpace(alias))
                throw PivotSyntax(
                    "PIVOT value aliases cannot be empty. Add an explicit alias, for example: '' as Empty.",
                    DiagnosticCode.MQ2022_InvalidAlias,
                    aliasSpan.IsEmpty ? expressions[0].Span : aliasSpan);
            values.Add(new PivotValue(expressions, alias));
            if (Current.TokenType != TokenType.Comma)
                break;
            Consume(TokenType.Comma);
            if (Current.TokenType == TokenType.RightParenthesis)
                throw PivotSyntax(
                    "PIVOT IN list has a trailing comma. Add another value or remove the comma.",
                    DiagnosticCode.MQ2014_TrailingComma,
                    Current.Span);
        } while (true);
        Consume(TokenType.RightParenthesis);
        return values.ToArray();
    }

    private Node[] ComposePivotValueExpressions(int keyCount)
    {
        if (keyCount == 1)
            return [ComposeOperations()];

        var values = new List<Node>();
        if (Current.TokenType == TokenType.LeftParenthesis)
        {
            Consume(TokenType.LeftParenthesis);
            while (Current.TokenType != TokenType.RightParenthesis)
            {
                values.Add(ComposeOperations());
                if (Current.TokenType != TokenType.Comma)
                    break;
                Consume(TokenType.Comma);
            }
            Consume(TokenType.RightParenthesis);
            return values.ToArray();
        }

        return [ComposeOperations()];
    }

    private void ValidatePivotValueExpressions(Node[] expressions, int keyCount)
    {
        if (expressions.Length != keyCount)
            throw PivotSyntax(
                $"PIVOT value tuple length mismatch. ON has {keyCount} key(s), but this IN value has {expressions.Length}.",
                DiagnosticCode.MQ2003_InvalidExpression,
                expressions.Length > 0 ? expressions[0].Span : Current.Span);

        foreach (var expression in expressions)
            if (!PivotQueryBuilder.IsSupportedValueExpression(expression))
                throw PivotSyntax(
                    "PIVOT IN values must be constants. Use literal values such as 'Q1', 2024, true, or null.",
                    DiagnosticCode.MQ2003_InvalidExpression,
                    expression.Span);
    }
    private void ConsumePivotUsingKeyword()
    {
        if (!IsContextualKeyword("using"))
            throw PivotSyntax(
                "PIVOT requires a USING clause with aggregate measures. Example: using Sum(Amount) as Sales.",
                DiagnosticCode.MQ2002_MissingToken,
                Current.Span);
        Consume(Current.TokenType);
    }
    private FieldNode[] ComposePivotMeasures()
    {
        var measures = new List<FieldNode>();
        var order = 0;
        while (!PivotQueryBuilder.IsMeasureTerminator(Current.TokenType))
        {
            measures.Add(ConsumeField(order++));
            if (Current.TokenType != TokenType.Comma)
                break;
            Consume(TokenType.Comma);
            if (PivotQueryBuilder.IsMeasureTerminator(Current.TokenType))
                throw PivotSyntax(
                    "PIVOT USING list has a trailing comma. Add another aggregate or remove the comma.",
                    DiagnosticCode.MQ2014_TrailingComma,
                    Current.Span);
        }
        if (measures.Count == 0)
            throw PivotSyntax(
                "PIVOT USING requires at least one aggregate function call.",
                DiagnosticCode.MQ2003_InvalidExpression,
                Current.Span);
        return measures.ToArray();
    }
}
