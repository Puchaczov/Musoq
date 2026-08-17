using System.Collections.Generic;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Nodes;
using Musoq.Parser.Tokens;

namespace Musoq.Parser;

public partial class Parser
{
    private bool IsParameterBlockStart()
    {
        return (Current.TokenType is TokenType.Identifier or TokenType.Word or TokenType.Function) &&
               (Current.Value.Equals("param", StringComparison.OrdinalIgnoreCase) || Current.Value.Equals("params", StringComparison.OrdinalIgnoreCase));
    }


    private ParameterBlockNode ComposeParameterBlock()
    {
        var start = ConsumeAndGetToken(Current.TokenType);
        Consume(TokenType.LeftParenthesis);

        var parameters = new List<ParameterDeclarationNode>();

        if (Current.TokenType != TokenType.RightParenthesis)
            do
            {
                if (Current.TokenType == TokenType.Comma)
                    Consume(TokenType.Comma);

                parameters.Add(ComposeParameterDeclaration());
            } while (Current.TokenType == TokenType.Comma);

        var end = ConsumeAndGetToken(TokenType.RightParenthesis);

        return new ParameterBlockNode(parameters.ToArray(), start.Span.Through(end.Span));
    }


    private ParameterDeclarationNode ComposeParameterDeclaration()
    {
        var name = ConsumeParameterName();

        if (Current.TokenType == TokenType.ParameterReference)
            throw new SyntaxException(
                "PowerShell-style script parameter declarations are not supported. Use Musoq syntax: param(author: string) and reference it as $author.",
                _lexer.AlreadyResolvedQueryPart,
                DiagnosticCode.MQ2032_UnsupportedScriptParameterSyntax,
                name.Span.Through(Current.Span));

        if (Current.TokenType != TokenType.Colon)
        {
            var example = CreateParameterDeclarationExample(name);
            throw new SyntaxException(
                $"Invalid script parameter declaration near '{name.Value}'. Use '{example}' with the parameter name before the type and a colon between them.",
                _lexer.AlreadyResolvedQueryPart,
                DiagnosticCode.MQ2031_InvalidScriptParameterDeclaration,
                name.Span.Through(Current.Span));
        }

        Consume(TokenType.Colon);
        var type = ConsumeParameterTypeName();
        var typeName = type.Value;
        var typeSpan = type.Span;

        if (Current.TokenType == TokenType.LeftSquareBracket)
        {
            Consume(TokenType.LeftSquareBracket);
            var rightBracket = ConsumeAndGetToken(TokenType.RightSquareBracket);
            typeName = $"{typeName}[]";
            typeSpan = typeSpan.Through(rightBracket.Span);
        }

        var isNullable = false;
        TextSpan? nullableSpan = null;
        if (Current.TokenType == TokenType.QuestionMark)
        {
            isNullable = true;
            nullableSpan = ConsumeAndGetToken(TokenType.QuestionMark).Span;
        }

        Node? defaultValue = null;
        if (Current.TokenType == TokenType.Equality)
        {
            Consume(TokenType.Equality);
            defaultValue = ComposeParameterDefaultValue();
        }

        var span = name.Span.Through(defaultValue?.Span ?? nullableSpan ?? typeSpan);
        return new ParameterDeclarationNode(name.Value, typeName, isNullable, defaultValue, span);
    }


    private string CreateParameterDeclarationExample(Token firstToken)
    {
        if (Current.TokenType is not (TokenType.Identifier or TokenType.Word))
            return $"param({firstToken.Value}: type)";

        if (IsLikelyParameterTypeName(firstToken.Value))
            return $"param({Current.Value}: {firstToken.Value})";

        return $"param({firstToken.Value}: {Current.Value})";
    }


    private static bool IsLikelyParameterTypeName(string value)
    {
        return ScriptParameterTypeCatalog.IsKnownScalarTypeName(value);
    }


    private Token ConsumeParameterName()
    {
        if (Current.TokenType is TokenType.Identifier or TokenType.Word)
            return ConsumeAndGetToken(Current.TokenType);

        if (Current.TokenType == TokenType.LeftSquareBracket)
            throw new SyntaxException(
                "PowerShell-style script parameter declarations are not supported. Use Musoq syntax: param(author: string) and reference it as $author.",
                _lexer.AlreadyResolvedQueryPart,
                DiagnosticCode.MQ2032_UnsupportedScriptParameterSyntax,
                Current.Span);

        throw new SyntaxException(
            $"Expected script parameter name but received {Current.TokenType}. Use Musoq syntax: param(author: string).",
            _lexer.AlreadyResolvedQueryPart,
            DiagnosticCode.MQ2031_InvalidScriptParameterDeclaration,
            Current.Span);
    }


    private Token ConsumeParameterTypeName()
    {
        if ((Current.TokenType is TokenType.Identifier or TokenType.Word) || IsSchemaKeywordToken(Current.TokenType))
            return ConsumeAndGetToken(Current.TokenType);

        throw new SyntaxException(
            $"Expected script parameter type name but received {Current.TokenType}. Use Musoq syntax: param(author: string).",
            _lexer.AlreadyResolvedQueryPart,
            DiagnosticCode.MQ2031_InvalidScriptParameterDeclaration,
            Current.Span);
    }


    private Node ComposeParameterDefaultValue()
    {
        switch (Current.TokenType)
        {
            case TokenType.Decimal:
                var token = ConsumeAndGetToken(TokenType.Decimal);
                return new DecimalNode(token.Value, token.Span);
            case TokenType.Integer:
                return ComposeInteger();
            case TokenType.HexadecimalInteger:
                return ComposeHexInteger();
            case TokenType.BinaryInteger:
                return ComposeBinaryInteger();
            case TokenType.OctalInteger:
                return ComposeOctalInteger();
            case TokenType.StringLiteral:
                return ComposeWord();
            case TokenType.True:
                token = ConsumeAndGetToken(TokenType.True);
                return new BooleanNode(true, token.Span);
            case TokenType.False:
                token = ConsumeAndGetToken(TokenType.False);
                return new BooleanNode(false, token.Span);
            case TokenType.Null:
                token = ConsumeAndGetToken(TokenType.Null);
                return new NullNode(token.Span);
            default:
                throw new SyntaxException(
                    $"Parameter default values support only primitive constants or null. Received {Current.TokenType}.",
                    _lexer.AlreadyResolvedQueryPart,
                    DiagnosticCode.MQ2031_InvalidScriptParameterDeclaration,
                    Current.Span);
        }
    }

}
