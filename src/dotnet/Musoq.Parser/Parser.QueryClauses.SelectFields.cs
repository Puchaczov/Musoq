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
            throw new SyntaxException(
                "A SELECT list cannot begin with a comma.",
                _lexer.AlreadyResolvedQueryPart,
                DiagnosticCode.MQ2015_LeadingComma,
                Current.Span);

        var fields = ComposeFields();

        if (fields.Length == 0)
            throw new SyntaxException(
                "SELECT list cannot be empty.",
                _lexer.AlreadyResolvedQueryPart,
                DiagnosticCode.MQ2005_InvalidSelectList,
                Current.Span);

        if (Previous?.TokenType == TokenType.Comma && Current.TokenType == TokenType.From)
            throw new SyntaxException(
                "A SELECT list cannot end with a comma.",
                _lexer.AlreadyResolvedQueryPart,
                DiagnosticCode.MQ2014_TrailingComma,
                Previous.Span);

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
                 Current.TokenType != TokenType.Where &&
                 Current.TokenType != TokenType.GroupBy &&
                 Current.TokenType != TokenType.Skip && Current.TokenType != TokenType.Take &&
                 Current.TokenType != TokenType.Select &&
                 Current.TokenType != TokenType.OrderBy &&
                 Current.TokenType != TokenType.Window &&
                 !IsContextualKeyword("rows") &&
                 !IsContextualKeyword("range") &&
                 !IsContextualKeyword("qualify") &&
                 !KeywordMisspellingFacts.IsLikelyMisspelledFromKeyword(Current, _lexer.Input) &&
                 ConsumeAndGetToken().TokenType == TokenType.Comma);

        return fields.ToArray();
    }

    private FieldNode ConsumeField(int order)
    {
        var fieldExpression = ComposeOperations();
        EnsureNumericLiteralBoundary(fieldExpression);
        var alias = ComposeAlias(AliasContext.Projection);
        EnsureAliasSyntax(alias, AliasContext.Projection);
        return new FieldNode(fieldExpression, order, alias.Alias);
    }

    private void EnsureNumericLiteralBoundary(Node fieldExpression)
    {
        if (!IsNumericExpression(fieldExpression) || !fieldExpression.HasSpan || Current.Span.Start != fieldExpression.Span.End ||
            !IsNumericContinuation(Current.TokenType))
            return;

        var span = new TextSpan(fieldExpression.Span.Start, Current.Span.End - fieldExpression.Span.Start);
        var literal = _lexer.Input[span.Start..span.End];
        throw new SyntaxException(
            $"Invalid numeric literal '{literal}'. Separate an alias with whitespace or use one of the supported numeric suffixes: b, ub, s, us, i, ui, l, ul, d, or D.",
            _lexer.AlreadyResolvedQueryPart,
            DiagnosticCode.MQ1003_InvalidNumericLiteral,
            span);
    }

    private static bool IsNumericExpression(Node expression)
    {
        var type = expression.ReturnType is { } returnType
            ? Nullable.GetUnderlyingType(returnType) ?? returnType
            : null;
        return type != null && Type.GetTypeCode(type) is
            TypeCode.SByte or TypeCode.Byte or TypeCode.Int16 or TypeCode.UInt16 or
            TypeCode.Int32 or TypeCode.UInt32 or TypeCode.Int64 or TypeCode.UInt64 or
            TypeCode.Single or TypeCode.Double or TypeCode.Decimal;
    }

    private static bool IsNumericContinuation(TokenType tokenType) => tokenType is TokenType.Identifier or TokenType.Word or
        TokenType.Function or TokenType.As or TokenType.From or TokenType.Where or TokenType.GroupBy or TokenType.Having or
        TokenType.OrderBy or TokenType.Skip or TokenType.Take or TokenType.Window or TokenType.Qualify or TokenType.Union or
        TokenType.UnionAll or TokenType.Except or TokenType.Intersect;

    private SyntaxException InvalidOrderByList(DiagnosticCode code, string message, TextSpan span) =>
        new(message, _lexer.AlreadyResolvedQueryPart, code, span);

    private FieldOrderedNode ConsumeFieldOrdered(int level)
    {
        var fieldExpression = ComposeOperations();
        var ordering = ComposeOrdering();
        return new FieldOrderedNode(fieldExpression, level, string.Empty, ordering.Order, ordering.NullOrdering);
    }

}
