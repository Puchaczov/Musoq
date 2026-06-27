using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Tokens;

namespace Musoq.Parser.Lexing;

/// <summary>
///     High-performance lexer that uses direct character scanning instead of regex matching.
///     Provides 17-42x speedup over previous regex-based lexer for most queries.
/// </summary>
public sealed partial class Lexer : ILexer
{
    private static readonly Regex StringLiteralRegex = new(@"\G'([^'\\]|\\.)*'", RegexOptions.Compiled);
    private static readonly Regex HFromRegex = new(@"\G#[\w*?_]+", RegexOptions.Compiled);
    private static readonly Regex LineCommentRegex = new(@"\G--[^\r\n]*", RegexOptions.Compiled);
    private static readonly Regex BlockCommentRegex = new(@"\G/\*[\s\S]*?\*/", RegexOptions.Compiled);
    private static readonly Regex BracketedColumnRegex = new(@"\G\[[^\]]+\]", RegexOptions.Compiled);

    private static readonly Regex HexIntegerRegex = new(@"\G0[xX][0-9a-fA-F]+", RegexOptions.Compiled);
    private static readonly Regex BinaryIntegerRegex = new(@"\G0[bB][01]+", RegexOptions.Compiled);
    private static readonly Regex OctalIntegerRegex = new(@"\G0[oO][0-7]+", RegexOptions.Compiled);

    // Multi-word keyword regexes
    private static readonly Regex NotInRegex = new(@"\Gnot\s+in(?=\s|$)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex NotLikeRegex = new(@"\Gnot\s+like(?=\s|$)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex NotRLikeRegex =
        new(@"\Gnot\s+rlike(?=\s|$)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex UnionAllRegex =
        new(@"\Gunion\s+all(?=\s|$)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex GroupByRegex =
        new(@"\Ggroup\s+by(?=\s|$)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex OrderByRegex =
        new(@"\Gorder\s+by(?=\s|$)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex PartitionByRegex =
        new(@"\Gpartition\s+by(?=\s|$)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex CurrentRowRegex =
        new(@"\Gcurrent\s+row(?=\s|\)|$)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex InnerJoinRegex =
        new(@"\G(?:inner\s+)?join\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex OuterJoinRegex = new(@"\G(left|right|full)(?:\s+outer)?\s+join\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex SemiJoinRegex = new(@"\G(?:left\s+)?semi\s+join\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex AntiJoinRegex = new(@"\G(?:left\s+anti\s+semi|anti(?:\s+semi)?)\s+join\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex CrossJoinRegex = new(@"\Gcross\s+join\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex CrossApplyRegex =
        new(@"\Gcross\s+apply(?=\s|$)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex OuterApplyRegex =
        new(@"\Gouter\s+apply(?=\s|$)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex AsOfJoinRegex =
        new(@"\Gasof\s+(?:(left)\s+(?:outer\s+)?)?join\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private readonly Queue<Token> _pendingSchemaTokens = new();
    private readonly bool _skipWhiteSpaces;
    private Token _currentToken;
    private Token _lastToken;
    private Token? _resolvedToken0;
    private Token? _resolvedToken1;
    private Token? _resolvedToken2;
    private Token? _resolvedToken3;
    private Token? _resolvedToken4;
    private int _resolvedTokenCount;
    private int _nextResolvedTokenSlot;

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
            var startToken = _resolvedTokenCount > 0
                ? GetResolvedTokenByAge(0)
                : null;

            if (startToken == null)
                return string.Empty;

            var endToken = GetResolvedTokenByAge(_resolvedTokenCount - 1);
            if (endToken == null)
                return string.Empty;

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
            TrackResolvedToken(queuedToken);
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

        TrackResolvedToken(token);
        return token;
    }

    public Token NextOf(Regex regex, Func<string, Token> getToken)
    {
        ArgumentNullException.ThrowIfNull(regex);
        ArgumentNullException.ThrowIfNull(getToken);
        if (Position >= Input.Length)
            return AssignToken(new EndOfFileToken(new TextSpan(Input.Length, 0)));

        var match = regex.Match(Input, Position);
        if (!match.Success || match.Index != Position)
            throw new UnknownTokenException(Position, Input[Position],
                $"Unrecognized token at {Position} for {Input[Position..]}");

        var token = getToken(match.Value);
        Position += match.Length;
        TrackResolvedToken(token);
        return AssignToken(token);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Token AssignToken(Token token)
    {
        _lastToken = _currentToken;
        _currentToken = token;
        return token;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void TrackResolvedToken(Token token)
    {
        switch (_nextResolvedTokenSlot)
        {
            case 0:
                _resolvedToken0 = token;
                break;
            case 1:
                _resolvedToken1 = token;
                break;
            case 2:
                _resolvedToken2 = token;
                break;
            case 3:
                _resolvedToken3 = token;
                break;
            default:
                _resolvedToken4 = token;
                break;
        }

        _nextResolvedTokenSlot++;
        if (_nextResolvedTokenSlot == 5)
            _nextResolvedTokenSlot = 0;

        if (_resolvedTokenCount < 5)
            _resolvedTokenCount++;
    }

    private Token? GetResolvedTokenByAge(int age)
    {
        var slot = _resolvedTokenCount < 5 ? age : _nextResolvedTokenSlot + age;
        if (slot >= 5)
            slot -= 5;

        return slot switch
        {
            0 => _resolvedToken0,
            1 => _resolvedToken1,
            2 => _resolvedToken2,
            3 => _resolvedToken3,
            _ => _resolvedToken4
        };
    }
}
