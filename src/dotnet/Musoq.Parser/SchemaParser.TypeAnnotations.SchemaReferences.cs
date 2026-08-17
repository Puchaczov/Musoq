using System.Collections.Generic;
using Musoq.Parser.Nodes.InterpretationSchema;
using Musoq.Parser.Tokens;

namespace Musoq.Parser;

public partial class SchemaParser
{
    private InlineSchemaTypeNode ComposeInlineSchema()
    {
        Consume(TokenType.LBracket);

        var fields = new List<SchemaFieldNode>();

        if (Current.TokenType != TokenType.RBracket)
        {
            fields.Add(ComposeBinaryField());

            while (Current.TokenType == TokenType.Comma)
            {
                Consume(TokenType.Comma);

                if (Current.TokenType == TokenType.RBracket)
                    break;
                fields.Add(ComposeBinaryField());
            }
        }

        Consume(TokenType.RBracket);

        return new InlineSchemaTypeNode(fields.ToArray());
    }

    private TypeAnnotationNode ComposeInlineSchemaOrArray()
    {
        var inlineSchema = ComposeInlineSchema();

        if (Current.TokenType == TokenType.LeftSquareBracket)
            return ComposeArrayOfType(inlineSchema);

        return inlineSchema;
    }

    private TypeAnnotationNode ComposeSchemaReferenceOrArray()
    {
        var schemaName = ComposeIdentifierOrWord();

        string[]? typeArguments = null;
        if (Current.TokenType == TokenType.Less) typeArguments = ComposeTypeArguments();

        var schemaRef = new SchemaReferenceTypeNode(schemaName, typeArguments);

        if (Current.TokenType == TokenType.LeftSquareBracket) return ComposeArrayOfType(schemaRef);

        return schemaRef;
    }

    private string[] ComposeTypeArguments()
    {
        Consume(TokenType.Less);

        var typeArgs = new List<string> { ComposeTypeArgument() };

        while (Current.TokenType == TokenType.Comma)
        {
            Consume(TokenType.Comma);
            typeArgs.Add(ComposeTypeArgument());
        }

        ConsumeGenericGreater();

        return typeArgs.ToArray();
    }

    private string ComposeTypeArgument()
    {
        var typeName = ComposeIdentifierOrWord();

        if (Current.TokenType != TokenType.Less)
            return typeName;

        var typeArguments = ComposeTypeArguments();
        return $"{typeName}<{string.Join(", ", typeArguments)}>";
    }

    private ArrayTypeNode ComposeArrayOfType(TypeAnnotationNode elementType)
    {
        Consume(TokenType.LeftSquareBracket);
        var sizeExpr = ComposeSizeExpression();
        Consume(TokenType.RightSquareBracket);

        return new ArrayTypeNode(elementType, sizeExpr);
    }

    private RepeatUntilTypeNode ComposeRepeatUntilType(TypeAnnotationNode elementType, string fieldName)
    {
        Consume(TokenType.Repeat);
        Consume(TokenType.Until);

        if (IsEndOfInputSentinel())
        {
            Consume(Current.TokenType);
            return RepeatUntilTypeNode.EndOfInput(elementType, fieldName);
        }

        var condition = ComposeExpression();

        return new RepeatUntilTypeNode(elementType, condition, fieldName);
    }

    /// <summary>
    ///     Detects a standalone <c>eof</c> sentinel after <c>repeat until</c>. The token is treated
    ///     as a sentinel only when it is not immediately followed by an expression continuation, so a
    ///     field literally named <c>eof</c> still parses as a condition (for example <c>eof = 0</c>).
    /// </summary>
    private bool IsEndOfInputSentinel()
    {
        if (Current.TokenType is not (TokenType.Identifier or TokenType.Word))
            return false;

        if (!string.Equals(Current.Value, "eof", StringComparison.OrdinalIgnoreCase))
            return false;

        return PeekNextTokenType() is TokenType.Comma or TokenType.RBracket or TokenType.EndOfFile
            or TokenType.At or TokenType.When or TokenType.Check
            or TokenType.Identifier or TokenType.Word;
    }
}
