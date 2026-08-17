using System.Collections.Frozen;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Lexing;
using Musoq.Parser.Nodes;
using Musoq.Parser.Tokens;

namespace Musoq.Parser;

/// <summary>
///     Parser for binary and text schema definitions.
///     Handles the interpretation schema syntax for defining data formats.
/// </summary>
public partial class SchemaParser
{
    /// <summary>
    ///     Token types that are allowed as field/schema names in schema context.
    ///     This includes SQL keywords and schema-specific keywords that should
    ///     be treated as identifiers when used as field names.
    /// </summary>
    private static readonly FrozenSet<TokenType> AllowedKeywordTokenTypes =
    new[]
    {
        // Text schema keywords
        TokenType.Rest, TokenType.Text, TokenType.End, TokenType.Binary,
        TokenType.Pattern, TokenType.Literal, TokenType.Until, TokenType.Between,
        TokenType.Chars, TokenType.Token, TokenType.Whitespace, TokenType.Switch,
        TokenType.Repeat, TokenType.Optional,

        // SQL keywords
        TokenType.And, TokenType.Or, TokenType.Not, TokenType.Where,
        TokenType.Select, TokenType.From, TokenType.Like, TokenType.NotLike,
        TokenType.RLike, TokenType.NotRLike, TokenType.As, TokenType.Is,
        TokenType.Null, TokenType.Union, TokenType.UnionAll, TokenType.Except,
        TokenType.Intersect, TokenType.GroupBy, TokenType.Having, TokenType.Contains,
        TokenType.Skip, TokenType.Take, TokenType.With, TokenType.InnerJoin,
        TokenType.OuterJoin, TokenType.CrossApply, TokenType.OuterApply, TokenType.On,
        TokenType.OrderBy, TokenType.Asc, TokenType.Desc, TokenType.Functions,
        TokenType.True, TokenType.False, TokenType.In, TokenType.NotIn, TokenType.Any, TokenType.Some, TokenType.All,
        TokenType.Table, TokenType.Couple, TokenType.Case, TokenType.When,
        TokenType.Then, TokenType.Else, TokenType.Distinct, TokenType.ColumnKeyword,

        // Schema-specific keywords
        TokenType.LittleEndian, TokenType.BigEndian, TokenType.ByteType,
        TokenType.SByteType, TokenType.ShortType, TokenType.UShortType,
        TokenType.IntType, TokenType.UIntType, TokenType.LongType, TokenType.ULongType,
        TokenType.FloatType, TokenType.DoubleType, TokenType.BitsType, TokenType.Align,
        TokenType.StringType, TokenType.Utf8, TokenType.Utf16Le, TokenType.Utf16Be,
        TokenType.Ascii, TokenType.Latin1, TokenType.Ebcdic, TokenType.Trim,
        TokenType.RTrim, TokenType.LTrim, TokenType.NullTerm, TokenType.Check,
        TokenType.At, TokenType.Substream, TokenType.Nested, TokenType.Escaped, TokenType.Greedy,
        TokenType.Lazy, TokenType.Lower, TokenType.Upper, TokenType.Capture,
        TokenType.Extends
    }.ToFrozenSet();

    private readonly ILexer _lexer;
    private bool _hasReplacedToken;
    private int _pendingGenericGreaterTokens;
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

    private Token Current =>
        _pendingGenericGreaterTokens > 0
            ? new GreaterToken(TextSpan.Empty)
            : _savedTokenBeforePeek ??
              (_hasReplacedToken && _replacedToken != null ? _replacedToken : _lexer.Current());

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
}
