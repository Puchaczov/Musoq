using Musoq.Parser.Nodes.InterpretationSchema;
using Musoq.Parser.Tokens;

namespace Musoq.Parser;

public partial class SchemaParser
{
    private bool IsTextSchemaReferenceStart()
    {
        return Current.TokenType is TokenType.Identifier or TokenType.Word;
    }

    private TextFieldDefinitionNode ComposeSchemaReferenceField(string name)
    {
        var schemaToken = Current;
        var schemaName = ComposeIdentifierOrWord();
        var modifiers = ComposeTextFieldModifiers();

        return (TextFieldDefinitionNode)new TextFieldDefinitionNode(
            name,
            TextFieldType.SchemaReference,
            schemaName,
            modifiers: modifiers).WithSpan(schemaToken.Span);
    }
}
