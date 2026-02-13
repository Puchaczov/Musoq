#nullable enable
using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Musoq.Parser.Tokens;

namespace Musoq.Parser.Lexing;

/// <summary>
///     Factory for creating token instances based on token type.
///     Uses dictionary-based dispatch for O(1) token creation.
/// </summary>
public static class TokenFactory
{
    /// <summary>
    ///     Delegate for creating a token from position, text, and optional match data.
    /// </summary>
    public delegate Token TokenCreator(int position, string tokenText, Match? match);

    private static readonly FrozenDictionary<TokenType, TokenCreator> SimpleTokenFactories;
    private static readonly FrozenDictionary<TokenType, TokenCreator> ComplexTokenFactories;

    static TokenFactory()
    {
        // Simple tokens that only need position and text length
        var simpleFactories = new Dictionary<TokenType, TokenCreator>
        {
            { TokenType.Desc, (pos, text, _) => new DescToken(new TextSpan(pos, text.Length)) },
            { TokenType.Asc, (pos, text, _) => new AscToken(new TextSpan(pos, text.Length)) },
            { TokenType.And, (pos, text, _) => new AndToken(new TextSpan(pos, text.Length)) },
            { TokenType.Comma, (pos, text, _) => new CommaToken(new TextSpan(pos, text.Length)) },
            { TokenType.Diff, (pos, text, _) => new DiffToken(new TextSpan(pos, text.Length)) },
            { TokenType.Equality, (pos, text, _) => new EqualityToken(new TextSpan(pos, text.Length)) },
            { TokenType.FSlash, (pos, text, _) => new FSlashToken(new TextSpan(pos, text.Length)) },
            { TokenType.Greater, (pos, text, _) => new GreaterToken(new TextSpan(pos, text.Length)) },
            { TokenType.GreaterEqual, (pos, text, _) => new GreaterEqualToken(new TextSpan(pos, text.Length)) },
            { TokenType.Hyphen, (pos, text, _) => new HyphenToken(new TextSpan(pos, text.Length)) },
            { TokenType.LeftParenthesis, (pos, text, _) => new LeftParenthesisToken(new TextSpan(pos, text.Length)) },
            { TokenType.Less, (pos, text, _) => new LessToken(new TextSpan(pos, text.Length)) },
            { TokenType.LessEqual, (pos, text, _) => new LessEqualToken(new TextSpan(pos, text.Length)) },
            { TokenType.Mod, (pos, text, _) => new ModuloToken(new TextSpan(pos, text.Length)) },
            { TokenType.Not, (pos, text, _) => new NotToken(new TextSpan(pos, text.Length)) },
            { TokenType.Or, (pos, text, _) => new OrToken(new TextSpan(pos, text.Length)) },
            { TokenType.Plus, (pos, text, _) => new PlusToken(new TextSpan(pos, text.Length)) },
            { TokenType.RightParenthesis, (pos, text, _) => new RightParenthesisToken(new TextSpan(pos, text.Length)) },
            { TokenType.Star, (pos, text, _) => new StarToken(new TextSpan(pos, text.Length)) },
            { TokenType.Where, (pos, text, _) => new WhereToken(new TextSpan(pos, text.Length)) },
            { TokenType.WhiteSpace, (pos, text, _) => new WhiteSpaceToken(new TextSpan(pos, text.Length)) },
            { TokenType.From, (pos, text, _) => new FromToken(new TextSpan(pos, text.Length)) },
            { TokenType.Select, (pos, text, _) => new SelectToken(new TextSpan(pos, text.Length)) },
            { TokenType.Like, (pos, text, _) => new LikeToken(new TextSpan(pos, text.Length)) },
            { TokenType.NotLike, (pos, text, _) => new NotLikeToken(new TextSpan(pos, text.Length)) },
            { TokenType.RLike, (pos, text, _) => new RLikeToken(new TextSpan(pos, text.Length)) },
            { TokenType.NotRLike, (pos, text, _) => new NotRLikeToken(new TextSpan(pos, text.Length)) },
            { TokenType.As, (pos, text, _) => new AsToken(new TextSpan(pos, text.Length)) },
            { TokenType.Except, (pos, text, _) => new ExceptToken(new TextSpan(pos, text.Length)) },
            { TokenType.Union, (pos, text, _) => new UnionToken(new TextSpan(pos, text.Length)) },
            { TokenType.Intersect, (pos, text, _) => new IntersectToken(new TextSpan(pos, text.Length)) },
            { TokenType.UnionAll, (pos, text, _) => new UnionAllToken(new TextSpan(pos, text.Length)) },
            { TokenType.Dot, (pos, text, _) => new DotToken(new TextSpan(pos, text.Length)) },
            { TokenType.GroupBy, (pos, text, _) => new GroupByToken(new TextSpan(pos, text.Length)) },
            { TokenType.Having, (pos, text, _) => new HavingToken(new TextSpan(pos, text.Length)) },
            { TokenType.Contains, (pos, text, _) => new ContainsToken(new TextSpan(pos, text.Length)) },
            { TokenType.With, (pos, text, _) => new WithToken(new TextSpan(pos, text.Length)) },
            { TokenType.On, (pos, text, _) => new OnToken(new TextSpan(pos, text.Length)) },
            { TokenType.InnerJoin, (pos, text, _) => new InnerJoinToken(new TextSpan(pos, text.Length)) },
            { TokenType.CrossApply, (pos, text, _) => new CrossApplyToken(new TextSpan(pos, text.Length)) },
            { TokenType.OuterApply, (pos, text, _) => new OuterApplyToken(new TextSpan(pos, text.Length)) },
            { TokenType.Is, (pos, text, _) => new IsToken(new TextSpan(pos, text.Length)) },
            { TokenType.Functions, (pos, text, _) => new FunctionsToken(new TextSpan(pos, text.Length)) },
            { TokenType.Null, (pos, text, _) => new NullToken(new TextSpan(pos, text.Length)) },
            { TokenType.OrderBy, (pos, text, _) => new OrderByToken(new TextSpan(pos, text.Length)) },
            { TokenType.True, (pos, text, _) => new TrueToken(new TextSpan(pos, text.Length)) },
            { TokenType.False, (pos, text, _) => new FalseToken(new TextSpan(pos, text.Length)) },
            { TokenType.In, (pos, text, _) => new InToken(new TextSpan(pos, text.Length)) },
            { TokenType.NotIn, (pos, text, _) => new NotInToken(new TextSpan(pos, text.Length)) },
            { TokenType.Between, (pos, text, _) => new BetweenToken(new TextSpan(pos, text.Length)) },
            { TokenType.Colon, (pos, text, _) => new ColonToken(new TextSpan(pos, text.Length)) },
            { TokenType.Table, (pos, text, _) => new TableToken(new TextSpan(pos, text.Length)) },
            { TokenType.LBracket, (pos, text, _) => new LBracketToken(new TextSpan(pos, text.Length)) },
            { TokenType.RBracket, (pos, text, _) => new RBracketToken(new TextSpan(pos, text.Length)) },
            {
                TokenType.LeftSquareBracket,
                (pos, text, _) => new LeftSquareBracketToken(new TextSpan(pos, text.Length))
            },
            {
                TokenType.RightSquareBracket,
                (pos, text, _) => new RightSquareBracketToken(new TextSpan(pos, text.Length))
            },
            { TokenType.Semicolon, (pos, text, _) => new SemicolonToken(new TextSpan(pos, text.Length)) },
            { TokenType.Couple, (pos, text, _) => new CoupleToken(new TextSpan(pos, text.Length)) },
            { TokenType.Case, (pos, text, _) => new CaseToken(new TextSpan(pos, text.Length)) },
            { TokenType.When, (pos, text, _) => new WhenToken(new TextSpan(pos, text.Length)) },
            { TokenType.Then, (pos, text, _) => new ThenToken(new TextSpan(pos, text.Length)) },
            { TokenType.Else, (pos, text, _) => new ElseToken(new TextSpan(pos, text.Length)) },
            { TokenType.End, (pos, text, _) => new EndToken(new TextSpan(pos, text.Length)) },
            { TokenType.Distinct, (pos, text, _) => new DistinctToken(new TextSpan(pos, text.Length)) },
            { TokenType.Ampersand, (pos, text, _) => new AmpersandToken(new TextSpan(pos, text.Length)) },
            { TokenType.Pipe, (pos, text, _) => new PipeToken(new TextSpan(pos, text.Length)) },
            { TokenType.Caret, (pos, text, _) => new CaretToken(new TextSpan(pos, text.Length)) },
            { TokenType.LeftShift, (pos, text, _) => new LeftShiftToken(new TextSpan(pos, text.Length)) },
            { TokenType.RightShift, (pos, text, _) => new RightShiftToken(new TextSpan(pos, text.Length)) },
            { TokenType.FatArrow, (pos, text, _) => new FatArrowToken(new TextSpan(pos, text.Length)) },
            { TokenType.QuestionMark, (pos, text, _) => new QuestionMarkToken(new TextSpan(pos, text.Length)) }
        };

        // Complex tokens that need additional processing
        var complexFactories = new Dictionary<TokenType, TokenCreator>
        {
            { TokenType.Function, (pos, text, _) => new FunctionToken(text, new TextSpan(pos, text.Length)) },
            {
                TokenType.Decimal,
                (pos, text, _) => new DecimalToken(text.TrimEnd('d', 'D'), new TextSpan(pos, text.Length))
            },
            { TokenType.Integer, (pos, text, _) => CreateIntegerToken(pos, text) },
            {
                TokenType.HexadecimalInteger,
                (pos, text, _) => new HexIntegerToken(text, new TextSpan(pos, text.Length))
            },
            { TokenType.BinaryInteger, (pos, text, _) => new BinaryIntegerToken(text, new TextSpan(pos, text.Length)) },
            { TokenType.OctalInteger, (pos, text, _) => new OctalIntegerToken(text, new TextSpan(pos, text.Length)) },
            { TokenType.AliasedStar, (pos, text, _) => new AliasedStarToken(text, new TextSpan(pos, text.Length)) },
            { TokenType.Identifier, (pos, text, _) => new ColumnToken(text, new TextSpan(pos, text.Length)) },
            { TokenType.Property, (pos, text, _) => new AccessPropertyToken(text, new TextSpan(pos, text.Length)) },
            { TokenType.NumericAccess, CreateNumericAccessToken },
            { TokenType.KeyAccess, CreateKeyAccessToken },
            { TokenType.Skip, (pos, text, _) => new SkipToken(text, new TextSpan(pos, text.Length)) },
            { TokenType.Take, (pos, text, _) => new TakeToken(text, new TextSpan(pos, text.Length)) },
            { TokenType.OuterJoin, CreateOuterJoinToken },
            { TokenType.MethodAccess, CreateMethodAccessToken },
            { TokenType.FieldLink, (pos, text, _) => new FieldLinkToken(text, new TextSpan(pos, text.Length)) },
            { TokenType.Comment, (pos, text, _) => new CommentToken(text, new TextSpan(pos, text.Length)) },
            { TokenType.Word, (pos, text, _) => new WordToken(text, new TextSpan(pos, text.Length)) }
        };

        SimpleTokenFactories = simpleFactories.ToFrozenDictionary();
        ComplexTokenFactories = complexFactories.ToFrozenDictionary();
    }

    /// <summary>
    ///     Creates a token of the specified type.
    /// </summary>
    /// <param name="tokenType">The type of token to create.</param>
    /// <param name="position">The position in the input.</param>
    /// <param name="tokenText">The text of the token.</param>
    /// <param name="match">Optional regex match for complex tokens.</param>
    /// <returns>The created token, or null if the type is not supported.</returns>
    public static Token? Create(TokenType tokenType, int position, string tokenText, Match? match = null)
    {
        if (SimpleTokenFactories.TryGetValue(tokenType, out var simpleFactory))
            return simpleFactory(position, tokenText, match);

        if (ComplexTokenFactories.TryGetValue(tokenType, out var complexFactory))
            return complexFactory(position, tokenText, match);

        return null;
    }

    /// <summary>
    ///     Creates a schema keyword token.
    /// </summary>
    /// <param name="tokenType">The schema token type.</param>
    /// <param name="position">The position in the input.</param>
    /// <param name="tokenText">The text of the token.</param>
    /// <returns>A schema keyword token.</returns>
    public static Token CreateSchemaKeywordToken(TokenType tokenType, int position, string tokenText)
    {
        return new SchemaKeywordToken(tokenText, tokenType, new TextSpan(position, tokenText.Length));
    }

    /// <summary>
    ///     Creates a string literal token.
    /// </summary>
    /// <param name="position">The position in the input.</param>
    /// <param name="value">The unescaped value of the string.</param>
    /// <param name="originalLength">The original length including quotes.</param>
    /// <returns>A string literal token.</returns>
    public static Token CreateStringLiteralToken(int position, string value, int originalLength)
    {
        return new StringLiteralToken(value, new TextSpan(position + 1, originalLength));
    }

    /// <summary>
    ///     Creates an empty word token (for empty string literals).
    /// </summary>
    /// <param name="position">The position in the input.</param>
    /// <returns>An empty word token.</returns>
    public static Token CreateEmptyWordToken(int position)
    {
        return new WordToken(string.Empty, new TextSpan(position + 1, 0));
    }

    private static Token CreateIntegerToken(int position, string tokenText)
    {
        var abbreviation = GetAbbreviation(tokenText);
        var unAbbreviatedValue = abbreviation.Length > 0
            ? tokenText.Replace(abbreviation, string.Empty)
            : tokenText;
        return new IntegerToken(unAbbreviatedValue, new TextSpan(position, tokenText.Length), abbreviation);
    }

    private static Token CreateNumericAccessToken(int position, string tokenText, Match? match)
    {
        if (match == null) throw new ArgumentNullException(nameof(match));
        return new NumericAccessToken(match.Groups[1].Value, match.Groups[2].Value,
            new TextSpan(position, tokenText.Length));
    }

    private static Token CreateKeyAccessToken(int position, string tokenText, Match? match)
    {
        if (match == null) throw new ArgumentNullException(nameof(match));
        return new KeyAccessToken(match.Groups[1].Value, match.Groups[2].Value,
            new TextSpan(position, tokenText.Length));
    }

    private static Token CreateOuterJoinToken(int position, string tokenText, Match? match)
    {
        if (match == null) throw new ArgumentNullException(nameof(match));
        var type = match.Groups[1].Value.Equals("left", StringComparison.OrdinalIgnoreCase)
            ? OuterJoinType.Left
            : OuterJoinType.Right;
        return new OuterJoinToken(type, new TextSpan(position, tokenText.Length));
    }

    private static Token CreateMethodAccessToken(int position, string tokenText, Match? match)
    {
        if (match == null) throw new ArgumentNullException(nameof(match));
        return new MethodAccessToken(match.Groups[1].Value,
            new TextSpan(position, match.Groups[1].Value.Length));
    }

    private static string GetAbbreviation(string tokenText)
    {
        if (tokenText.Length == 1) return string.Empty;

        var position = 1;
        while (position < tokenText.Length && char.IsDigit(tokenText[position]))
            position++;

        return tokenText[position..];
    }
}
