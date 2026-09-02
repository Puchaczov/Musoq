using System.Collections.Generic;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Nodes.InterpretationSchema;
using Musoq.Parser.Tokens;

namespace Musoq.Parser;

public partial class SchemaParser
{
    private TextFieldDefinitionNode ComposeRepeatField(string name)
    {
        Consume(TokenType.Repeat);

        var schemaName = ComposeIdentifierOrWord();

        string? untilDelimiter = null;
        if (Current.TokenType == TokenType.Until)
        {
            Consume(TokenType.Until);

            if (Current.TokenType == TokenType.End)
                Consume(TokenType.End);
            else
                untilDelimiter = ComposeStringLiteral();
        }

        return new TextFieldDefinitionNode(
            name, TextFieldType.Repeat, schemaName, untilDelimiter);
    }

    private TextFieldDefinitionNode ComposeSwitchField(string name)
    {
        Consume(TokenType.Switch);
        Consume(TokenType.LBracket);

        var cases = new List<TextSwitchCaseNode>();
        var seenDefault = false;

        if (Current.TokenType == TokenType.RBracket)
            throw new SyntaxException(
                $"Text switch field '{name}' must contain at least one case.",
                _lexer.AlreadyResolvedQueryPart,
                DiagnosticCode.MQ4002_InvalidTextSchemaField,
                Current.Span);

        while (Current.TokenType != TokenType.RBracket)
        {
            TextSwitchCaseNode switchCase;

            var isDefault = Current.Value == "_" &&
                            (Current.TokenType is TokenType.Word or TokenType.Identifier or TokenType.Property);
            if (isDefault)
            {
                if (seenDefault)
                    throw new SyntaxException(
                        $"Text switch field '{name}' may contain only one default case.",
                        _lexer.AlreadyResolvedQueryPart,
                        DiagnosticCode.MQ4002_InvalidTextSchemaField,
                        Current.Span);

                seenDefault = true;
                Consume(Current.TokenType);
                Consume(TokenType.FatArrow);
                var defaultTypeName = ComposeIdentifierOrWord();
                switchCase = new TextSwitchCaseNode(null, defaultTypeName);
            }
            else
            {
                if (seenDefault)
                    throw new SyntaxException(
                        $"Text switch field '{name}' must place the default case after all pattern cases.",
                        _lexer.AlreadyResolvedQueryPart,
                        DiagnosticCode.MQ4002_InvalidTextSchemaField,
                        Current.Span);

                Consume(TokenType.Pattern);
                var patternToken = Current;
                var pattern = ComposeStringLiteral();
                if (!TextPatternValidator.TryValidate(pattern, Array.Empty<string>(), out var validationError))
                    throw new SyntaxException(
                        $"Invalid switch pattern for text field '{name}': {validationError}",
                        _lexer.AlreadyResolvedQueryPart,
                        DiagnosticCode.MQ4002_InvalidTextSchemaField,
                        patternToken.Span);

                Consume(TokenType.FatArrow);
                var typeName = ComposeIdentifierOrWord();
                switchCase = new TextSwitchCaseNode(pattern, typeName);
            }

            cases.Add(switchCase);

            if (Current.TokenType == TokenType.Comma) Consume(TokenType.Comma);
        }

        Consume(TokenType.RBracket);

        return new TextFieldDefinitionNode(name, cases.ToArray());
    }

    private string[] ComposeOptionalCaptureGroups()
    {
        if (Current.TokenType != TokenType.Capture)
            return Array.Empty<string>();

        Consume(TokenType.Capture);
        Consume(TokenType.LeftParenthesis);

        var groups = new List<string> { ComposeIdentifierOrWord() };

        while (Current.TokenType == TokenType.Comma)
        {
            Consume(TokenType.Comma);
            groups.Add(ComposeIdentifierOrWord());
        }

        Consume(TokenType.RightParenthesis);

        return groups.ToArray();
    }

    private TextFieldModifier ComposeTextFieldModifiers()
    {
        var modifiers = TextFieldModifier.None;

        while (true)
            switch (Current.TokenType)
            {
                case TokenType.Trim:
                    modifiers |= TextFieldModifier.Trim;
                    Consume(TokenType.Trim);
                    break;
                case TokenType.RTrim:
                    modifiers |= TextFieldModifier.RTrim;
                    Consume(TokenType.RTrim);
                    break;
                case TokenType.LTrim:
                    modifiers |= TextFieldModifier.LTrim;
                    Consume(TokenType.LTrim);
                    break;
                case TokenType.Nested:
                    modifiers |= TextFieldModifier.Nested;
                    Consume(TokenType.Nested);
                    break;
                case TokenType.Escaped:
                    modifiers |= TextFieldModifier.Escaped;
                    Consume(TokenType.Escaped);
                    break;
                case TokenType.Greedy:
                    modifiers |= TextFieldModifier.Greedy;
                    Consume(TokenType.Greedy);
                    break;
                case TokenType.Lazy:
                    modifiers |= TextFieldModifier.Lazy;
                    Consume(TokenType.Lazy);
                    break;
                case TokenType.Lower:
                    modifiers |= TextFieldModifier.Lower;
                    Consume(TokenType.Lower);
                    break;
                case TokenType.Upper:
                    modifiers |= TextFieldModifier.Upper;
                    Consume(TokenType.Upper);
                    break;
                case TokenType.Optional:
                    modifiers |= TextFieldModifier.Optional;
                    Consume(TokenType.Optional);
                    break;
                default:
                    return modifiers;
            }
    }
}
