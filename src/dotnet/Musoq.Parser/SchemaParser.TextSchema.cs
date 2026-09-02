using System.Collections.Generic;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Nodes.InterpretationSchema;
using Musoq.Parser.Tokens;

namespace Musoq.Parser;

/// <summary>
///     Parser for binary and text schema definitions.
///     Handles the interpretation schema syntax for defining data formats.
/// </summary>
public partial class SchemaParser
{

    private TextSchemaNode ComposeTextSchema()
    {
        var textToken = ConsumeAndGetToken(TokenType.Text);
        return ComposeTextSchemaBody(textToken.Span);
    }

    private TextSchemaNode ComposeTextSchemaBody(TextSpan schemaStartSpan = default)
    {
        var name = ComposeIdentifierOrWord();
        var extends = ComposeOptionalExtends(out var extendsSpan);

        Consume(TokenType.LBracket);
        var fields = ComposeTextFieldList();
        var closingToken = ConsumeAndGetToken(TokenType.RBracket);

        ValidateTextFieldNames(fields);
        ValidateTextFieldModifiers(fields);

        var schema = new TextSchemaNode(
            name,
            fields,
            extends,
            extendsSpan,
            GetSchemaComments(schemaStartSpan.Through(closingToken.Span)));

        return (TextSchemaNode)schema.WithSpan(schemaStartSpan.Through(closingToken.Span));
    }

    private void ValidateTextFieldNames(IEnumerable<TextFieldDefinitionNode> fields)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var field in fields)
        {
            if (field.IsDiscard || names.Add(field.Name))
                continue;

            throw new SyntaxException(
                $"Text schema field '{field.Name}' is declared more than once.",
                _lexer.AlreadyResolvedQueryPart,
                DiagnosticCode.MQ4008_DuplicateSchemaField,
                field.Span);
        }
    }

    private TextFieldDefinitionNode[] ComposeTextFieldList()
    {
        var fields = new List<TextFieldDefinitionNode>();

        while (Current.TokenType != TokenType.RBracket && Current.TokenType != TokenType.EndOfFile)
        {
            fields.Add(ComposeTextField());

            if (Current.TokenType == TokenType.Comma)
                Consume(TokenType.Comma);
            else if (Current.TokenType != TokenType.RBracket)
                throw new SyntaxException(
                    $"Expected ',' or '}}' after field definition, but found '{Current.TokenType}'",
                    _lexer.AlreadyResolvedQueryPart);
        }

        return fields.ToArray();
    }

}
