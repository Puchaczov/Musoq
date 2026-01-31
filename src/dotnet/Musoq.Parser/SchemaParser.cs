#nullable enable
using System;
using System.Collections.Generic;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Lexing;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.InterpretationSchema;
using Musoq.Parser.Tokens;

namespace Musoq.Parser;

/// <summary>
///     Parser for binary and text schema definitions.
///     Handles the interpretation schema syntax for defining data formats.
/// </summary>
public class SchemaParser
{
    private readonly ILexer _lexer;
    private bool _hasReplacedToken;
    private Token? _peekedToken; // Token peeked ahead but not yet consumed

    // ReSharper disable once NotAccessedField.Local - Reserved for future token replacement support
    private Token? _replacedToken;
    private Token? _savedTokenBeforePeek; // Current token saved before peeking

    /// <summary>
    ///     Creates a new schema parser.
    /// </summary>
    /// <param name="lexer">The lexer to use for tokenization.</param>
    public SchemaParser(ILexer lexer)
    {
        _lexer = lexer ?? throw new ArgumentNullException(nameof(lexer));
    }

    private Token Current => _savedTokenBeforePeek ??
                             (_hasReplacedToken && _replacedToken != null ? _replacedToken : _lexer.Current());

    private Token? Previous { get; set; }

    /// <summary>
    ///     Parses a complete schema definition (binary or text).
    ///     Advances the lexer first before parsing.
    /// </summary>
    /// <returns>The parsed schema node.</returns>
    public Node ParseSchema()
    {
        _lexer.IsSchemaContext = true;

        try
        {
            _lexer.Next();

            return Current.TokenType switch
            {
                TokenType.Binary => ComposeBinarySchema(),
                TokenType.Text => ComposeTextSchema(),
                _ => throw new SyntaxException(
                    $"Expected 'binary' or 'text' keyword but found '{Current.TokenType}'",
                    _lexer.AlreadyResolvedQueryPart)
            };
        }
        finally
        {
            _lexer.IsSchemaContext = false;
        }
    }

    /// <summary>
    ///     Parses a schema definition starting from the current lexer position.
    ///     Used when the main parser has already positioned at 'binary' or 'text'.
    /// </summary>
    /// <returns>The parsed schema node.</returns>
    public Node ParseSchemaFromCurrentPosition()
    {
        _lexer.IsSchemaContext = true;

        try
        {
            var isBinary = Current.TokenType == TokenType.Binary ||
                           (Current.TokenType == TokenType.Identifier &&
                            Current.Value.Equals("binary", StringComparison.OrdinalIgnoreCase));
            var isText = Current.TokenType == TokenType.Text ||
                         (Current.TokenType == TokenType.Identifier &&
                          Current.Value.Equals("text", StringComparison.OrdinalIgnoreCase));

            if (isBinary)
            {
                Consume(Current.TokenType);
                return ComposeBinarySchemaBody();
            }

            if (isText)
            {
                Consume(Current.TokenType);
                return ComposeTextSchemaBody();
            }

            throw new SyntaxException(
                $"Expected 'binary' or 'text' keyword but found '{Current.TokenType}'",
                _lexer.AlreadyResolvedQueryPart);
        }
        finally
        {
            _lexer.IsSchemaContext = false;
        }
    }

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


        var typeAnnotation = ComposeTypeAnnotation();


        if (Current.TokenType == TokenType.Repeat) typeAnnotation = ComposeRepeatUntilType(typeAnnotation, name);

        var atOffset = ComposeOptionalAtOffset();
        var constraint = ComposeOptionalConstraint();
        var whenCondition = ComposeOptionalWhenCondition();

        return new FieldDefinitionNode(name, typeAnnotation, constraint, atOffset, whenCondition);
    }

    private bool IsComputedFieldStart()
    {
        if (IsTypeKeyword(Current.TokenType))
            return false;


        if (Current.TokenType is TokenType.Identifier or TokenType.Word)
        {
            var value = Current.Value.ToLowerInvariant();
            if (value is "byte" or "sbyte" or "short" or "ushort" or "int" or "uint"
                or "long" or "ulong" or "float" or "double" or "string" or "bits" or "align")
                return false;


            var nextType = PeekNextTokenType();
            if (nextType is TokenType.LittleEndian or TokenType.BigEndian or TokenType.LeftSquareBracket
                or TokenType.Less or TokenType.At or TokenType.Check or TokenType.When or TokenType.Repeat
                or TokenType.Comma or TokenType.RBracket)
                return false;


            return true;
        }


        if (Current.TokenType is TokenType.Integer or TokenType.Decimal or TokenType.LeftParenthesis
            or TokenType.Hyphen or TokenType.True or TokenType.False)
            return true;

        return false;
    }

    private static bool IsTypeKeyword(TokenType tokenType)
    {
        return tokenType is TokenType.ByteType or TokenType.SByteType or
            TokenType.ShortType or TokenType.UShortType or
            TokenType.IntType or TokenType.UIntType or
            TokenType.LongType or TokenType.ULongType or
            TokenType.FloatType or TokenType.DoubleType or
            TokenType.StringType or TokenType.BitsType or TokenType.Align;
    }

    private FieldDefinitionNode ComposeBinaryField()
    {
        var name = ComposeIdentifierOrWord();
        Consume(TokenType.Colon);

        var typeAnnotation = ComposeTypeAnnotation();


        if (Current.TokenType == TokenType.Repeat) typeAnnotation = ComposeRepeatUntilType(typeAnnotation, name);

        var atOffset = ComposeOptionalAtOffset();
        var constraint = ComposeOptionalConstraint();
        var whenCondition = ComposeOptionalWhenCondition();

        return new FieldDefinitionNode(name, typeAnnotation, constraint, atOffset, whenCondition);
    }

    private TypeAnnotationNode ComposeTypeAnnotation()
    {
        return Current.TokenType switch
        {
            TokenType.ByteType when PeekNextTokenType() == TokenType.LeftSquareBracket =>
                ComposeByteArrayType(),
            TokenType.ByteType => ComposePrimitiveType(PrimitiveTypeName.Byte, false),
            TokenType.SByteType => ComposePrimitiveType(PrimitiveTypeName.SByte, false),
            TokenType.ShortType => ComposePrimitiveType(PrimitiveTypeName.Short, true),
            TokenType.UShortType => ComposePrimitiveType(PrimitiveTypeName.UShort, true),
            TokenType.IntType => ComposePrimitiveType(PrimitiveTypeName.Int, true),
            TokenType.UIntType => ComposePrimitiveType(PrimitiveTypeName.UInt, true),
            TokenType.LongType => ComposePrimitiveType(PrimitiveTypeName.Long, true),
            TokenType.ULongType => ComposePrimitiveType(PrimitiveTypeName.ULong, true),
            TokenType.FloatType => ComposePrimitiveType(PrimitiveTypeName.Float, true),
            TokenType.DoubleType => ComposePrimitiveType(PrimitiveTypeName.Double, true),


            TokenType.Identifier when Current.Value.Equals("byte", StringComparison.OrdinalIgnoreCase) =>
                ComposeByteArrayType(),


            TokenType.StringType => ComposeStringType(),


            TokenType.BitsType => ComposeBitsType(),


            TokenType.Align => ComposeAlignmentType(),


            TokenType.LBracket => ComposeInlineSchema(),


            TokenType.Identifier or TokenType.Word => ComposeSchemaReferenceOrArray(),

            _ => throw new SyntaxException(
                $"Expected type annotation but found '{Current.TokenType}' ({Current.Value})",
                _lexer.AlreadyResolvedQueryPart)
        };
    }

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

    private TypeAnnotationNode ComposePrimitiveType(PrimitiveTypeName typeName, bool canHaveEndianness)
    {
        Consume(Current.TokenType);


        Node? arraySizeExpr = null;
        var arraySizeBeforeEndianness = false;
        if (Current.TokenType == TokenType.LeftSquareBracket)
        {
            Consume(TokenType.LeftSquareBracket);
            arraySizeExpr = ComposeSizeExpression();
            Consume(TokenType.RightSquareBracket);
            arraySizeBeforeEndianness = true;
        }


        Endianness endianness;
        if (canHaveEndianness)
        {
            endianness = Current.TokenType switch
            {
                TokenType.LittleEndian => Endianness.LittleEndian,
                TokenType.BigEndian => Endianness.BigEndian,
                _ => throw new SyntaxException(
                    $"Multi-byte type '{typeName}' requires endianness specifier (le or be)",
                    _lexer.AlreadyResolvedQueryPart)
            };
            Consume(Current.TokenType);
        }
        else
        {
            endianness = Endianness.NotApplicable;
        }


        if (arraySizeExpr == null && Current.TokenType == TokenType.LeftSquareBracket)
        {
            Consume(TokenType.LeftSquareBracket);
            arraySizeExpr = ComposeSizeExpression();
            Consume(TokenType.RightSquareBracket);
        }

        var primitiveType = new PrimitiveTypeNode(typeName, endianness);

        if (arraySizeExpr != null) return new ArrayTypeNode(primitiveType, arraySizeExpr);

        return primitiveType;
    }

    private TypeAnnotationNode ComposeByteArrayType()
    {
        Consume(Current.TokenType);
        Consume(TokenType.LeftSquareBracket);
        var sizeExpr = ComposeSizeExpression();
        Consume(TokenType.RightSquareBracket);

        return new ByteArrayTypeNode(sizeExpr);
    }

    private TypeAnnotationNode ComposeStringType()
    {
        Consume(TokenType.StringType);
        Consume(TokenType.LeftSquareBracket);
        var sizeExpr = ComposeSizeExpression();
        Consume(TokenType.RightSquareBracket);

        var encoding = ComposeStringEncoding();
        var modifiers = ComposeStringModifiers();


        string? asTextSchemaName = null;
        if (Current.TokenType == TokenType.As)
        {
            Consume(TokenType.As);
            if (Current.TokenType != TokenType.Identifier)
                throw new SyntaxException(
                    $"Expected text schema name after 'as' but found '{Current.TokenType}'",
                    _lexer.AlreadyResolvedQueryPart);
            asTextSchemaName = Current.Value;
            Consume(TokenType.Identifier);
        }

        var stringType = new StringTypeNode(sizeExpr, encoding, modifiers, asTextSchemaName);


        if (Current.TokenType == TokenType.LeftSquareBracket) return ComposeArrayOfType(stringType);

        return stringType;
    }

    private StringEncoding ComposeStringEncoding()
    {
        var encoding = Current.TokenType switch
        {
            TokenType.Utf8 => StringEncoding.Utf8,
            TokenType.Utf16Le => StringEncoding.Utf16Le,
            TokenType.Utf16Be => StringEncoding.Utf16Be,
            TokenType.Ascii => StringEncoding.Ascii,
            TokenType.Latin1 => StringEncoding.Latin1,
            TokenType.Ebcdic => StringEncoding.Ebcdic,
            _ => throw new SyntaxException(
                $"Expected string encoding (utf8, utf16le, utf16be, ascii, latin1, ebcdic) but found '{Current.TokenType}'",
                _lexer.AlreadyResolvedQueryPart)
        };
        Consume(Current.TokenType);
        return encoding;
    }

    private StringModifier ComposeStringModifiers()
    {
        var modifiers = StringModifier.None;

        while (true)
            switch (Current.TokenType)
            {
                case TokenType.Trim:
                    modifiers |= StringModifier.Trim;
                    Consume(TokenType.Trim);
                    break;
                case TokenType.RTrim:
                    modifiers |= StringModifier.RTrim;
                    Consume(TokenType.RTrim);
                    break;
                case TokenType.LTrim:
                    modifiers |= StringModifier.LTrim;
                    Consume(TokenType.LTrim);
                    break;
                case TokenType.NullTerm:
                    modifiers |= StringModifier.NullTerm;
                    Consume(TokenType.NullTerm);
                    break;
                default:
                    return modifiers;
            }
    }

    private BitsTypeNode ComposeBitsType()
    {
        Consume(TokenType.BitsType);
        Consume(TokenType.LeftSquareBracket);

        if (Current.TokenType != TokenType.Integer)
            throw new SyntaxException(
                "bits[] requires a constant integer for bit count",
                _lexer.AlreadyResolvedQueryPart);

        var countToken = ConsumeAndGetToken(TokenType.Integer);
        var bitCount = int.Parse(countToken.Value);

        Consume(TokenType.RightSquareBracket);

        return new BitsTypeNode(bitCount);
    }

    private AlignmentNode ComposeAlignmentType()
    {
        Consume(TokenType.Align);
        Consume(TokenType.LeftSquareBracket);

        if (Current.TokenType != TokenType.Integer)
            throw new SyntaxException(
                "align[] requires a constant integer for alignment bits",
                _lexer.AlreadyResolvedQueryPart);

        var bitsToken = ConsumeAndGetToken(TokenType.Integer);
        var alignmentBits = int.Parse(bitsToken.Value);

        Consume(TokenType.RightSquareBracket);

        return new AlignmentNode(alignmentBits);
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

        var typeArgs = new List<string>();


        typeArgs.Add(ComposeIdentifierOrWord());


        while (Current.TokenType == TokenType.Comma)
        {
            Consume(TokenType.Comma);
            typeArgs.Add(ComposeIdentifierOrWord());
        }

        Consume(TokenType.Greater);

        return typeArgs.ToArray();
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

        var condition = ComposeExpression();

        return new RepeatUntilTypeNode(elementType, condition, fieldName);
    }

    private Node ComposeExpression()
    {
        return ComposeComparisonExpression();
    }

    private Node ComposeSizeExpression()
    {
        return ComposeAdditiveExpression();
    }

    private Node ComposeAdditiveExpression()
    {
        var left = ComposeMultiplicativeExpression();

        while (Current.TokenType is TokenType.Plus or TokenType.Hyphen)
        {
            var op = Current.TokenType;
            Consume(op);
            var right = ComposeMultiplicativeExpression();

            left = op == TokenType.Plus
                ? new AddNode(left, right)
                : new HyphenNode(left, right);
        }

        return left;
    }

    private Node ComposeMultiplicativeExpression()
    {
        var left = ComposePrimaryExpression();

        while (Current.TokenType is TokenType.Star or TokenType.FSlash or TokenType.Mod)
        {
            var op = Current.TokenType;
            Consume(op);
            var right = ComposePrimaryExpression();

            left = op switch
            {
                TokenType.Star => new StarNode(left, right),
                TokenType.FSlash => new FSlashNode(left, right),
                TokenType.Mod => new ModuloNode(left, right),
                _ => throw new InvalidOperationException($"Unexpected operator: {op}")
            };
        }

        return left;
    }

    private Node ComposePrimaryExpression()
    {
        switch (Current.TokenType)
        {
            case TokenType.Integer:
                var intToken = ConsumeAndGetToken(TokenType.Integer);
                return new IntegerNode(intToken.Value, "i");

            case TokenType.HexadecimalInteger:
                var hexToken = ConsumeAndGetToken(TokenType.HexadecimalInteger);
                return new HexIntegerNode(hexToken.Value);

            case TokenType.BinaryInteger:
                var binToken = ConsumeAndGetToken(TokenType.BinaryInteger);
                return new BinaryIntegerNode(binToken.Value);

            case TokenType.OctalInteger:
                var octToken = ConsumeAndGetToken(TokenType.OctalInteger);
                return new OctalIntegerNode(octToken.Value);

            case TokenType.Identifier:
            case TokenType.Word:
            case TokenType.Function:
                return ComposeIdentifierOrFunctionCall();

            case TokenType.StringLiteral:

                var stringToken = ConsumeAndGetToken(TokenType.StringLiteral);
                return new WordNode(stringToken.Value);

            case TokenType.LeftParenthesis:
                Consume(TokenType.LeftParenthesis);
                var expr = ComposeExpression();
                Consume(TokenType.RightParenthesis);
                return expr;

            default:
                throw new SyntaxException(
                    $"Expected integer, identifier, or expression but found '{Current.TokenType}'",
                    _lexer.AlreadyResolvedQueryPart);
        }
    }

    private Node ComposeIdentifierOrFunctionCall()
    {
        var token = ConsumeAndGetToken(Current.TokenType);
        var name = token.Value;


        if (Current.TokenType == TokenType.LeftParenthesis)
        {
            Consume(TokenType.LeftParenthesis);
            var args = new List<Node>();


            if (Current.TokenType != TokenType.RightParenthesis)
            {
                args.Add(ComposeExpression());
                while (Current.TokenType == TokenType.Comma)
                {
                    Consume(TokenType.Comma);
                    args.Add(ComposeExpression());
                }
            }

            Consume(TokenType.RightParenthesis);
            var funcToken = new FunctionToken(name, token.Span);
            return new AccessMethodNode(funcToken, new ArgsListNode([..args]), null, false);
        }

        return new IdentifierNode(name);
    }

    private Node? ComposeOptionalAtOffset()
    {
        if (Current.TokenType != TokenType.At)
            return null;

        Consume(TokenType.At);
        return ComposeSizeExpression();
    }

    private FieldConstraintNode? ComposeOptionalConstraint()
    {
        if (Current.TokenType != TokenType.Check)
            return null;

        Consume(TokenType.Check);


        var hasParenthesis = Current.TokenType == TokenType.LeftParenthesis;
        if (hasParenthesis)
            Consume(TokenType.LeftParenthesis);

        var expression = ComposeComparisonExpression();

        if (hasParenthesis)
            Consume(TokenType.RightParenthesis);

        return new FieldConstraintNode(expression);
    }

    private Node? ComposeOptionalWhenCondition()
    {
        if (Current.TokenType != TokenType.When)
            return null;

        Consume(TokenType.When);
        return ComposeComparisonExpression();
    }

    private Node ComposeComparisonExpression()
    {
        return ComposeLogicalOrExpression();
    }

    private Node ComposeLogicalOrExpression()
    {
        var left = ComposeLogicalAndExpression();

        while (Current.TokenType == TokenType.Or)
        {
            Consume(TokenType.Or);
            var right = ComposeLogicalAndExpression();
            left = new OrNode(left, right);
        }

        return left;
    }

    private Node ComposeLogicalAndExpression()
    {
        var left = ComposeRelationalExpression();

        while (Current.TokenType == TokenType.And)
        {
            Consume(TokenType.And);
            var right = ComposeRelationalExpression();
            left = new AndNode(left, right);
        }

        return left;
    }

    private Node ComposeRelationalExpression()
    {
        var left = ComposeBitwiseOrExpression();

        switch (Current.TokenType)
        {
            case TokenType.Equality:
                Consume(TokenType.Equality);
                return new EqualityNode(left, ComposeBitwiseOrExpression());
            case TokenType.Diff:
                Consume(TokenType.Diff);
                return new DiffNode(left, ComposeBitwiseOrExpression());
            case TokenType.Greater:
                Consume(TokenType.Greater);
                return new GreaterNode(left, ComposeBitwiseOrExpression());
            case TokenType.GreaterEqual:
                Consume(TokenType.GreaterEqual);
                return new GreaterOrEqualNode(left, ComposeBitwiseOrExpression());
            case TokenType.Less:
                Consume(TokenType.Less);
                return new LessNode(left, ComposeBitwiseOrExpression());
            case TokenType.LessEqual:
                Consume(TokenType.LessEqual);
                return new LessOrEqualNode(left, ComposeBitwiseOrExpression());
            default:
                return left;
        }
    }

    private Node ComposeBitwiseOrExpression()
    {
        var left = ComposeBitwiseXorExpression();

        while (Current.TokenType == TokenType.Pipe)
        {
            Consume(TokenType.Pipe);
            var right = ComposeBitwiseXorExpression();
            left = new BitwiseOrNode(left, right);
        }

        return left;
    }

    private Node ComposeBitwiseXorExpression()
    {
        var left = ComposeBitwiseAndExpression();

        while (Current.TokenType == TokenType.Caret)
        {
            Consume(TokenType.Caret);
            var right = ComposeBitwiseAndExpression();
            left = new BitwiseXorNode(left, right);
        }

        return left;
    }

    private Node ComposeBitwiseAndExpression()
    {
        var left = ComposeShiftExpression();

        while (Current.TokenType == TokenType.Ampersand)
        {
            Consume(TokenType.Ampersand);
            var right = ComposeShiftExpression();
            left = new BitwiseAndNode(left, right);
        }

        return left;
    }

    private Node ComposeShiftExpression()
    {
        var left = ComposeAdditiveExpression();

        while (Current.TokenType is TokenType.LeftShift or TokenType.RightShift)
        {
            var op = Current.TokenType;
            Consume(op);
            var right = ComposeAdditiveExpression();

            left = op == TokenType.LeftShift
                ? new LeftShiftNode(left, right)
                : new RightShiftNode(left, right);
        }

        return left;
    }

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

    private TextFieldDefinitionNode ComposeTextField()
    {
        var name = ComposeIdentifierOrWord();
        Consume(TokenType.Colon);


        var isOptionalPrefix = Current.TokenType == TokenType.Optional;
        if (isOptionalPrefix) Consume(TokenType.Optional);

        var field = Current.TokenType switch
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
                $"Expected text field type (pattern, literal, until, between, chars, token, rest, whitespace, repeat, switch) but found '{Current.TokenType}'",
                _lexer.AlreadyResolvedQueryPart)
        };


        if (isOptionalPrefix)
            return new TextFieldDefinitionNode(
                field.Name,
                field.FieldType,
                field.PrimaryValue,
                field.SecondaryValue,
                field.Modifiers | TextFieldModifier.Optional,
                field.EscapeCharacter,
                field.CaptureGroups);

        return field;
    }

    private TextFieldDefinitionNode ComposePatternField(string name)
    {
        Consume(TokenType.Pattern);
        var pattern = ComposeStringLiteral();
        var captureGroups = ComposeOptionalCaptureGroups();
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

    private TextFieldDefinitionNode ComposeBetweenField(string name)
    {
        Consume(TokenType.Between);
        var openDelimiter = ComposeStringLiteral();
        var closeDelimiter = ComposeStringLiteral();
        var modifiers = ComposeTextFieldModifiers();

        string? escapeChar = null;
        if ((modifiers & TextFieldModifier.Escaped) != 0 &&
            Current.TokenType is TokenType.Word or TokenType.StringLiteral)
            escapeChar = ComposeStringLiteral();

        return new TextFieldDefinitionNode(
            name, TextFieldType.Between, openDelimiter, closeDelimiter, modifiers, escapeChar);
    }

    private TextFieldDefinitionNode ComposeCharsField(string name)
    {
        Consume(TokenType.Chars);
        Consume(TokenType.LeftSquareBracket);

        if (Current.TokenType != TokenType.Integer)
            throw new SyntaxException(
                "chars[] requires an integer count",
                _lexer.AlreadyResolvedQueryPart);

        var countStr = ConsumeAndGetToken(TokenType.Integer).Value;
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
                 (Current.TokenType == TokenType.Word && Current.Value == "?"))
        {
            Consume(Current.TokenType);
            quantifier = "?";
        }

        var modifiers = ComposeTextFieldModifiers();

        return new TextFieldDefinitionNode(
            name, TextFieldType.Whitespace, quantifier, null, modifiers);
    }

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

        var groups = new List<string>();
        groups.Add(ComposeIdentifierOrWord());

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

    private string[]? ComposeOptionalTypeParameters()
    {
        if (Current.TokenType != TokenType.Less)
            return null;

        Consume(TokenType.Less);

        var typeParams = new List<string>();


        typeParams.Add(ComposeIdentifierOrWord());


        while (Current.TokenType == TokenType.Comma)
        {
            Consume(TokenType.Comma);
            typeParams.Add(ComposeIdentifierOrWord());
        }

        Consume(TokenType.Greater);

        return typeParams.ToArray();
    }

    private string? ComposeOptionalExtends()
    {
        if (Current.TokenType != TokenType.Extends)
            return null;

        Consume(TokenType.Extends);
        return ComposeIdentifierOrWord();
    }

    private string ComposeIdentifierOrWord()
    {
        return Current.TokenType switch
        {
            TokenType.Identifier => ConsumeAndGetToken(TokenType.Identifier).Value,
            TokenType.Word => ConsumeAndGetToken(TokenType.Word).Value,

            TokenType.Property when Current.Value == "_" => ConsumeAndGetToken(TokenType.Property).Value,


            TokenType.Rest => ConsumeAndGetToken(TokenType.Rest).Value,
            TokenType.Text => ConsumeAndGetToken(TokenType.Text).Value,
            TokenType.End => ConsumeAndGetToken(TokenType.End).Value,
            TokenType.Binary => ConsumeAndGetToken(TokenType.Binary).Value,
            TokenType.Pattern => ConsumeAndGetToken(TokenType.Pattern).Value,
            TokenType.Literal => ConsumeAndGetToken(TokenType.Literal).Value,
            TokenType.Until => ConsumeAndGetToken(TokenType.Until).Value,
            TokenType.Between => ConsumeAndGetToken(TokenType.Between).Value,
            TokenType.Chars => ConsumeAndGetToken(TokenType.Chars).Value,
            TokenType.Token => ConsumeAndGetToken(TokenType.Token).Value,
            TokenType.Whitespace => ConsumeAndGetToken(TokenType.Whitespace).Value,
            TokenType.Switch => ConsumeAndGetToken(TokenType.Switch).Value,
            TokenType.Repeat => ConsumeAndGetToken(TokenType.Repeat).Value,
            TokenType.Optional => ConsumeAndGetToken(TokenType.Optional).Value,

            _ => throw new SyntaxException(
                $"Expected identifier but found '{Current.TokenType}' ({Current.Value})",
                _lexer.AlreadyResolvedQueryPart)
        };
    }

    private string ComposeStringLiteral()
    {
        if (Current.TokenType != TokenType.Word && Current.TokenType != TokenType.StringLiteral)
            throw new SyntaxException(
                $"Expected string literal but found '{Current.TokenType}'",
                _lexer.AlreadyResolvedQueryPart);

        var token = ConsumeAndGetToken(Current.TokenType);

        var value = token.Value;

        if (token.TokenType == TokenType.Word &&
            ((value.StartsWith("'") && value.EndsWith("'")) ||
             (value.StartsWith("\"") && value.EndsWith("\""))) && value.Length >= 2)
            value = value[1..^1];

        return UnescapeString(value);
    }

    private static string UnescapeString(string value)
    {
        return value
            .Replace("\\'", "'")
            .Replace("\\\"", "\"")
            .Replace("\\\\", "\\")
            .Replace("\\r", "\r")
            .Replace("\\n", "\n")
            .Replace("\\t", "\t");
    }

    private TokenType PeekNextTokenType()
    {
        _savedTokenBeforePeek = _lexer.Current();


        _lexer.Next();
        var nextToken = _lexer.Current();


        _peekedToken = nextToken;

        return nextToken.TokenType;
    }

    private void Consume(TokenType tokenType)
    {
        if (!Current.TokenType.Equals(tokenType))
            throw new SyntaxException(
                $"Expected token '{tokenType}' but found '{Current.TokenType}' ({Current.Value})",
                _lexer.AlreadyResolvedQueryPart);

        Previous = Current;
        _hasReplacedToken = false;
        _savedTokenBeforePeek = null;


        if (_peekedToken != null)
        {
            _replacedToken = _peekedToken;
            _hasReplacedToken = true;
            _peekedToken = null;
        }
        else
        {
            _lexer.Next();
        }
    }

    private Token ConsumeAndGetToken(TokenType expected)
    {
        var token = Current;
        Consume(expected);
        return token;
    }
}
