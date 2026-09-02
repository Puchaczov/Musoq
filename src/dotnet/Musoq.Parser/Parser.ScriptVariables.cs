using Musoq.Parser.Diagnostics;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Nodes;
using Musoq.Parser.Tokens;

namespace Musoq.Parser;

public partial class Parser
{
    private bool IsScriptVariableDeclarationStart()
    {
        return (Current.TokenType is TokenType.Identifier or TokenType.Word or TokenType.Function) &&
               Current.Value.Equals("let", StringComparison.OrdinalIgnoreCase);
    }

    private ScriptVariableDeclarationNode ComposeScriptVariableDeclaration()
    {
        var start = ConsumeAndGetToken(Current.TokenType);
        var name = ConsumeScriptVariableName();

        if (Current.TokenType != TokenType.Colon)
        {
            var example = CreateScriptVariableDeclarationExample(name);
            throw new SyntaxException(
                $"Invalid script variable declaration near '{name.Value}'. Use '{example}' with the variable name before the type and a colon between them.",
                _lexer.AlreadyResolvedQueryPart,
                DiagnosticCode.MQ2033_InvalidScriptVariableDeclaration,
                name.Span.Through(Current.Span));
        }

        Consume(TokenType.Colon);
        var type = ConsumeScriptVariableTypeName();

        var isNullable = false;
        if (Current.TokenType == TokenType.QuestionMark)
        {
            isNullable = true;
            Consume(TokenType.QuestionMark);
        }

        if (Current.TokenType != TokenType.Equality)
        {
            throw new SyntaxException(
                $"Script variable '{name.Value}' must declare an initializer. Use 'let {name.Value}: {type.Value} = value'.",
                _lexer.AlreadyResolvedQueryPart,
                DiagnosticCode.MQ2033_InvalidScriptVariableDeclaration,
                name.Span.Through(Current.Span));
        }

        Consume(TokenType.Equality);
        var initializer = ComposeOperations();
        var span = start.Span.Through(initializer is AccessMethodNode accessMethod && accessMethod.Arguments.HasSpan ? accessMethod.FunctionToken.Span.Through(accessMethod.Arguments.Span) : initializer.Span);

        return new ScriptVariableDeclarationNode(name.Value, type.Value, isNullable, initializer, span);
    }

    private Token ConsumeScriptVariableName()
    {
        if (Current.TokenType is TokenType.Identifier or TokenType.Word)
            return ConsumeAndGetToken(Current.TokenType);

        throw new SyntaxException(
            $"Expected script variable name but received {Current.TokenType}. Use Musoq syntax: let name: type = value.",
            _lexer.AlreadyResolvedQueryPart,
            DiagnosticCode.MQ2033_InvalidScriptVariableDeclaration,
            Current.Span);
    }

    private Token ConsumeScriptVariableTypeName()
    {
        if ((Current.TokenType is TokenType.Identifier or TokenType.Word) || IsSchemaKeywordToken(Current.TokenType))
            return ConsumeAndGetToken(Current.TokenType);

        throw new SyntaxException(
            $"Expected script variable type name but received {Current.TokenType}. Use Musoq syntax: let name: type = value.",
            _lexer.AlreadyResolvedQueryPart,
            DiagnosticCode.MQ2033_InvalidScriptVariableDeclaration,
            Current.Span);
    }

    private string CreateScriptVariableDeclarationExample(Token firstToken)
    {
        if (Current.TokenType is not (TokenType.Identifier or TokenType.Word))
            return $"let {firstToken.Value}: type = value";

        if (IsLikelyParameterTypeName(firstToken.Value))
            return $"let {Current.Value}: {firstToken.Value} = value";

        return $"let {firstToken.Value}: {Current.Value} = value";
    }
}
