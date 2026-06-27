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

    private BinarySchemaNode ComposeBinarySchema()
    {
        Consume(TokenType.Binary);
        return ComposeBinarySchemaBody();
    }

    private BinarySchemaNode ComposeBinarySchemaBody()
    {
        var name = ComposeIdentifierOrWord();
        var typeParameters = ComposeOptionalTypeParameters();
        var extends = ComposeOptionalExtends();

        Consume(TokenType.LBracket);
        var fields = ComposeBinaryFieldList();
        Consume(TokenType.RBracket);

        ValidateBinarySwitchSelectors(fields, extends is not null);

        return new BinarySchemaNode(name, fields, extends, typeParameters);
    }

    private SchemaFieldNode[] ComposeBinaryFieldList()
    {
        var fields = new List<SchemaFieldNode>();

        while (Current.TokenType != TokenType.RBracket && Current.TokenType != TokenType.EndOfFile)
        {
            fields.Add(ComposeBinaryFieldOrComputed());

            if (Current.TokenType == TokenType.Comma)
                Consume(TokenType.Comma);
            else if (Current.TokenType != TokenType.RBracket)
                throw new SyntaxException(
                    $"Expected ',' or '}}' after field definition, but found '{Current.TokenType}'",
                    _lexer.AlreadyResolvedQueryPart);
        }

        return fields.ToArray();
    }

    private SchemaFieldNode ComposeBinaryFieldOrComputed()
    {
        var name = ComposeIdentifierOrWord();
        Consume(TokenType.Colon);

        if (Current.TokenType == TokenType.Equality)
        {
            Consume(TokenType.Equality);
            var expression = ComposeExpression();
            return new ComputedFieldNode(name, expression);
        }

        if (IsComputedFieldStart())
        {
            var expression = ComposeExpression();
            return new ComputedFieldNode(name, expression);
        }

        var typeAnnotation = ComposeTypeAnnotation(name);

        if (Current.TokenType == TokenType.Repeat) typeAnnotation = ComposeRepeatUntilType(typeAnnotation, name);

        var valueValidation = ComposeOptionalFieldValueValidation(typeAnnotation, name);
        var atOffset = ComposeOptionalAtOffset();
        var whenCondition = ComposeOptionalWhenCondition();
        var constraint = ComposeOptionalConstraint();

        return new FieldDefinitionNode(name, typeAnnotation, constraint, atOffset, whenCondition, valueValidation);
    }

    private bool IsComputedFieldStart()
    {
        if (IsTypeKeyword(Current.TokenType))
            return false;

        if (Current.TokenType == TokenType.Switch)
            return false;

        if (Current.TokenType == TokenType.Substream)
            return false;

        if (Current.TokenType is TokenType.Identifier or TokenType.Word)
        {
            var value = Current.Value.ToUpperInvariant();
            if (value is "BYTE" or "SBYTE" or "SHORT" or "USHORT" or "INT" or "UINT"
                or "LONG" or "ULONG" or "FLOAT" or "DOUBLE" or "STRING" or "BITS" or "ALIGN")
                return false;

            var nextType = PeekNextTokenType();
            if (nextType is TokenType.LittleEndian or TokenType.BigEndian or TokenType.LeftSquareBracket
                or TokenType.Less or TokenType.At or TokenType.Check or TokenType.When or TokenType.Repeat
                or TokenType.Comma or TokenType.RBracket)
                return false;

            if (MatchValidationKeyword(_peekedToken) is not null)
                return false;

            return true;
        }

        if (Current.TokenType is TokenType.Integer or TokenType.Decimal or TokenType.LeftParenthesis
            or TokenType.Hyphen or TokenType.True or TokenType.False)
            return true;

        if (AllowedKeywordTokenTypes.Contains(Current.TokenType))
        {
            var nextType = PeekNextTokenType();
            if (nextType is TokenType.LittleEndian or TokenType.BigEndian or TokenType.LeftSquareBracket
                or TokenType.Less or TokenType.At or TokenType.Check or TokenType.When or TokenType.Repeat
                or TokenType.Comma or TokenType.RBracket)
                return false;

            if (MatchValidationKeyword(_peekedToken) is not null)
                return false;

            return true;
        }

        return false;
    }

    private FieldDefinitionNode ComposeBinaryField()
    {
        var name = ComposeIdentifierOrWord();
        Consume(TokenType.Colon);

        var typeAnnotation = ComposeTypeAnnotation(name);

        if (Current.TokenType == TokenType.Repeat) typeAnnotation = ComposeRepeatUntilType(typeAnnotation, name);

        var valueValidation = ComposeOptionalFieldValueValidation(typeAnnotation, name);
        var atOffset = ComposeOptionalAtOffset();
        var whenCondition = ComposeOptionalWhenCondition();
        var constraint = ComposeOptionalConstraint();

        return new FieldDefinitionNode(name, typeAnnotation, constraint, atOffset, whenCondition, valueValidation);
    }
}
