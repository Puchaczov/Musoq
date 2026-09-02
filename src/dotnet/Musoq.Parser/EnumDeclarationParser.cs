using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Nodes;
using Musoq.Parser.Tokens;

namespace Musoq.Parser;

public partial class Parser
{
    private readonly HashSet<string> _enumDeclarationNames = new(StringComparer.OrdinalIgnoreCase);

    private static readonly Dictionary<string, EnumBackingRange> EnumBackingRanges = new(
        StringComparer.OrdinalIgnoreCase)
    {
        ["byte"] = new("byte", 8, false),
        ["sbyte"] = new("sbyte", 8, true),
        ["short"] = new("short", 16, true),
        ["ushort"] = new("ushort", 16, false),
        ["int"] = new("int", 32, true),
        ["uint"] = new("uint", 32, false),
        ["long"] = new("long", 64, true),
        ["ulong"] = new("ulong", 64, false)
    };

    private bool IsEnumDeclarationStart()
    {
        return IsContextualStatementWord("enum") || IsContextualStatementWord("flags");
    }

    private bool TryComposeEnumDeclarationStatement(out StatementNode statement)
    {
        if (IsEnumDeclarationStart())
        {
            statement = ComposeAndSkipIfPresent(
                parser => new StatementNode(parser.ComposeEnumDeclaration()),
                TokenType.Semicolon);
            return true;
        }

        if (Current.TokenType == TokenType.Identifier &&
            Current.Value.Equals("create", StringComparison.OrdinalIgnoreCase))
        {
            throw new SyntaxException(
                "CREATE TYPE ... AS ENUM is not supported. Use Musoq syntax: enum Name : int { Member = 1 };",
                _lexer.AlreadyResolvedQueryPart,
                DiagnosticCode.MQ2048_UnsupportedEnumSyntax,
                Current.Span);
        }

        statement = null!;
        return false;
    }

    private bool IsContextualStatementWord(string value)
    {
        return Current.TokenType is TokenType.Identifier or TokenType.Word or TokenType.Function &&
               Current.Value.Equals(value, StringComparison.OrdinalIgnoreCase);
    }

    private EnumDeclarationNode ComposeEnumDeclaration()
    {
        var startToken = Current;
        var isFlags = IsContextualStatementWord("flags");

        if (isFlags)
        {
            ConsumeAndGetToken();
            if (!IsContextualStatementWord("enum"))
                throw EnumSyntaxException(
                    DiagnosticCode.MQ2042_InvalidEnumDeclaration,
                    "FLAGS must be followed by ENUM in a query-local enum declaration.",
                    Current.Span);
        }

        if (!IsContextualStatementWord("enum"))
            throw EnumSyntaxException(
                DiagnosticCode.MQ2042_InvalidEnumDeclaration,
                "Expected ENUM at the start of a query-local enum declaration.",
                Current.Span);

        if (Current.TokenType == TokenType.Function)
            throw EnumSyntaxException(
                DiagnosticCode.MQ2048_UnsupportedEnumSyntax,
                "MySQL-style ENUM(...) declarations are not supported. Declare a named Musoq enum with an integral backing type.",
                Current.Span);

        ConsumeAndGetToken();
        var nameToken = ConsumeEnumIdentifier("enum type name");

        if (_enumDeclarationNames.Contains(nameToken.Value))
            throw EnumSyntaxException(
                DiagnosticCode.MQ2042_InvalidEnumDeclaration,
                $"Enum type '{nameToken.Value}' is declared more than once; query-local type names are case-insensitive.",
                nameToken.Span);

        if (Current.TokenType != TokenType.Colon)
            throw EnumSyntaxException(
                DiagnosticCode.MQ2042_InvalidEnumDeclaration,
                "An enum declaration requires ': <integral backing type>' after its name.",
                Current.Span);

        Consume(TokenType.Colon);
        var backingToken = ConsumeEnumIdentifier("enum backing type");
        if (!EnumBackingRanges.TryGetValue(backingToken.Value, out var backingRange))
            throw EnumSyntaxException(
                DiagnosticCode.MQ2043_InvalidEnumBackingType,
                $"Enum backing type '{backingToken.Value}' is not supported. Use byte, sbyte, short, ushort, int, uint, long, or ulong.",
                backingToken.Span);

        if (Current.TokenType != TokenType.LBracket)
            throw EnumSyntaxException(
                DiagnosticCode.MQ2042_InvalidEnumDeclaration,
                "An enum declaration requires a '{ ... }' member body.",
                Current.Span);

        Consume(TokenType.LBracket);
        if (Current.TokenType == TokenType.RBracket)
            throw EnumSyntaxException(
                DiagnosticCode.MQ2047_EmptyEnumDeclaration,
                $"Enum '{nameToken.Value}' must declare at least one explicitly-valued member.",
                Current.Span);

        var members = new List<EnumMemberNode>();
        var memberNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        while (Current.TokenType != TokenType.RBracket)
        {
            if (Current.TokenType == TokenType.EndOfFile)
                throw EnumSyntaxException(
                    DiagnosticCode.MQ2017_UnexpectedEndOfFile,
                    "The enum declaration ends unexpectedly; expected a closing '}'.",
                    new TextSpan(Current.Span.Start, 0));

            var memberNameToken = ConsumeEnumIdentifier("enum member name");
            if (memberNames.TryGetValue(memberNameToken.Value, out var existingName))
            {
                var detail = existingName.Equals(memberNameToken.Value, StringComparison.Ordinal)
                    ? $"Enum member '{memberNameToken.Value}' is declared more than once."
                    : $"Enum members '{existingName}' and '{memberNameToken.Value}' differ only by casing, which is not allowed.";
                throw EnumSyntaxException(
                    DiagnosticCode.MQ2045_DuplicateEnumMember,
                    detail,
                    memberNameToken.Span);
            }

            memberNames.Add(memberNameToken.Value, memberNameToken.Value);

            if (Current.TokenType != TokenType.Equality)
                throw EnumSyntaxException(
                    DiagnosticCode.MQ2044_MissingEnumMemberValue,
                    $"Enum member '{memberNameToken.Value}' requires an explicit integral value.",
                    memberNameToken.Span);

            Consume(TokenType.Equality);
            var member = ComposeEnumMember(memberNameToken, backingRange);
            members.Add(member);

            if (Current.TokenType != TokenType.Comma)
            {
                if (Current.TokenType != TokenType.RBracket)
                    throw EnumSyntaxException(
                        DiagnosticCode.MQ2042_InvalidEnumDeclaration,
                        "Enum members must be separated with commas.",
                        Current.Span);

                break;
            }

            Consume(TokenType.Comma);
            if (Current.TokenType == TokenType.RBracket)
                break;
        }

        var closingToken = ConsumeAndGetToken(TokenType.RBracket);
        _enumDeclarationNames.Add(nameToken.Value);
        return new EnumDeclarationNode(
            nameToken.Value,
            backingRange.CanonicalName,
            isFlags,
            members,
            nameToken.Span,
            backingToken.Span,
            startToken.Span.Through(closingToken.Span));
    }

    private EnumMemberNode ComposeEnumMember(Token nameToken, EnumBackingRange backingRange)
    {
        var hasSeparateNegativeSign = Current.TokenType == TokenType.Hyphen;
        var startSpan = Current.Span;
        if (hasSeparateNegativeSign)
            Consume(TokenType.Hyphen);

        if (Current.TokenType is not (TokenType.Integer or TokenType.HexadecimalInteger or
            TokenType.BinaryInteger or TokenType.OctalInteger))
        {
            throw EnumSyntaxException(
                DiagnosticCode.MQ2044_MissingEnumMemberValue,
                $"Enum member '{nameToken.Value}' requires an explicit integral literal value.",
                Current.Span);
        }

        var valueToken = ConsumeAndGetToken();
        var hasTokenNegativeSign = valueToken.Value.StartsWith("-", StringComparison.Ordinal);
        var isNegative = hasSeparateNegativeSign || hasTokenNegativeSign;
        var valueSpan = hasSeparateNegativeSign ? startSpan.Through(valueToken.Span) : valueToken.Span;
        var literalText = _lexer.Input.Substring(valueSpan.Start, valueSpan.Length);
        BigInteger magnitude;

        try
        {
            magnitude = ParseEnumMagnitude(valueToken, hasTokenNegativeSign);
        }
        catch (FormatException ex)
        {
            throw EnumSyntaxException(
                DiagnosticCode.MQ2046_EnumMemberValueOutOfRange,
                $"Enum member value '{literalText}' is not a valid integral literal.",
                valueSpan,
                ex);
        }

        var numericValue = isNegative ? -magnitude : magnitude;
        if (!backingRange.Contains(numericValue))
            throw EnumSyntaxException(
                DiagnosticCode.MQ2046_EnumMemberValueOutOfRange,
                $"Enum member value '{literalText}' is outside the range of backing type '{backingRange.CanonicalName}'.",
                valueSpan);

        var rawValue = backingRange.ToRawValue(numericValue);
        return new EnumMemberNode(
            nameToken.Value,
            rawValue,
            literalText,
            nameToken.Span,
            valueSpan,
            nameToken.Span.Through(valueSpan));
    }

    private Token ConsumeEnumIdentifier(string role)
    {
        if (Current.TokenType != TokenType.Identifier)
            throw EnumSyntaxException(
                DiagnosticCode.MQ2042_InvalidEnumDeclaration,
                $"Expected an identifier for the {role}.",
                Current.Span);

        return ConsumeAndGetToken(TokenType.Identifier);
    }

    private static BigInteger ParseEnumMagnitude(Token token, bool hasNegativeSign)
    {
        var (digits, radix) = token.TokenType switch
        {
            TokenType.Integer => (hasNegativeSign ? token.Value[1..] : token.Value, 10),
            TokenType.HexadecimalInteger => (token.Value[2..], 16),
            TokenType.BinaryInteger => (token.Value[2..], 2),
            TokenType.OctalInteger => (token.Value[2..], 8),
            _ => throw new InvalidOperationException($"Token '{token.TokenType}' is not an integral literal.")
        };

        if (radix == 10)
            return BigInteger.Parse(digits, NumberStyles.None, CultureInfo.InvariantCulture);

        var value = BigInteger.Zero;
        foreach (var character in digits)
        {
            var digit = character switch
            {
                >= '0' and <= '9' => character - '0',
                >= 'a' and <= 'f' => character - 'a' + 10,
                >= 'A' and <= 'F' => character - 'A' + 10,
                _ => -1
            };

            if (digit < 0 || digit >= radix)
                throw new FormatException($"Invalid base-{radix} digit '{character}'.");

            value = value * radix + digit;
        }

        return value;
    }

    private SyntaxException EnumSyntaxException(
        DiagnosticCode code,
        string message,
        TextSpan span,
        Exception? innerException = null)
    {
        return innerException == null
            ? new SyntaxException(message, _lexer.AlreadyResolvedQueryPart, code, span)
            : new SyntaxException(message, _lexer.AlreadyResolvedQueryPart, code, span, innerException);
    }

    private readonly record struct EnumBackingRange(string CanonicalName, int BitWidth, bool IsSigned)
    {
        public bool Contains(BigInteger value)
        {
            var minimum = IsSigned ? -(BigInteger.One << (BitWidth - 1)) : BigInteger.Zero;
            var maximum = IsSigned
                ? (BigInteger.One << (BitWidth - 1)) - BigInteger.One
                : (BigInteger.One << BitWidth) - BigInteger.One;
            return value >= minimum && value <= maximum;
        }

        public ulong ToRawValue(BigInteger value)
        {
            var normalized = value.Sign < 0 ? value + (BigInteger.One << BitWidth) : value;
            return (ulong)normalized;
        }
    }
}
