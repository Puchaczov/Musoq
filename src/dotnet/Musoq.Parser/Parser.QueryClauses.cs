using Musoq.Parser.Diagnostics;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Nodes;
using Musoq.Parser.Tokens;

namespace Musoq.Parser;

public partial class Parser
{
    private QueryNode ComposeQuery()
    {
        QueryNode query;
        if (Current.TokenType == TokenType.Select)
            query = ComposeRegularQuery();
        else if (Current.TokenType == TokenType.From)
            query = ComposeReorderedQuery();
        else if (Current.TokenType == TokenType.Pivot)
            query = ComposePivotQuery();
        else if (Current.TokenType == TokenType.Unpivot)
            query = ComposeUnpivotQuery();
        else
            throw new NotSupportedException("Cannot recognize if query is regular or reordered.");
        return query;
    }

    private QueryNode ComposeRegularQuery()
    {
        PushFromAliasesScope();
        try
        {
            _fromPosition += 1;
            var selectNode = ComposeSelectNode();
            var fromNode = ComposeFrom();
            fromNode = ComposeJoinOrApply(fromNode);
            var whereNode = ComposeWhere(false);
            var groupBy = ComposeGroupByNode();
            var window = ComposeWindowClause();
            var qualify = ComposeQualify();
            var orderBy = ComposeOrderBy();
            var skip = ComposeSkip();
            var take = ComposeTake();
            return new QueryNode(selectNode, fromNode, whereNode, groupBy, orderBy, skip, take, window, qualify, default);
        }
        finally
        {
            PopFromAliasesScope();
        }
    }

    private QueryNode ComposeReorderedQuery()
    {
        PushFromAliasesScope();
        try
        {
            _fromPosition += 1;
            var fromNode = ComposeFrom();
            fromNode = ComposeJoinOrApply(fromNode);
            var whereNode = ComposeWhere(false);
            var groupBy = ComposeGroupByNode();
            var window = ComposeWindowClause();
            var selectNode = ComposeSelectNode();
            var qualify = ComposeQualify();
            var orderBy = ComposeOrderBy();
            var skip = ComposeSkip();
            var take = ComposeTake();
            return new QueryNode(selectNode, fromNode, whereNode, groupBy, orderBy, skip, take, window, qualify, default);
        }
        finally
        {
            PopFromAliasesScope();
        }
    }

    private OrderByNode? ComposeOrderBy()
    {
        if (Current.TokenType != TokenType.OrderBy) return null;

        Consume(TokenType.OrderBy);
        return new OrderByNode(ComposeOrderedFields());
    }

    private TakeNode? ComposeTake()
    {
        if (Current.TokenType == TokenType.Take)
        {
            Consume(TokenType.Take);
            var valueSpan = Current.Span;
            var intNode = ComposeInteger();

            if (IsNegativeInteger(intNode))
                RecordError(DiagnosticCode.MQ2030_UnsupportedSyntax,
                    "TAKE value must be non-negative.",
                    valueSpan);

            return new TakeNode(intNode);
        }

        return null;
    }

    private SkipNode? ComposeSkip()
    {
        if (Current.TokenType == TokenType.Skip)
        {
            Consume(TokenType.Skip);
            var valueSpan = Current.Span;
            var intNode = ComposeInteger();

            if (IsNegativeInteger(intNode))
                RecordError(DiagnosticCode.MQ2030_UnsupportedSyntax,
                    "SKIP value must be non-negative.",
                    valueSpan);

            return new SkipNode(intNode);
        }

        return null;
    }

    private static bool IsNegativeInteger(IntegerNode intNode)
    {
        return intNode.ObjValue switch
        {
            int i => i < 0,
            long l => l < 0,
            short s => s < 0,
            sbyte sb => sb < 0,
            _ => false
        };
    }

    private GroupByNode? ComposeGroupByNode()
    {
        if (Current.TokenType != TokenType.GroupBy) return null;

        Consume(TokenType.GroupBy);

        if (Current.TokenType == TokenType.Comma)
            throw new SyntaxException("Unnecessary comma found after GROUP BY clause.",
                _lexer.AlreadyResolvedQueryPart);

        if (Current.TokenType == TokenType.All)
            return ComposeGroupByAllNode();

        var fields = ComposeFields();

        if (Previous?.TokenType == TokenType.Comma && Current.TokenType == TokenType.EndOfFile)
            throw new SyntaxException("Unnecessary comma found after GROUP BY clause.",
                _lexer.AlreadyResolvedQueryPart);

        if (fields.Length == 0) throw new NotSupportedException("Group by clause does not have any fields.");
        if (Current.TokenType != TokenType.Having) return new GroupByNode(fields, null);

        Consume(TokenType.Having);

        var having = new HavingNode(ComposeOperations());

        return new GroupByNode(fields, having);
    }
}
