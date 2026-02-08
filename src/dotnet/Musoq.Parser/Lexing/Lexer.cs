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


        var funcMatch = FunctionRegex.Match(Input, Position);
        if (funcMatch.Success && funcMatch.Index == Position)
        {
            Position += funcMatch.Length;
            return AssignToken(new FunctionToken(funcMatch.Value, new TextSpan(start, funcMatch.Length)));
        }


        var methodMatch = MethodAccessRegex.Match(Input, Position);
        if (methodMatch.Success && methodMatch.Index == Position)
        {
            Position += methodMatch.Length;
            return AssignToken(new MethodAccessToken(methodMatch.Groups[1].Value,
                new TextSpan(start, methodMatch.Length)));
        }


        var aliasedStarMatch = AliasedStarRegex.Match(Input, Position);
        if (aliasedStarMatch.Success && aliasedStarMatch.Index == Position)
        {
            Position += aliasedStarMatch.Length;
            return AssignToken(new AliasedStarToken(aliasedStarMatch.Value,
                new TextSpan(start, aliasedStarMatch.Length)));
        }


        var numAccessMatch = NumericAccessRegex.Match(Input, Position);
        if (numAccessMatch.Success && numAccessMatch.Index == Position)
        {
            Position += numAccessMatch.Length;
            var name = numAccessMatch.Groups[1].Value;
            var index = numAccessMatch.Groups[2].Value;
            return AssignToken(new NumericAccessToken(name, index, new TextSpan(start, numAccessMatch.Length)));
        }

        var keyAccessConstMatch = KeyAccessConstRegex.Match(Input, Position);
        if (keyAccessConstMatch.Success && keyAccessConstMatch.Index == Position)
        {
            Position += keyAccessConstMatch.Length;
            var name = keyAccessConstMatch.Groups[1].Value;
            var key = keyAccessConstMatch.Groups[2].Value;
            return AssignToken(new KeyAccessToken(name, key, new TextSpan(start, keyAccessConstMatch.Length)));
        }

        var keyAccessVarMatch = KeyAccessVarRegex.Match(Input, Position);
        if (keyAccessVarMatch.Success && keyAccessVarMatch.Index == Position)
        {
            Position += keyAccessVarMatch.Length;
            var name = keyAccessVarMatch.Groups[1].Value;
            var key = keyAccessVarMatch.Groups[2].Value;
            return AssignToken(new KeyAccessToken(name, key, new TextSpan(start, keyAccessVarMatch.Length)));
        }


        while (Position < Input.Length && FastCharacterClassifier.IsIdentifierContinue(Input[Position]))
            Position++;

        var text = Input[start..Position];
        var span = new TextSpan(start, Position - start);


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


        switch (c)
        {
            case 'n':
                var notInMatch = NotInRegex.Match(Input, Position);
                if (notInMatch.Success && notInMatch.Index == Position)
                {
                    Position += notInMatch.Length;
                    return new NotInToken(new TextSpan(start, notInMatch.Length));
                }

                var notLikeMatch = NotLikeRegex.Match(Input, Position);
                if (notLikeMatch.Success && notLikeMatch.Index == Position)
                {
                    Position += notLikeMatch.Length;
                    return new NotLikeToken(new TextSpan(start, notLikeMatch.Length));
                }

                var notRLikeMatch = NotRLikeRegex.Match(Input, Position);
                if (notRLikeMatch.Success && notRLikeMatch.Index == Position)
                {
                    Position += notRLikeMatch.Length;
                    return new NotRLikeToken(new TextSpan(start, notRLikeMatch.Length));
                }

                break;

            case 'u':
                var unionAllMatch = UnionAllRegex.Match(Input, Position);
                if (unionAllMatch.Success && unionAllMatch.Index == Position)
                {
                    Position += unionAllMatch.Length;
                    return new UnionAllToken(new TextSpan(start, unionAllMatch.Length));
                }

                break;

            case 'g':
                var groupByMatch = GroupByRegex.Match(Input, Position);
                if (groupByMatch.Success && groupByMatch.Index == Position)
                {
                    Position += groupByMatch.Length;
                    return new GroupByToken(new TextSpan(start, groupByMatch.Length));
                }

                break;

            case 'o':
                var orderByMatch = OrderByRegex.Match(Input, Position);
                if (orderByMatch.Success && orderByMatch.Index == Position)
                {
                    Position += orderByMatch.Length;
                    return new OrderByToken(new TextSpan(start, orderByMatch.Length));
                }

                var outerApplyMatch = OuterApplyRegex.Match(Input, Position);
                if (outerApplyMatch.Success && outerApplyMatch.Index == Position)
                {
                    Position += outerApplyMatch.Length;
                    return new OuterApplyToken(new TextSpan(start, outerApplyMatch.Length));
                }

                break;

            case 'j':
                var innerJoinMatch = InnerJoinRegex.Match(Input, Position);
                if (innerJoinMatch.Success && innerJoinMatch.Index == Position)
                {
                    Position += innerJoinMatch.Length;
                    return new InnerJoinToken(new TextSpan(start, innerJoinMatch.Length));
                }

                break;

            case 'i':
                var innerJoinMatch2 = InnerJoinRegex.Match(Input, Position);
                if (innerJoinMatch2.Success && innerJoinMatch2.Index == Position)
                {
                    Position += innerJoinMatch2.Length;
                    return new InnerJoinToken(new TextSpan(start, innerJoinMatch2.Length));
                }

                break;

            case 'l':
            case 'r':
                var outerJoinMatch = OuterJoinRegex.Match(Input, Position);
                if (outerJoinMatch.Success && outerJoinMatch.Index == Position)
                {
                    Position += outerJoinMatch.Length;
                    var isLeft = char.ToLowerInvariant(Input[start]) == 'l';
                    return new OuterJoinToken(isLeft ? OuterJoinType.Left : OuterJoinType.Right,
                        new TextSpan(start, outerJoinMatch.Length));
                }

                break;

            case 'c':
                var crossApplyMatch = CrossApplyRegex.Match(Input, Position);
                if (crossApplyMatch.Success && crossApplyMatch.Index == Position)
                {
                    Position += crossApplyMatch.Length;
                    return new CrossApplyToken(new TextSpan(start, crossApplyMatch.Length));
                }

                break;
        }

        return null;
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
            }
            else if (next == 'b')
            {
                var binMatch = BinaryIntegerRegex.Match(Input, Position);
                if (binMatch.Success && binMatch.Index == Position)
                {
                    Position += binMatch.Length;
                    return AssignToken(new BinaryIntegerToken(binMatch.Value, new TextSpan(start, binMatch.Length)));
                }
            }
            else if (next == 'o')
            {
                var octMatch = OctalIntegerRegex.Match(Input, Position);
                if (octMatch.Success && octMatch.Index == Position)
                {
                    Position += octMatch.Length;
                    return AssignToken(new OctalIntegerToken(octMatch.Value, new TextSpan(start, octMatch.Length)));
                }
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

    private Token ScanStringLiteral()
    {
        var start = Position;
        var match = StringLiteralRegex.Match(Input, Position);

        if (match.Success && match.Index == Position)
        {
            Position += match.Length;
            var fullText = match.Value;
            var innerText = fullText[1..^1];
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
                "Unterminated string literal", span);

            Position = end;
            return AssignToken(new ErrorToken(Input[start..end], span));
        }

        throw new UnknownTokenException(Position, '\'', "Unterminated string literal");
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

        return AssignToken(new WordToken(FastCharacterClassifier.CharToString(c), span));
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
