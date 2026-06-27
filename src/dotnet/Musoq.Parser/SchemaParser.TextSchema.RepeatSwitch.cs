using System.Collections.Generic;
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

        while (Current.TokenType != TokenType.RBracket)
        {
            TextSwitchCaseNode switchCase;

            if (Current.TokenType is TokenType.Word or TokenType.Identifier &&
                Current.Value == "_")
            {
                Consume(Current.TokenType);
                Consume(TokenType.FatArrow);
                var defaultTypeName = ComposeIdentifierOrWord();
                switchCase = new TextSwitchCaseNode(null, defaultTypeName);
            }
            else
            {
                Consume(TokenType.Pattern);
                var pattern = ComposeStringLiteral();
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
