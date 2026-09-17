using System.Collections.Generic;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Nodes;
using Musoq.Parser.Tokens;

namespace Musoq.Parser;

public partial class Parser
{
    private StructuralTypeSyntaxNode ComposeStructuralTypeSyntax(bool allowFieldDefaults = true)
    {
        var type = ComposeStructuralTypePrimary(allowFieldDefaults);

        while (true)
        {
            if (Current.TokenType == TokenType.QuestionMark)
            {
                var nullable = ConsumeAndGetToken(TokenType.QuestionMark);
                type = type.WithNullable(type.Span.Through(nullable.Span));
                continue;
            }

            if (Current.TokenType == TokenType.LeftSquareBracket)
            {
                Consume(TokenType.LeftSquareBracket);
                var closing = ConsumeAndGetToken(TokenType.RightSquareBracket);
                type = StructuralTypeSyntaxNode.Collection(type, type.Span.Through(closing.Span));
                continue;
            }

            break;
        }

        return type;
    }

    private StructuralTypeSyntaxNode ComposeStructuralTypePrimary(bool allowFieldDefaults)
    {
        if (Current.TokenType == TokenType.LeftParenthesis)
        {
            var opening = ConsumeAndGetToken(TokenType.LeftParenthesis);
            var fields = new List<StructuralTypeFieldSyntax>();

            if (Current.TokenType == TokenType.RightParenthesis)
                throw new SyntaxException(
                    "A structural record type must contain at least one named field.",
                    _lexer.AlreadyResolvedQueryPart,
                    DiagnosticCode.MQ2031_InvalidScriptParameterDeclaration,
                    Current.Span);

            while (true)
            {
                if (!IsStructuralFieldNameToken(Current.TokenType))
                    throw new SyntaxException(
                        "A structural type field must start with a field name.",
                        _lexer.AlreadyResolvedQueryPart,
                        DiagnosticCode.MQ2031_InvalidScriptParameterDeclaration,
                        Current.Span);

                var name = ConsumeAndGetToken(Current.TokenType);
                Consume(TokenType.Colon);
                var fieldType = ComposeStructuralTypeSyntax(allowFieldDefaults);
                Node? defaultValue = null;
                if (allowFieldDefaults && Current.TokenType == TokenType.Equality)
                {
                    Consume(TokenType.Equality);
                    defaultValue = ComposeParameterDefaultValue();
                }

                var fieldEnd = defaultValue?.Span ?? fieldType.Span;
                fields.Add(new StructuralTypeFieldSyntax(
                    name.Value,
                    fieldType,
                    defaultValue,
                    name.Span,
                    name.Span.Through(fieldEnd)));

                if (Current.TokenType != TokenType.Comma)
                    break;

                Consume(TokenType.Comma);
                if (Current.TokenType == TokenType.RightParenthesis)
                    break;
            }

            var closing = ConsumeAndGetToken(TokenType.RightParenthesis);
            return StructuralTypeSyntaxNode.Record(fields, opening.Span.Through(closing.Span));
        }

        var token = ConsumeParameterTypeName();
        return StructuralTypeSyntaxNode.Scalar(token.Value, token.Span);
    }

    private static bool TryGetLegacyTypeParts(
        StructuralTypeSyntaxNode type,
        out string typeName,
        out bool isNullable)
    {
        if (type.Kind == StructuralTypeSyntaxKind.Scalar)
        {
            typeName = type.ScalarName!;
            isNullable = type.IsNullable;
            return true;
        }

        if (type.Kind == StructuralTypeSyntaxKind.Collection &&
            type.ElementType is { Kind: StructuralTypeSyntaxKind.Scalar, IsNullable: false } element)
        {
            typeName = $"{element.ScalarName}[]";
            isNullable = type.IsNullable;
            return true;
        }

        typeName = type.ToString();
        isNullable = type.IsNullable;
        return false;
    }

    private Node ComposeParameterDefaultValueOrStructured()
    {
        if (IsContextualArrayLiteralStart() || IsParenthesizedRecordLiteralStart())
            return ComposeOperations();

        return ComposeParameterDefaultValue();
    }
}
