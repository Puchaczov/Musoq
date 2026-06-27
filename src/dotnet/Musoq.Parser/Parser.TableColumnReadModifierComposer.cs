using System.Collections.Generic;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Nodes;
using Musoq.Parser.Tokens;

namespace Musoq.Parser;

public partial class Parser
{
    private IReadOnlyList<CreateTableColumnModifier> ComposeTableColumnReadModifiers(string columnName)
    {
        var modifiers = new List<CreateTableColumnModifier>();
        var usedKeys = new HashSet<string>(StringComparer.Ordinal);

        while (IsTableColumnReadModifierStart(Current))
            modifiers.Add(ReadTableColumnModifier(columnName, usedKeys));

        return modifiers;
    }

    private CreateTableColumnModifier ReadTableColumnModifier(string columnName, HashSet<string> usedKeys)
    {
        var modifierName = Current.Value;
        if (modifierName.Equals("encoding", StringComparison.OrdinalIgnoreCase))
            return ReadStringTableColumnModifier(columnName, usedKeys, "encoding");

        if (modifierName.Equals("culture", StringComparison.OrdinalIgnoreCase))
            return ReadStringTableColumnModifier(columnName, usedKeys, "culture");

        if (modifierName.Equals("format", StringComparison.OrdinalIgnoreCase))
            return ReadStringTableColumnModifier(columnName, usedKeys, "format");

        if (modifierName.Equals("trim", StringComparison.OrdinalIgnoreCase))
        {
            var trimToken = ConsumeAndGetToken(Current.TokenType);
            return CreateTableColumnModifier(columnName, usedKeys, "trim", "true", trimToken.Span);
        }

        var sourceToken = ConsumeAndGetToken(Current.TokenType);
        var sourceName = ConsumeTableColumnModifierIdentifier("source modifier name");
        var valueToken = ConsumeAndGetToken(TokenType.StringLiteral);
        return CreateTableColumnModifier(
            columnName,
            usedKeys,
            $"source.{sourceName.Value.ToLowerInvariant()}",
            valueToken.Value,
            sourceToken.Span.Through(valueToken.Span));
    }

    private CreateTableColumnModifier ReadStringTableColumnModifier(
        string columnName,
        HashSet<string> usedKeys,
        string key)
    {
        var modifierToken = ConsumeAndGetToken(Current.TokenType);
        var valueToken = ConsumeAndGetToken(TokenType.StringLiteral);
        return CreateTableColumnModifier(
            columnName,
            usedKeys,
            key,
            valueToken.Value,
            modifierToken.Span.Through(valueToken.Span));
    }

    private CreateTableColumnModifier CreateTableColumnModifier(
        string columnName,
        HashSet<string> usedKeys,
        string key,
        string value,
        TextSpan span)
    {
        if (!usedKeys.Add(key))
        {
            throw new SyntaxException(
                $"Duplicate read modifier '{key}' for column '{columnName}'.",
                _lexer.AlreadyResolvedQueryPart,
                DiagnosticCode.MQ2012_InvalidSchemaDefinition,
                span);
        }

        return new CreateTableColumnModifier(key, value, span);
    }

    private Token ConsumeTableColumnModifierIdentifier(string expected)
    {
        if (!IsTableColumnModifierToken(Current))
        {
            throw new SyntaxException(
                $"Expected {expected} but received {Current.TokenType}.",
                _lexer.AlreadyResolvedQueryPart,
                DiagnosticCode.MQ2012_InvalidSchemaDefinition,
                Current.Span);
        }

        return ConsumeAndGetToken(Current.TokenType);
    }

    private static bool IsTableColumnReadModifierStart(Token token)
    {
        return IsTableColumnModifierToken(token) &&
               (token.Value.Equals("encoding", StringComparison.OrdinalIgnoreCase) ||
                token.Value.Equals("culture", StringComparison.OrdinalIgnoreCase) ||
                token.Value.Equals("format", StringComparison.OrdinalIgnoreCase) ||
                token.Value.Equals("trim", StringComparison.OrdinalIgnoreCase) ||
                token.Value.Equals("source", StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsTableColumnModifierToken(Token token)
    {
        return token.TokenType is TokenType.Identifier or TokenType.Word or TokenType.Function or TokenType.Trim;
    }
}
