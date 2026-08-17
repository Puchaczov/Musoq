using System.Collections.Generic;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Nodes;
using Musoq.Parser.Tokens;

namespace Musoq.Parser;

public partial class Parser
{
    private SelectNode ComposeSelectNode()
    {
        Consume(TokenType.Select);
        ConsumeWhiteSpaces();

        var isDistinct = false;
        if (Current.TokenType == TokenType.Distinct)
        {
            Consume(TokenType.Distinct);
            ConsumeWhiteSpaces();
            isDistinct = true;
        }

        if (Current.TokenType == TokenType.Comma)
            throw new SyntaxException("Unnecessary comma found after SELECT keyword.", _lexer.AlreadyResolvedQueryPart);

        var fields = ComposeFields();

        if (fields.Length == 0)
            throw new SyntaxException(
                "SELECT list cannot be empty.",
                _lexer.AlreadyResolvedQueryPart,
                DiagnosticCode.MQ2005_InvalidSelectList,
                Current.Span);

        if (Previous?.TokenType == TokenType.Comma && Current.TokenType == TokenType.From)
            throw new SyntaxException("Unnecessary comma found at the end of SELECT clause.",
                _lexer.AlreadyResolvedQueryPart);

        return new SelectNode(fields, isDistinct);
    }


    private FieldNode[] ComposeFields()
    {
        var fields = new List<FieldNode>();
        var i = 0;

        do
        {
            if (Current.TokenType == TokenType.From) break;

            if (Current.TokenType == TokenType.EndOfFile) break;

            fields.Add(ConsumeField(i++));
        } while (!IsSetOperator(Current.TokenType) && Current.TokenType != TokenType.RightParenthesis &&
                 Current.TokenType != TokenType.From && Current.TokenType != TokenType.Having &&
                 Current.TokenType != TokenType.GroupBy &&
                 Current.TokenType != TokenType.Skip && Current.TokenType != TokenType.Take &&
                 Current.TokenType != TokenType.Select &&
                 Current.TokenType != TokenType.OrderBy &&
                 Current.TokenType != TokenType.Window &&
                 !IsContextualKeyword("rows") &&
                 !IsContextualKeyword("range") &&
                 !IsContextualKeyword("qualify") &&
                 ConsumeAndGetToken().TokenType == TokenType.Comma);

        return fields.ToArray();
    }


    private FieldOrderedNode[] ComposeOrderedFields()
    {
        var fields = new List<FieldOrderedNode>();
        var i = 0;

        do
        {
            fields.Add(ConsumeFieldOrdered(i++));
        } while (!IsSetOperator(Current.TokenType) && Current.TokenType != TokenType.RightParenthesis &&
                 Current.TokenType != TokenType.Skip && Current.TokenType != TokenType.Take &&
                 !IsContextualKeyword("rows") && !IsContextualKeyword("range") &&
                 ConsumeAndGetToken().TokenType == TokenType.Comma);

        return fields.ToArray();
    }


    private FieldNode ConsumeField(int order)
    {
        var fieldExpression = ComposeOperations();
        var alias = ComposeAlias(AliasContext.Projection);
        EnsureAliasSyntax(alias, AliasContext.Projection);
        return new FieldNode(fieldExpression, order, alias.Alias);
    }


    private FieldOrderedNode ConsumeFieldOrdered(int level)
    {
        var fieldExpression = ComposeOperations();
        var ordering = ComposeOrdering();
        return new FieldOrderedNode(fieldExpression, level, string.Empty, ordering.Order, ordering.NullOrdering);
    }

}
