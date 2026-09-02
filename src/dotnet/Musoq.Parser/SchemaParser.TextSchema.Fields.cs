using System.Globalization;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Nodes.InterpretationSchema;
using Musoq.Parser.Tokens;

namespace Musoq.Parser;

public partial class SchemaParser
{
    private TextFieldDefinitionNode ComposeTextField()
    {
        var nameToken = Current;
        var name = ComposeIdentifierOrWord();
        Consume(TokenType.Colon);

        var isOptionalPrefix = Current.TokenType == TokenType.Optional;
        if (isOptionalPrefix) Consume(TokenType.Optional);

        var field = IsTextSchemaReferenceStart()
            ? ComposeSchemaReferenceField(name)
            : Current.TokenType switch
            {
                TokenType.Pattern => ComposePatternField(name),
                TokenType.Literal => ComposeLiteralField(name),
                TokenType.Until => ComposeUntilField(name),
                TokenType.Between => ComposeBetweenField(name),
                TokenType.Chars => ComposeCharsField(name),
                TokenType.Token => ComposeTokenField(name),
                TokenType.Rest => ComposeRestField(name),
                TokenType.Whitespace => ComposeWhitespaceField(name),
                TokenType.Repeat => ComposeRepeatField(name),
                TokenType.Switch => ComposeSwitchField(name),
                _ => throw new SyntaxException(
                    $"Expected text field type (pattern, literal, until, between, chars, token, rest, whitespace, repeat, switch, or a schema reference) but found '{Current.TokenType}'",
                    _lexer.AlreadyResolvedQueryPart)
            };

        if (isOptionalPrefix)
            field = field.FieldType == TextFieldType.Switch
                ? new TextFieldDefinitionNode(
                    field.Name,
                    field.SwitchCases,
                    field.Modifiers | TextFieldModifier.Optional)
                : new TextFieldDefinitionNode(
                    field.Name,
                    field.FieldType,
                    field.PrimaryValue,
                    field.SecondaryValue,
                    field.Modifiers | TextFieldModifier.Optional,
                    field.EscapeCharacter,
                    field.CaptureGroups);

        return (TextFieldDefinitionNode)field.WithSpan(nameToken.Span);
    }

    private TextFieldDefinitionNode ComposePatternField(string name)
    {
        Consume(TokenType.Pattern);
        var patternToken = Current;
        var pattern = ComposeStringLiteral();
        var captureGroups = ComposeOptionalCaptureGroups();
        if (!TextPatternValidator.TryValidate(pattern, captureGroups, out var validationError))
            throw new SyntaxException(
                $"Invalid pattern for text field '{name}': {validationError}",
                _lexer.AlreadyResolvedQueryPart,
                DiagnosticCode.MQ4002_InvalidTextSchemaField,
                patternToken.Span);

        var modifiers = ComposeTextFieldModifiers();

        return new TextFieldDefinitionNode(
            name, TextFieldType.Pattern, pattern, null, modifiers, null, captureGroups);
    }

    private TextFieldDefinitionNode ComposeLiteralField(string name)
    {
        Consume(TokenType.Literal);
        var literal = ComposeStringLiteral();
        var modifiers = ComposeTextFieldModifiers();

        return new TextFieldDefinitionNode(
            name, TextFieldType.Literal, literal, null, modifiers);
    }

    private TextFieldDefinitionNode ComposeUntilField(string name)
    {
        Consume(TokenType.Until);
        var delimiter = ComposeStringLiteral();
        var modifiers = ComposeTextFieldModifiers();

        return new TextFieldDefinitionNode(
            name, TextFieldType.Until, delimiter, null, modifiers);
    }

    private TextFieldDefinitionNode ComposeCharsField(string name)
    {
        Consume(TokenType.Chars);
        Consume(TokenType.LeftSquareBracket);

        if (Current.TokenType != TokenType.Integer)
            throw new SyntaxException(
                "chars[] requires an integer count",
                _lexer.AlreadyResolvedQueryPart);

        var countToken = ConsumeAndGetToken(TokenType.Integer);
        var countStr = countToken.Value;

        if (!int.TryParse(countStr, NumberStyles.None, CultureInfo.InvariantCulture, out var count) || count < 0)
            throw new SyntaxException(
                $"chars[] size must be a non-negative 32-bit integer, but got {countStr}.",
                _lexer.AlreadyResolvedQueryPart,
                DiagnosticCode.MQ4002_InvalidTextSchemaField,
                countToken.Span);

        Consume(TokenType.RightSquareBracket);

        var modifiers = ComposeTextFieldModifiers();

        return new TextFieldDefinitionNode(
            name, TextFieldType.Chars, countStr, null, modifiers);
    }

    private TextFieldDefinitionNode ComposeTokenField(string name)
    {
        Consume(TokenType.Token);
        var modifiers = ComposeTextFieldModifiers();

        return new TextFieldDefinitionNode(
            name, TextFieldType.Token, null, null, modifiers);
    }

    private TextFieldDefinitionNode ComposeRestField(string name)
    {
        Consume(TokenType.Rest);
        var modifiers = ComposeTextFieldModifiers();

        return new TextFieldDefinitionNode(
            name, TextFieldType.Rest, null, null, modifiers);
    }

    private TextFieldDefinitionNode ComposeWhitespaceField(string name)
    {
        Consume(TokenType.Whitespace);

        var quantifier = "+";
        if (Current.TokenType == TokenType.Plus)
        {
            Consume(TokenType.Plus);
            quantifier = "+";
        }
        else if (Current.TokenType == TokenType.Star)
        {
            Consume(TokenType.Star);
            quantifier = "*";
        }
        else if (Current.TokenType == TokenType.QuestionMark ||
                 Current is { TokenType: TokenType.Word, Value: "?" })
        {
            Consume(Current.TokenType);
            quantifier = "?";
        }

        var modifiers = ComposeTextFieldModifiers();

        return new TextFieldDefinitionNode(
            name, TextFieldType.Whitespace, quantifier, null, modifiers);
    }
}
