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
            throw new SyntaxException(
                "Cannot recognize if query is regular or reordered.",
                _lexer.AlreadyResolvedQueryPart,
                DiagnosticCode.MQ2030_UnsupportedSyntax,
                Current.Span);
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
            var fromExpression = ComposeJoinOrApply(fromNode);
            var whereNode = ComposeWhere(false);
            var groupBy = ComposeGroupByNode();
            var window = ComposeWindowClause();
            var qualify = ComposeQualify();
            var orderBy = ComposeOrderBy();
            var skip = ComposeSkip();
            var take = ComposeTake();
            return new QueryNode(selectNode, fromExpression, whereNode, groupBy, orderBy, skip, take, window, qualify, default);
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
            var fromExpression = ComposeJoinOrApply(fromNode);
            var whereNode = ComposeWhere(false);
            var groupBy = ComposeGroupByNode();
            var window = ComposeWindowClause();
            var selectNode = ComposeSelectNode();
            var qualify = ComposeQualify();
            var orderBy = ComposeOrderBy();
            var skip = ComposeSkip();
            var take = ComposeTake();
            return new QueryNode(selectNode, fromExpression, whereNode, groupBy, orderBy, skip, take, window, qualify, default);
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
            var intNode = ComposeSliceCount("TAKE");

            return new TakeNode(intNode);
        }

        return null;
    }

    private SkipNode? ComposeSkip()
    {
        if (Current.TokenType == TokenType.Skip)
        {
            Consume(TokenType.Skip);
            var intNode = ComposeSliceCount("SKIP");

            return new SkipNode(intNode);
        }

        return null;
    }

    private IntegerNode ComposeSliceCount(string clause)
    {
        if (Current.TokenType != TokenType.Integer)
            throw InvalidSliceCount(clause, Current.Span);

        var countToken = Current;
        var count = ComposeInteger();
        if (countToken.Value.StartsWith("-", StringComparison.Ordinal) || IsNegativeInteger(count))
            throw InvalidSliceCount(clause, countToken.Span);

        return count;
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

    private SyntaxException InvalidSliceCount(string clause, TextSpan span)
    {
        return new SyntaxException(
            $"{clause} count must be a non-negative integer.",
            _lexer.AlreadyResolvedQueryPart,
            DiagnosticCode.MQ2038_InvalidSliceCount,
            span);
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

        if (fields.Length == 0)
            throw new SyntaxException(
                "GROUP BY requires at least one field.",
                _lexer.AlreadyResolvedQueryPart,
                DiagnosticCode.MQ2006_MissingGroupByColumn,
                Current.Span);
        if (Current.TokenType != TokenType.Having) return new GroupByNode(fields, null);

        Consume(TokenType.Having);

        var having = new HavingNode(ComposeOperations());

        return new GroupByNode(fields, having);
    }
}
