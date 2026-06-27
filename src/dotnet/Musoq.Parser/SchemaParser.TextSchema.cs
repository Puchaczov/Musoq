using System.Collections.Generic;
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
        Consume(TokenType.Text);
        return ComposeTextSchemaBody();
    }

    private TextSchemaNode ComposeTextSchemaBody()
    {
        var name = ComposeIdentifierOrWord();
        var extends = ComposeOptionalExtends();

        Consume(TokenType.LBracket);
        var fields = ComposeTextFieldList();
        Consume(TokenType.RBracket);

        return new TextSchemaNode(name, fields, extends);
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
