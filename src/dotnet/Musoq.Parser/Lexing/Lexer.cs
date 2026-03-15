using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Helpers;
using Musoq.Parser.Tokens;

namespace Musoq.Parser.Lexing;

/// <summary>
///     High-performance lexer that uses direct character scanning instead of regex matching.
///     Provides 17-42x speedup over previous regex-based lexer for most queries.
/// </summary>
public sealed class Lexer : ILexer
{
    // Compiled regexes for complex patterns that can't be easily hand-parsed
    private static readonly Regex FunctionRegex = new(@"\G[a-zA-Z_][a-zA-Z0-9_-]*(?=\()", RegexOptions.Compiled);

    private static readonly Regex MethodAccessRegex =
        new(@"\G([a-zA-Z0-9_]+)(?=\.[a-zA-Z_-][a-zA-Z0-9_-]*\()", RegexOptions.Compiled);

    private static readonly Regex NumericAccessRegex = new(@"\G([\w*?_]+)\[([-]?\d+)\]", RegexOptions.Compiled);
    private static readonly Regex KeyAccessConstRegex = new(@"\G([\w*?_]+)\[('[a-zA-Z0-9]+')\]", RegexOptions.Compiled);

    private static readonly Regex KeyAccessVarRegex =
        new(@"\G([\w*?_]+)\[([a-zA-Z0-9_\s\+\-\*\/\%\(\)]+)\]", RegexOptions.Compiled);

    private static readonly Regex StringLiteralRegex = new(@"\G'([^'\\]|\\.)*'", RegexOptions.Compiled);
    private static readonly Regex FieldLinkRegex = new(@"\G::[1-9]\d*", RegexOptions.Compiled);
    private static readonly Regex AliasedStarRegex = new(@"\G[a-zA-Z_]\w*\.\*", RegexOptions.Compiled);
    private static readonly Regex HFromRegex = new(@"\G#[\w*?_]+", RegexOptions.Compiled);
    private static readonly Regex LineCommentRegex = new(@"\G--[^\r\n]*", RegexOptions.Compiled);
    private static readonly Regex BlockCommentRegex = new(@"\G/\*[\s\S]*?\*/", RegexOptions.Compiled);
    private static readonly Regex BracketedColumnRegex = new(@"\G\[[^\]]+\]", RegexOptions.Compiled);

    // Numeric literal regexes
    private static readonly Regex DecimalWithDotRegex = new(@"\G-?\d+\.\d+[dD]?", RegexOptions.Compiled);
    private static readonly Regex HexIntegerRegex = new(@"\G0[xX][0-9a-fA-F]+", RegexOptions.Compiled);
    private static readonly Regex BinaryIntegerRegex = new(@"\G0[bB][01]+", RegexOptions.Compiled);
    private static readonly Regex OctalIntegerRegex = new(@"\G0[oO][0-7]+", RegexOptions.Compiled);

    // Multi-word keyword regexes
    private static readonly Regex NotInRegex =
        new(@"\Gnot\s+in(?=\s|$)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex NotLikeRegex =
        new(@"\Gnot\s+like(?=\s|$)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex NotRLikeRegex =
        new(@"\Gnot\s+rlike(?=\s|$)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex UnionAllRegex =
        new(@"\Gunion\s+all(?=\s|$)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex GroupByRegex =
        new(@"\Ggroup\s+by(?=\s|$)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex OrderByRegex =
        new(@"\Gorder\s+by(?=\s|$)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex InnerJoinRegex =
        new(@"\G(?:inner\s+)?join\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex OuterJoinRegex = new(@"\G(left|right)(?:\s+outer)?\s+join\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex CrossApplyRegex =
        new(@"\Gcross\s+apply(?=\s|$)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex OuterApplyRegex =
        new(@"\Gouter\s+apply(?=\s|$)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private readonly List<Token> _alreadyResolvedTokens = [];
    private readonly Queue<Token> _pendingSchemaTokens = new();
    private readonly bool _skipWhiteSpaces;
    private Token _currentToken;
    private Token _lastToken;

    /// <summary>
    ///     Initialize instance.
    /// </summary>
    /// <param name="input">The SQL query to tokenize.</param>
    /// <param name="skipWhiteSpaces">Whether to skip whitespace tokens.</param>
    /// <param name="recoverOnError">Whether to recover from errors instead of throwing.</param>
    public Lexer(string input, bool skipWhiteSpaces, bool recoverOnError = false)
    {
        if (input == null)
            throw ParserValidationException.ForNullInput();

        if (string.IsNullOrWhiteSpace(input))
            throw ParserValidationException.ForEmptyInput();

        Input = input.Trim();
        _skipWhiteSpaces = skipWhiteSpaces;
        RecoverOnError = recoverOnError;
        Position = 0;
        _currentToken = new NoneToken();
        _lastToken = _currentToken;
        SourceText = new SourceText(Input);
        Diagnostics = new DiagnosticBag { SourceText = SourceText };
    }

    /// <summary>
    ///     Gets or sets whether the lexer is in schema parsing context.
    /// </summary>
    public bool IsSchemaContext { get; set; }

    /// <summary>
    ///     Gets the input string.
    /// </summary>
    public string Input { get; }

    /// <summary>
    ///     Gets the source text for the input.
    /// </summary>
    public SourceText SourceText { get; }

    /// <summary>
    ///     Gets the diagnostic bag for collecting errors.
    /// </summary>
    public DiagnosticBag Diagnostics { get; }

    /// <summary>
    ///     Gets or sets whether to recover from errors instead of throwing.
    /// </summary>
    public bool RecoverOnError { get; set; }

    /// <summary>
    ///     Gets the current position.
    /// </summary>
    public int Position { get; private set; }

    /// <summary>
    ///     Gets the part of resolved query.
    /// </summary>
    public string AlreadyResolvedQueryPart
    {
        get
        {
            var startToken = _alreadyResolvedTokens.Count >= 5
                ? _alreadyResolvedTokens[^5]
                : _alreadyResolvedTokens.Count > 0
                    ? _alreadyResolvedTokens[0]
                    : null;

            if (startToken == null)
                return string.Empty;

            var endToken = _alreadyResolvedTokens[^1];
            return Input.Substring(startToken.Span.Start, endToken.Span.End - startToken.Span.Start);
        }
    }

    public Token Current()
    {
        return _currentToken;
    }

    public Token Last()
    {
        return _lastToken;
    }

    public Token Next()
    {
        if (IsSchemaContext && _pendingSchemaTokens.Count > 0)
        {
            var queuedToken = _pendingSchemaTokens.Dequeue();
            _alreadyResolvedTokens.Add(queuedToken);
            return AssignToken(queuedToken);
        }

        var token = NextInternal();
        while (ShouldSkipToken(token))
            token = NextInternal();


        if (IsSchemaContext)
        {
            if (token.TokenType == TokenType.NumericAccess)
                token = SplitNumericAccessToken(token);
            else if (token.TokenType == TokenType.KeyAccess)
                token = SplitKeyAccessToken(token);
        }

        _alreadyResolvedTokens.Add(token);
        return token;
    }

    public Token NextOf(Regex regex, Func<string, Token> getToken)
    {
        if (Position >= Input.Length)
            return AssignToken(new EndOfFileToken(new TextSpan(Input.Length, 0)));

        var match = regex.Match(Input, Position);
        if (!match.Success || match.Index != Position)
            throw new UnknownTokenException(Position, Input[Position],
                $"Unrecognized token at {Position} for {Input[Position..]}");

        var token = getToken(match.Value);
        Position += match.Length;
        _alreadyResolvedTokens.Add(token);
        return AssignToken(token);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Token AssignToken(Token token)
    {
        _lastToken = _currentToken;
        _currentToken = token;
        return token;
    }

    private Token NextInternal()
    {
        if (Position >= Input.Length)
            return AssignToken(new EndOfFileToken(new TextSpan(Input.Length, 0)));

        var c = Input[Position];
        var category = FastCharacterClassifier.GetCategory(c);

        return category switch
        {
            FastCharacterClassifier.CharCategory.Whitespace => ScanWhitespace(),
            FastCharacterClassifier.CharCategory.Identifier => ScanIdentifierOrKeyword(),
            FastCharacterClassifier.CharCategory.Digit => ScanNumber(),
            FastCharacterClassifier.CharCategory.Quote => ScanStringLiteral(),
            FastCharacterClassifier.CharCategory.SingleCharOperator => ScanSingleCharOperator(),
            FastCharacterClassifier.CharCategory.MultiCharOperator => ScanMultiCharOperator(),
            FastCharacterClassifier.CharCategory.Hash => ScanHashFrom(),
            FastCharacterClassifier.CharCategory.Dash => ScanDash(),
            FastCharacterClassifier.CharCategory.Slash => ScanSlash(),
            FastCharacterClassifier.CharCategory.Dot => ScanDot(),
            FastCharacterClassifier.CharCategory.SquareBracket => ScanSquareBracket(),
            FastCharacterClassifier.CharCategory.Colon => ScanColon(),
            _ => ScanUnknown()
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Token ScanWhitespace()
    {
        var start = Position;
        while (Position < Input.Length && FastCharacterClassifier.IsWhitespace(Input[Position]))
            Position++;

        return AssignToken(new WhiteSpaceToken(new TextSpan(start, Position - start)));
    }

    private Token ScanIdentifierOrKeyword()
    {
        var start = Position;


        var multiWordToken = TryMatchMultiWordKeyword();
        if (multiWordToken != null)
            return AssignToken(multiWordToken);


        var funcToken = TryMatchRegex(FunctionRegex, start, (m, span) => new FunctionToken(m.Value, span));
        if (funcToken != null)
            return AssignToken(funcToken);


        var methodToken = TryMatchRegex(MethodAccessRegex, start, (m, span) => new MethodAccessToken(m.Groups[1].Value, span));
        if (methodToken != null)
            return AssignToken(methodToken);


        var aliasedStarToken = TryMatchRegex(AliasedStarRegex, start, (m, span) => new AliasedStarToken(m.Value, span));
        if (aliasedStarToken != null)
            return AssignToken(aliasedStarToken);


        var numAccessToken = TryMatchRegex(NumericAccessRegex, start, (m, span) => new NumericAccessToken(m.Groups[1].Value, m.Groups[2].Value, span));
        if (numAccessToken != null)
            return AssignToken(numAccessToken);

        var keyAccessConstToken = TryMatchRegex(KeyAccessConstRegex, start, (m, span) => new KeyAccessToken(m.Groups[1].Value, m.Groups[2].Value, span));
        if (keyAccessConstToken != null)
            return AssignToken(keyAccessConstToken);

        var keyAccessVarToken = TryMatchRegex(KeyAccessVarRegex, start, (m, span) => new KeyAccessToken(m.Groups[1].Value, m.Groups[2].Value, span));
        if (keyAccessVarToken != null)
            return AssignToken(keyAccessVarToken);


        while (Position < Input.Length && FastCharacterClassifier.IsIdentifierContinue(Input[Position]))
            Position++;

        var text = Input[start..Position];
        var span = new TextSpan(start, Position - start);


        if (IsSchemaContext && KeywordLookup.IsSchemaKeyword(text))
        {
            if (_currentToken?.TokenType == TokenType.Dot)
                return AssignToken(new PropertyToken(text, span));

            var schemaType = KeywordLookup.GetSchemaKeywordType(text);
            return AssignToken(new SchemaToken(text, schemaType, span));
        }

        if (KeywordLookup.TryGetKeyword(text, out var keywordType))
        {
            if (keywordType == TokenType.End && _currentToken?.TokenType == TokenType.Dot)
                return AssignToken(new PropertyToken(text, span));

            return AssignToken(TokenFactory.Create(keywordType, start, text) ?? new WordToken(text, span));
        }


        if (KeywordLookup.IsSchemaKeyword(text))
        {
            if (_currentToken?.TokenType == TokenType.Dot)
                return AssignToken(new PropertyToken(text, span));

            if (!IsSchemaContext)
                return AssignToken(new ColumnToken(text, span));

            var schemaType = KeywordLookup.GetSchemaKeywordType(text);
            return AssignToken(new SchemaToken(text, schemaType, span));
        }


        if (_currentToken?.TokenType == TokenType.Dot)
            return AssignToken(new PropertyToken(text, span));

        return AssignToken(new ColumnToken(text, span));
    }

    private Token? TryMatchMultiWordKeyword()
    {
        var start = Position;
        var c = char.ToLowerInvariant(Input[Position]);

        return c switch
        {
            'n' => TryMatchRegex(NotInRegex, start, span => new NotInToken(span)) ??
                   TryMatchRegex(NotLikeRegex, start, span => new NotLikeToken(span)) ??
                   TryMatchRegex(NotRLikeRegex, start, span => new NotRLikeToken(span)),

            'u' => TryMatchRegex(UnionAllRegex, start, span => new UnionAllToken(span)),

            'g' => TryMatchRegex(GroupByRegex, start, span => new GroupByToken(span)),

            'o' => TryMatchRegex(OrderByRegex, start, span => new OrderByToken(span)) ??
                   TryMatchRegex(OuterApplyRegex, start, span => new OuterApplyToken(span)),

            'j' or 'i' => TryMatchRegex(InnerJoinRegex, start, span => new InnerJoinToken(span)),

            'l' or 'r' => TryMatchRegex(OuterJoinRegex, start, span =>
                new OuterJoinToken(
                    char.ToLowerInvariant(Input[start]) == 'l' ? OuterJoinType.Left : OuterJoinType.Right,
                    span)),

            'c' => TryMatchRegex(CrossApplyRegex, start, span => new CrossApplyToken(span)),

            _ => null
        };
    }

    private Token? TryMatchRegex(Regex regex, int start, Func<TextSpan, Token> tokenFactory)
    {
        var match = regex.Match(Input, Position);

        if (!match.Success || match.Index != Position)
            return null;

        Position += match.Length;
        return tokenFactory(new TextSpan(start, match.Length));
    }

    private Token? TryMatchRegex(Regex regex, int start, Func<Match, TextSpan, Token> tokenFactory)
    {
        var match = regex.Match(Input, Position);

        if (!match.Success || match.Index != Position)
            return null;

        Position += match.Length;
        return tokenFactory(match, new TextSpan(start, match.Length));
    }

    private Token ScanNumber()
    {
        var start = Position;


        if (Input[Position] == '0' && Position + 1 < Input.Length)
        {
            var next = char.ToLowerInvariant(Input[Position + 1]);
            if (next == 'x')
            {
                var hexMatch = HexIntegerRegex.Match(Input, Position);
                if (hexMatch.Success && hexMatch.Index == Position)
                {
                    Position += hexMatch.Length;
                    return AssignToken(new HexIntegerToken(hexMatch.Value, new TextSpan(start, hexMatch.Length)));
                }

                return HandleInvalidBaseNumber(start, DiagnosticCode.MQ1006_InvalidHexNumber, "hexadecimal", "0x");
            }

            if (next == 'b')
            {
                var binMatch = BinaryIntegerRegex.Match(Input, Position);
                if (binMatch.Success && binMatch.Index == Position)
                {
                    Position += binMatch.Length;
                    return AssignToken(new BinaryIntegerToken(binMatch.Value, new TextSpan(start, binMatch.Length)));
                }

                return HandleInvalidBaseNumber(start, DiagnosticCode.MQ1007_InvalidBinaryNumber, "binary", "0b");
            }

            if (next == 'o')
            {
                var octMatch = OctalIntegerRegex.Match(Input, Position);
                if (octMatch.Success && octMatch.Index == Position)
                {
                    Position += octMatch.Length;
                    return AssignToken(new OctalIntegerToken(octMatch.Value, new TextSpan(start, octMatch.Length)));
                }

                return HandleInvalidBaseNumber(start, DiagnosticCode.MQ1008_InvalidOctalNumber, "octal", "0o");
            }
        }


        while (Position < Input.Length && FastCharacterClassifier.IsDigit(Input[Position]))
            Position++;


        if (Position < Input.Length && Input[Position] == '.')
            if (Position + 1 < Input.Length && FastCharacterClassifier.IsDigit(Input[Position + 1]))
            {
                Position++;
                while (Position < Input.Length && FastCharacterClassifier.IsDigit(Input[Position]))
                    Position++;

                var decimalTextEnd = Position;


                if (Position < Input.Length && (Input[Position] == 'd' || Input[Position] == 'D'))
                    Position++;

                var text = Input[start..decimalTextEnd];
                return AssignToken(new DecimalToken(text, new TextSpan(start, Position - start)));
            }


        var numericEnd = Position;
        var suffix = string.Empty;
        if (Position < Input.Length)
        {
            var ch = char.ToLowerInvariant(Input[Position]);


            if (ch == 'd')
            {
                Position++;
                var intText = Input[start..numericEnd];
                return AssignToken(new DecimalToken(intText, new TextSpan(start, Position - start)));
            }


            if (ch == 'u' && Position + 1 < Input.Length)
            {
                var nextCh = char.ToLowerInvariant(Input[Position + 1]);
                if (nextCh is 'i' or 'l' or 's' or 'b')
                {
                    suffix = Input.Substring(Position, 2).ToLowerInvariant();
                    Position += 2;
                }
            }
            else if (ch is 'i' or 'l' or 's' or 'b')
            {
                suffix = FastCharacterClassifier.CharToString(ch);
                Position++;
            }
        }

        var numText = Input[start..numericEnd];
        return AssignToken(new IntegerToken(numText, new TextSpan(start, Position - start), suffix));
    }

    private Token HandleInvalidBaseNumber(int start, DiagnosticCode code, string baseName, string prefix)
    {
        var scanEnd = Position + 2;
        while (scanEnd < Input.Length && FastCharacterClassifier.IsIdentifierContinue(Input[scanEnd]))
            scanEnd++;

        var invalidLiteral = Input[start..scanEnd];
        var span = new TextSpan(start, scanEnd - start);

        if (RecoverOnError)
        {
            Diagnostics.AddError(code, span, invalidLiteral);
            Position = scanEnd;
            return AssignToken(new ErrorToken(Input[start], span));
        }

        Position = scanEnd;
        throw new LexerException(
            $"Invalid {baseName} number literal '{invalidLiteral}'. Expected valid {baseName} digits after '{prefix}' prefix.",
            start,
            code);
    }

    private Token ScanStringLiteral()
    {
        var start = Position;
        var match = StringLiteralRegex.Match(Input, Position);

        if (match.Success && match.Index == Position)
        {
            Position += match.Length;
            var fullText = match.Value;
            var innerText = fullText[1..^1];

            if (TryFindInvalidEscapeSequence(innerText.AsSpan(), out var invalidEscape, out var invalidEscapeSpan))
            {
                var absoluteSpan = new TextSpan(start + 1 + invalidEscapeSpan.Start, invalidEscapeSpan.Length);
                var message = $"Invalid escape sequence '{invalidEscape}'.";

                if (RecoverOnError)
                    Diagnostics.AddError(DiagnosticCode.MQ1004_InvalidEscapeSequence, message, absoluteSpan);
                else
                    throw new LexerException(message, absoluteSpan.Start, DiagnosticCode.MQ1004_InvalidEscapeSequence);
            }

            var unescaped = innerText.Unescape();
            return AssignToken(new StringLiteralToken(unescaped, new TextSpan(start, match.Length)));
        }


        if (Position + 1 < Input.Length && Input[Position + 1] == '\'')
        {
            Position += 2;
            return AssignToken(new StringLiteralToken(string.Empty, new TextSpan(start, 2)));
        }


        if (RecoverOnError)
        {
            var end = Position + 1;
            while (end < Input.Length && Input[end] != '\n' && Input[end] != '\r')
                end++;

            var span = new TextSpan(start, end - start);
            Diagnostics.AddError(DiagnosticCode.MQ1002_UnterminatedString,
                "Unterminated string literal: missing closing '", span);

            Position = end;
            return AssignToken(new ErrorToken(Input[start..end], span));
        }

        throw new LexerException("Unterminated string literal: missing closing '", start, DiagnosticCode.MQ1002_UnterminatedString);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Token ScanSingleCharOperator()
    {
        var c = Input[Position];
        var start = Position;
        Position++;

        if (FastCharacterClassifier.TryGetSingleCharOperator(c, out var tokenType))
            return AssignToken(TokenFactory.Create(tokenType, start, FastCharacterClassifier.CharToString(c))
                               ?? new WordToken(FastCharacterClassifier.CharToString(c), new TextSpan(start, 1)));

        return AssignToken(new WordToken(FastCharacterClassifier.CharToString(c), new TextSpan(start, 1)));
    }

    private static bool TryFindInvalidEscapeSequence(ReadOnlySpan<char> value, out string invalidEscape,
        out TextSpan span)
    {
        for (var i = 0; i < value.Length; i++)
        {
            if (value[i] != '\\')
                continue;

            if (i + 1 >= value.Length)
            {
                invalidEscape = "\\";
                span = new TextSpan(i, 1);
                return true;
            }

            var next = value[i + 1];

            if (IsSimpleEscape(next))
            {
                i += 1;
                continue;
            }

            if (next == 'u')
                return TryValidateFixedLengthEscape(value, i, 4, out invalidEscape, out span);

            if (next == 'x')
                return TryValidateFixedLengthEscape(value, i, 2, out invalidEscape, out span);

            i += 1;
        }

        invalidEscape = string.Empty;
        span = TextSpan.Empty;
        return false;
    }

    private static bool TryValidateFixedLengthEscape(ReadOnlySpan<char> value, int start, int digitsLength,
        out string invalidEscape, out TextSpan span)
    {
        var availableDigits = Math.Min(digitsLength, value.Length - (start + 2));

        if (availableDigits == 0)
        {
            invalidEscape = string.Empty;
            span = TextSpan.Empty;
            return false;
        }

        if (availableDigits < digitsLength)
        {
            var invalidLength = Math.Min(2 + availableDigits, value.Length - start);
            invalidEscape = value.Slice(start, invalidLength).ToString();
            span = new TextSpan(start, invalidLength);
            return true;
        }

        for (var i = 0; i < digitsLength; i++)
        {
            if (Uri.IsHexDigit(value[start + 2 + i]))
                continue;

            invalidEscape = value.Slice(start, 2 + digitsLength).ToString();
            span = new TextSpan(start, 2 + digitsLength);
            return true;
        }

        invalidEscape = string.Empty;
        span = TextSpan.Empty;
        return false;
    }

    private static bool IsSimpleEscape(char value)
    {
        return value is '\\' or '\'' or '"' or 'n' or 'r' or 't' or 'b' or 'f' or 'e' or '0';
    }

    private Token ScanMultiCharOperator()
    {
        var start = Position;
        var c = Input[Position];
        var hasNext = Position + 1 < Input.Length;
        var next = hasNext ? Input[Position + 1] : '\0';

        switch (c)
        {
            case '<':
                if (next == '=')
                {
                    Position += 2;
                    return AssignToken(new LessEqualToken(new TextSpan(start, 2)));
                }

                if (next == '<')
                {
                    Position += 2;
                    return AssignToken(new LeftShiftToken(new TextSpan(start, 2)));
                }

                if (next == '>')
                {
                    Position += 2;
                    return AssignToken(new DiffToken(new TextSpan(start, 2)));
                }

                Position++;
                return AssignToken(new LessToken(new TextSpan(start, 1)));

            case '>':
                if (next == '=')
                {
                    Position += 2;
                    return AssignToken(new GreaterEqualToken(new TextSpan(start, 2)));
                }

                if (next == '>')
                {
                    Position += 2;
                    return AssignToken(new RightShiftToken(new TextSpan(start, 2)));
                }

                Position++;
                return AssignToken(new GreaterToken(new TextSpan(start, 1)));

            case '=':
                if (next == '>')
                {
                    Position += 2;
                    return AssignToken(new FatArrowToken(new TextSpan(start, 2)));
                }

                Position++;
                return AssignToken(new EqualityToken(new TextSpan(start, 1)));

            case '!':
                if (next == '=')
                {
                    Position += 2;
                    return AssignToken(new DiffToken("!=", new TextSpan(start, 2)));
                }

                Position++;
                return AssignToken(new WordToken("!", new TextSpan(start, 1)));

            case '&':
                Position++;
                return AssignToken(new AmpersandToken(new TextSpan(start, 1)));

            case '|':
                Position++;
                return AssignToken(new PipeToken(new TextSpan(start, 1)));

            case '^':
                Position++;
                return AssignToken(new CaretToken(new TextSpan(start, 1)));
        }

        Position++;
        return AssignToken(new WordToken(FastCharacterClassifier.CharToString(c), new TextSpan(start, 1)));
    }

    private Token ScanHashFrom()
    {
        var start = Position;
        var match = HFromRegex.Match(Input, Position);

        if (match.Success && match.Index == Position)
        {
            Position += match.Length;
            return AssignToken(new WordToken(match.Value, new TextSpan(start, match.Length)));
        }

        Position++;
        return AssignToken(new WordToken("#", new TextSpan(start, 1)));
    }

    private Token ScanDash()
    {
        var start = Position;


        if (Position + 1 < Input.Length && Input[Position + 1] == '-')
        {
            var match = LineCommentRegex.Match(Input, Position);
            if (match.Success && match.Index == Position)
            {
                Position += match.Length;
                return AssignToken(new CommentToken(match.Value, new TextSpan(start, match.Length)));
            }
        }


        if (Position + 1 < Input.Length && FastCharacterClassifier.IsDigit(Input[Position + 1]))
        {
            Position++;
            var numToken = ScanNumber();

            var numText = "-" + numToken.Value;
            return AssignToken(numToken switch
            {
                DecimalToken => new DecimalToken(numText, new TextSpan(start, numToken.Span.End - start)),
                IntegerToken it => new IntegerToken(numText, new TextSpan(start, numToken.Span.End - start),
                    it.Abbreviation),
                _ => numToken
            });
        }


        Position++;
        return AssignToken(new HyphenToken(new TextSpan(start, 1)));
    }

    private Token ScanSlash()
    {
        var start = Position;


        if (Position + 1 < Input.Length && Input[Position + 1] == '*')
        {
            var match = BlockCommentRegex.Match(Input, Position);
            if (match.Success && match.Index == Position)
            {
                Position += match.Length;
                return AssignToken(new CommentToken(match.Value, new TextSpan(start, match.Length)));
            }

            var span = new TextSpan(start, Input.Length - start);

            if (RecoverOnError)
            {
                Diagnostics.AddError(DiagnosticCode.MQ1005_UnterminatedBlockComment, span, Input[start..]);
                Position = Input.Length;
                return AssignToken(new ErrorToken(Input[start], span));
            }

            Position = Input.Length;
            throw new LexerException(
                "Unterminated block comment. Expected closing '*/' but reached end of input.",
                start,
                DiagnosticCode.MQ1005_UnterminatedBlockComment);
        }


        Position++;
        return AssignToken(new FSlashToken(new TextSpan(start, 1)));
    }

    private Token ScanDot()
    {
        var start = Position;


        if (Position + 1 < Input.Length && FastCharacterClassifier.IsDigit(Input[Position + 1]))
        {
            Position++;
            while (Position < Input.Length && FastCharacterClassifier.IsDigit(Input[Position]))
                Position++;

            if (Position < Input.Length && (Input[Position] == 'd' || Input[Position] == 'D'))
                Position++;

            var text = Input[start..Position];
            return AssignToken(new DecimalToken(text, new TextSpan(start, Position - start)));
        }

        Position++;
        return AssignToken(new DotToken(new TextSpan(start, 1)));
    }

    private Token ScanSquareBracket()
    {
        var start = Position;


        var match = BracketedColumnRegex.Match(Input, Position);
        if (match.Success && match.Index == Position)
        {
            Position += match.Length;
            var text = match.Value;


            if (IsSchemaContext)
            {
                var innerValue = text[1..^1];
                _pendingSchemaTokens.Enqueue(
                    int.TryParse(innerValue, out _)
                        ? new IntegerToken(innerValue, new TextSpan(start + 1, innerValue.Length), "i")
                        : new WordToken(innerValue, new TextSpan(start + 1, innerValue.Length)));
                _pendingSchemaTokens.Enqueue(new RightSquareBracketToken(new TextSpan(start + text.Length - 1, 1)));
                return AssignToken(new LeftSquareBracketToken(new TextSpan(start, 1)));
            }


            var columnName = text[1..^1];
            return AssignToken(new ColumnToken(columnName, new TextSpan(start, text.Length)));
        }


        Position++;
        return AssignToken(new LeftSquareBracketToken(new TextSpan(start, 1)));
    }

    private Token ScanColon()
    {
        var start = Position;


        if (Position + 1 < Input.Length && Input[Position + 1] == ':')
        {
            var match = FieldLinkRegex.Match(Input, Position);
            if (match.Success && match.Index == Position)
            {
                Position += match.Length;
                return AssignToken(new FieldLinkToken(match.Value, new TextSpan(start, match.Length)));
            }
        }

        Position++;
        return AssignToken(TokenFactory.Create(TokenType.Colon, start, ":") ??
                           new WordToken(":", new TextSpan(start, 1)));
    }

    private Token ScanUnknown()
    {
        var start = Position;
        var c = Input[start];
        var span = new TextSpan(start, 1);
        Position++;


        if (RecoverOnError)
        {
            Diagnostics.AddError(DiagnosticCode.MQ1001_UnknownToken, span, c.ToString());
            return AssignToken(new ErrorToken(c, span));
        }

        var remaining = Input[start..];
        throw new UnknownTokenException(start, c, remaining);
    }

    private bool ShouldSkipToken(Token token)
    {
        return (_skipWhiteSpaces && token.TokenType == TokenType.WhiteSpace) ||
               token.TokenType == TokenType.Comment ||
               (RecoverOnError && token.TokenType == TokenType.Error);
    }

    private Token SplitNumericAccessToken(Token numericAccessToken)
    {
        if (numericAccessToken is not NumericAccessToken numericToken)
            return numericAccessToken;

        var typeName = numericToken.Name;
        var sizeValue = numericToken.Index.ToString();
        var basePosition = numericAccessToken.Span.Start;

        var typeToken = ResolveSchemaTypeToken(typeName, basePosition);

        var leftBracketPos = basePosition + typeName.Length;
        var integerPos = leftBracketPos + 1;
        var rightBracketPos = integerPos + sizeValue.Length;

        _pendingSchemaTokens.Enqueue(new LeftSquareBracketToken(new TextSpan(leftBracketPos, 1)));
        _pendingSchemaTokens.Enqueue(new IntegerToken(sizeValue, new TextSpan(integerPos, sizeValue.Length), "i"));
        _pendingSchemaTokens.Enqueue(new RightSquareBracketToken(new TextSpan(rightBracketPos, 1)));

        return AssignToken(typeToken);
    }

    private Token SplitKeyAccessToken(Token keyAccessToken)
    {
        if (keyAccessToken is not KeyAccessToken keyToken)
            return keyAccessToken;

        var typeName = keyToken.Name;
        var innerContent = keyToken.Key.Trim('\'');
        var basePosition = keyAccessToken.Span.Start;

        var typeToken = ResolveSchemaTypeToken(typeName, basePosition);

        var leftBracketPos = basePosition + typeName.Length;
        _pendingSchemaTokens.Enqueue(new LeftSquareBracketToken(new TextSpan(leftBracketPos, 1)));

        var innerPos = leftBracketPos + 1;
        var innerTokens = LexInnerExpression(innerContent, innerPos);
        foreach (var innerToken in innerTokens)
            _pendingSchemaTokens.Enqueue(innerToken);

        var rightBracketPos = innerPos + innerContent.Length;
        _pendingSchemaTokens.Enqueue(new RightSquareBracketToken(new TextSpan(rightBracketPos, 1)));

        return AssignToken(typeToken);
    }

    private List<Token> LexInnerExpression(string content, int basePosition)
    {
        var tokens = new List<Token>();
        var pos = 0;

        while (pos < content.Length)
        {
            while (pos < content.Length && char.IsWhiteSpace(content[pos])) pos++;
            if (pos >= content.Length) break;

            var ch = content[pos];
            var spanStart = basePosition + pos;

            switch (ch)
            {
                case '+':
                    tokens.Add(new PlusToken(new TextSpan(spanStart, 1)));
                    pos++;
                    break;
                case '-':
                    tokens.Add(new HyphenToken(new TextSpan(spanStart, 1)));
                    pos++;
                    break;
                case '*':
                    tokens.Add(new StarToken(new TextSpan(spanStart, 1)));
                    pos++;
                    break;
                case '/':
                    tokens.Add(new FSlashToken(new TextSpan(spanStart, 1)));
                    pos++;
                    break;
                case '%':
                    tokens.Add(new ModuloToken(new TextSpan(spanStart, 1)));
                    pos++;
                    break;
                case '(':
                    tokens.Add(new LeftParenthesisToken(new TextSpan(spanStart, 1)));
                    pos++;
                    break;
                case ')':
                    tokens.Add(new RightParenthesisToken(new TextSpan(spanStart, 1)));
                    pos++;
                    break;
                default:
                    if (char.IsDigit(ch))
                    {
                        var start = pos;
                        while (pos < content.Length && char.IsDigit(content[pos])) pos++;
                        tokens.Add(new IntegerToken(content[start..pos], new TextSpan(spanStart, pos - start), "i"));
                    }
                    else if (char.IsLetter(ch) || ch == '_')
                    {
                        var start = pos;
                        while (pos < content.Length &&
                               (char.IsLetterOrDigit(content[pos]) || content[pos] == '_')) pos++;
                        tokens.Add(new WordToken(content[start..pos], new TextSpan(spanStart, pos - start)));
                    }
                    else
                    {
                        pos++;
                    }

                    break;
            }
        }

        return tokens;
    }

    private static Token ResolveSchemaTypeToken(string typeName, int position)
    {
        var span = new TextSpan(position, typeName.Length);
        var tokenType = KeywordLookup.GetSchemaKeywordType(typeName);
        return new SchemaToken(typeName, tokenType, span);
    }
}
