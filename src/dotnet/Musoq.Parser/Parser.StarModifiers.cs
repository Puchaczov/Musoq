using System.Collections.Generic;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Nodes;
using Musoq.Parser.Tokens;

namespace Musoq.Parser;

public partial class Parser
{
    private Node TryComposeStarModifiers(Node node)
    {
        if (node is not AllColumnsNode allColumns)
            return node;

        if (!IsStarModifierStart())
        {
            ThrowIfNearMissStarModifier();
            return node;
        }

        string? likePattern = null;
        var isNotLike = false;
        string[]? excludeColumns = null;
        StarReplaceItemNode[]? replaceItems = null;
        StarRenameItemNode[]? renameItems = null;

        if (Current.TokenType is TokenType.Like or TokenType.NotLike)
        {
            isNotLike = Current.TokenType == TokenType.NotLike;
            Consume(Current.TokenType);

            if (Current.TokenType != TokenType.Word && Current.TokenType != TokenType.StringLiteral)
                throw new SyntaxException(
                    "Expected a string pattern after LIKE in star expression.",
                    _lexer.AlreadyResolvedQueryPart,
                    DiagnosticCode.MQ2003_InvalidExpression,
                    Current.Span);

            likePattern = Current.Value;
            Consume(Current.TokenType);
        }

        if (IsContextSensitiveKeyword("exclude"))
        {
            Consume(Current.TokenType);
            excludeColumns = ComposeExcludeList();
        }

        if (IsContextSensitiveKeyword("replace"))
        {
            Consume(Current.TokenType);
            replaceItems = ComposeReplaceList();
        }

        if (IsContextSensitiveKeyword("rename"))
        {
            Consume(Current.TokenType);
            renameItems = ComposeRenameList();
        }

        if (IsStarModifierStart())
            throw new SyntaxException(
                "Duplicate or out-of-order star modifier. Expected order: LIKE/NOT LIKE, EXCLUDE, REPLACE, RENAME.",
                _lexer.AlreadyResolvedQueryPart,
                DiagnosticCode.MQ2030_UnsupportedSyntax,
                Current.Span);

        return new AllColumnsNode(
            allColumns.Alias,
            likePattern,
            isNotLike,
            excludeColumns,
            replaceItems,
            renameItems).WithSpan(allColumns.Span);
    }


    private bool IsStarModifierStart()
    {
        return Current.TokenType is TokenType.Like or TokenType.NotLike
               || IsContextSensitiveKeyword("exclude")
               || IsContextSensitiveKeyword("replace")
               || IsContextSensitiveKeyword("rename");
    }


    private bool IsContextSensitiveKeyword(string keyword)
    {
        return Current.TokenType == TokenType.Identifier
               && string.Equals(Current.Value, keyword, StringComparison.OrdinalIgnoreCase);
    }


    private void ThrowIfNearMissStarModifier()
    {
        if (Current.TokenType != TokenType.Identifier)
            return;

        var value = Current.Value;
        if (IsNearMiss(value, "exclude") || IsNearMiss(value, "replace") || IsNearMiss(value, "rename"))
            throw new SyntaxException(
                $"Unknown modifier '{value}' after star expression. Did you mean EXCLUDE or REPLACE, or RENAME?",
                _lexer.AlreadyResolvedQueryPart,
                DiagnosticCode.MQ2001_UnexpectedToken,
                Current.Span);
    }


    private static bool IsNearMiss(string input, string target)
    {
        if (string.Equals(input, target, StringComparison.OrdinalIgnoreCase))
            return false;

        if (input.Length < 3)
            return false;

        if (target.StartsWith(input, StringComparison.OrdinalIgnoreCase) ||
            input.StartsWith(target, StringComparison.OrdinalIgnoreCase))
            return true;

        var distance = LevenshteinDistance(input.ToUpperInvariant(), target.ToUpperInvariant());
        return distance <= 2;
    }


    private static int LevenshteinDistance(string source, string target)
    {
        var sourceLength = source.Length;
        var targetLength = target.Length;
        var distances = new int[sourceLength + 1][];

        for (var i = 0; i <= sourceLength; i++)
            distances[i] = new int[targetLength + 1];

        for (var i = 0; i <= sourceLength; i++) distances[i][0] = i;
        for (var j = 0; j <= targetLength; j++) distances[0][j] = j;

        for (var i = 1; i <= sourceLength; i++)
        for (var j = 1; j <= targetLength; j++)
        {
            var cost = source[i - 1] == target[j - 1] ? 0 : 1;
            distances[i][j] = Math.Min(
                Math.Min(distances[i - 1][j] + 1, distances[i][j - 1] + 1),
                distances[i - 1][j - 1] + cost);
        }

        return distances[sourceLength][targetLength];
    }


    private string[] ComposeExcludeList()
    {
        if (Current.TokenType != TokenType.LeftParenthesis)
            throw new SyntaxException(
                $"EXCLUDE requires a parenthesized column list. Expected '(' but found '{Current.Value}'. Usage: EXCLUDE (Column1, Column2).",
                _lexer.AlreadyResolvedQueryPart,
                DiagnosticCode.MQ2001_UnexpectedToken,
                Current.Span);

        Consume(TokenType.LeftParenthesis);

        if (Current.TokenType == TokenType.RightParenthesis)
            throw new SyntaxException(
                "EXCLUDE list must contain at least one column name. Usage: EXCLUDE (Column1, Column2).",
                _lexer.AlreadyResolvedQueryPart,
                DiagnosticCode.MQ2003_InvalidExpression,
                Current.Span);

        var columns = new List<string>();

        do
        {
            if (Current.TokenType == TokenType.Comma)
                Consume(TokenType.Comma);

            var columnName = ConsumeColumnIdentifier();

            if (Current.TokenType == TokenType.Dot)
            {
                Consume(TokenType.Dot);

                if (Current.TokenType == TokenType.Property)
                {
                    columnName = Current.Value;
                    Consume(TokenType.Property);
                }
                else
                {
                    columnName = ConsumeColumnIdentifier();
                }
            }

            columns.Add(columnName);
        } while (Current.TokenType == TokenType.Comma);

        if (Current.TokenType != TokenType.RightParenthesis)
            throw new SyntaxException(
                $"Expected ')' to close EXCLUDE list but found '{Current.Value}'. Check for missing commas between column names.",
                _lexer.AlreadyResolvedQueryPart,
                DiagnosticCode.MQ2001_UnexpectedToken,
                Current.Span);

        Consume(TokenType.RightParenthesis);

        return columns.ToArray();
    }


    private StarReplaceItemNode[] ComposeReplaceList()
    {
        if (Current.TokenType != TokenType.LeftParenthesis)
            throw new SyntaxException(
                $"REPLACE requires a parenthesized list. Expected '(' but found '{Current.Value}'. Usage: REPLACE (expression AS Column).",
                _lexer.AlreadyResolvedQueryPart,
                DiagnosticCode.MQ2001_UnexpectedToken,
                Current.Span);

        Consume(TokenType.LeftParenthesis);

        if (Current.TokenType == TokenType.RightParenthesis)
            throw new SyntaxException(
                "REPLACE list must contain at least one replacement. Usage: REPLACE (expression AS Column).",
                _lexer.AlreadyResolvedQueryPart,
                DiagnosticCode.MQ2003_InvalidExpression,
                Current.Span);

        var items = new List<StarReplaceItemNode>();

        do
        {
            if (Current.TokenType == TokenType.Comma)
                Consume(TokenType.Comma);

            var expression = ComposeOperations();

            if (Current.TokenType != TokenType.As)
                throw new SyntaxException(
                    $"Expected AS keyword after expression in REPLACE item but found '{Current.Value}'. Usage: REPLACE (expression AS ColumnName).",
                    _lexer.AlreadyResolvedQueryPart,
                    DiagnosticCode.MQ2001_UnexpectedToken,
                    Current.Span);

            Consume(TokenType.As);

            var columnName = ConsumeColumnIdentifier();

            items.Add(new StarReplaceItemNode(expression, columnName));
        } while (Current.TokenType == TokenType.Comma);

        if (Current.TokenType != TokenType.RightParenthesis)
            throw new SyntaxException(
                $"Expected ')' to close REPLACE list but found '{Current.Value}'. Check for missing commas between items.",
                _lexer.AlreadyResolvedQueryPart,
                DiagnosticCode.MQ2001_UnexpectedToken,
                Current.Span);

        Consume(TokenType.RightParenthesis);

        return items.ToArray();
    }


    private string ConsumeColumnIdentifier()
    {
        if (Current.TokenType is TokenType.Identifier or TokenType.Word)
        {
            var name = Current.Value;
            Consume(Current.TokenType);
            return name;
        }

        if (SqlKeywordTokenFacts.CanRepresentQualifiedIdentifier(Current.TokenType))
        {
            var name = Current.Value;
            Consume(Current.TokenType);
            return name;
        }

        throw new SyntaxException(
            $"Expected a column name but found '{Current.Value}'.",
            _lexer.AlreadyResolvedQueryPart,
            DiagnosticCode.MQ2001_UnexpectedToken,
            Current.Span);
    }


}
