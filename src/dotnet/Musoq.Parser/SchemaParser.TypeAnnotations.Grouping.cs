using Musoq.Parser.Nodes.InterpretationSchema;
using Musoq.Parser.Tokens;

namespace Musoq.Parser;

public partial class SchemaParser
{
    private TypeAnnotationNode ComposeGroupedBinaryType(string? fieldName)
    {
        Consume(TokenType.LeftParenthesis);
        var type = ComposeTypeAnnotation(fieldName);
        Consume(TokenType.RightParenthesis);
        return type;
    }

    private bool IsGroupedBinaryTypeStart()
    {
        var distance = 1;
        while (PeekSchemaToken(distance).TokenType == TokenType.LeftParenthesis)
            distance++;
        var first = PeekSchemaToken(distance).TokenType;
        if (IsTypeKeyword(first) || first is TokenType.Switch or TokenType.Substream or TokenType.LBracket)
            return true;
        if (first is not (TokenType.Identifier or TokenType.Word))
            return false;
        distance++;
        var genericDepth = 0;
        while (PeekSchemaToken(distance).TokenType is TokenType.Less or TokenType.Greater or TokenType.RightShift
               or TokenType.Identifier or TokenType.Word or TokenType.Comma)
        {
            var token = PeekSchemaToken(distance).TokenType;
            genericDepth += token switch { TokenType.Less => 1, TokenType.Greater => -1, TokenType.RightShift => -2, _ => 0 };
            if (genericDepth <= 0 && token is not (TokenType.Greater or TokenType.RightShift))
                break;
            distance++;
        }
        return PeekSchemaToken(distance).TokenType == TokenType.Repeat;
    }
}
