using Musoq.Parser.Diagnostics;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Nodes.InterpretationSchema;
using Musoq.Parser.Tokens;

namespace Musoq.Parser;

public partial class SchemaParser
{
    private TextFieldDefinitionNode ComposeBetweenField(string name)
    {
        Consume(TokenType.Between);
        var openDelimiter = ComposeStringLiteral();
        var closeDelimiter = ComposeStringLiteral();
        var modifiers = ComposeTextFieldModifiers();

        string? escapeChar = null;
        if ((modifiers & TextFieldModifier.Escaped) != 0 &&
            Current.TokenType is TokenType.Word or TokenType.StringLiteral)
        {
            var escapeToken = Current;
            escapeChar = ComposeStringLiteral();
            if (escapeChar.Length != 1)
                throw new SyntaxException(
                    $"The escape character for text field '{name}' must contain exactly one character.",
                    _lexer.AlreadyResolvedQueryPart,
                    DiagnosticCode.MQ4002_InvalidTextSchemaField,
                    escapeToken.Span);
        }

        return new TextFieldDefinitionNode(
            name, TextFieldType.Between, openDelimiter, closeDelimiter, modifiers, escapeChar);
    }
}
