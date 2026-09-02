using System.Collections.Generic;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Tokens;

namespace Musoq.Parser;

public partial class SchemaParser
{
    private string[]? ComposeOptionalTypeParameters()
    {
        if (Current.TokenType != TokenType.Less)
            return null;

        Consume(TokenType.Less);

        var typeParams = new List<string>();
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        AddGenericTypeParameter(typeParams, names);

        while (Current.TokenType == TokenType.Comma)
        {
            Consume(TokenType.Comma);
            AddGenericTypeParameter(typeParams, names);
        }

        Consume(TokenType.Greater);

        return typeParams.ToArray();
    }

    private void AddGenericTypeParameter(List<string> typeParams, HashSet<string> names)
    {
        if (Current.TokenType is not (TokenType.Identifier or TokenType.Word))
            throw new SyntaxException(
                $"Expected a generic type parameter identifier but found '{Current.TokenType}' ({Current.Value})",
                _lexer.AlreadyResolvedQueryPart,
                DiagnosticCode.MQ2012_InvalidSchemaDefinition,
                Current.Span);

        var token = ConsumeAndGetToken(Current.TokenType);
        if (!names.Add(token.Value))
            throw new SyntaxException(
                $"Generic type parameter '{token.Value}' is declared more than once.",
                _lexer.AlreadyResolvedQueryPart,
                DiagnosticCode.MQ2012_InvalidSchemaDefinition,
                token.Span);

        typeParams.Add(token.Value);
    }

    private string? ComposeOptionalExtends()
    {
        return ComposeOptionalExtends(out _);
    }

    private string? ComposeOptionalExtends(out TextSpan extendsSpan)
    {
        extendsSpan = TextSpan.Empty;
        if (Current.TokenType != TokenType.Extends)
            return null;

        Consume(TokenType.Extends);
        var nameToken = Current;
        var name = ComposeIdentifierOrWord();
        extendsSpan = nameToken.Span;
        return name;
    }
}
