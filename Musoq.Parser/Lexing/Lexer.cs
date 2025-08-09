using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Helpers;
using Musoq.Parser.Nodes;
using Musoq.Parser.Tokens;

namespace Musoq.Parser.Lexing;

public class Lexer : LexerBase<Token>
{
    private readonly bool _skipWhiteSpaces;

    private readonly Regex[] _decimalCandidates =
    [
        new(TokenRegexDefinition.KDecimalWithDot),
        new(TokenRegexDefinition.KDecimalWithSuffix),
        new(TokenRegexDefinition.KDecimalWithDotAndSuffix)
    ];
        
    private readonly List<Token> _alreadyResolvedTokens = [];

    /// <summary>
    ///     Initialize instance.
    /// </summary>
    /// <param name="input">The query.</param>
    /// <param name="skipWhiteSpaces">Should skip whitespaces?</param>
    public Lexer(string input, bool skipWhiteSpaces) :
        base(ValidateInput(input), new NoneToken(), DefinitionSets.General)
    {
        _skipWhiteSpaces = skipWhiteSpaces;
    }

    private static string ValidateInput(string input)
    {
        if (input == null)
            throw ParserValidationException.ForNullInput();
        
        if (string.IsNullOrWhiteSpace(input))
            throw ParserValidationException.ForEmptyInput();
        
        return input;
    }

    /// <summary>
    /// Gets the part of resolved query
    /// </summary>
    public string AlreadyResolvedQueryPart
    {
        get
        {
            //get first of last 5 tokens
            var startToken = _alreadyResolvedTokens.TakeLast(5).FirstOrDefault();
                
            if (startToken == null)
                return string.Empty;
                
            var endToken = _alreadyResolvedTokens.Last();
                
            return Input.Substring(startToken.Span.Start, endToken.Span.End - startToken.Span.Start);
        }
    }

    /// <summary>
    ///     Resolve the statement type.
    /// </summary>
    /// <param name="tokenText">Text that match some definition</param>
    /// <param name="regex">Definition that text matched.</param>
    /// <returns>Statement type.</returns>
    private TokenType GetTokenCandidate(string tokenText, string regex)
    {
        var loweredToken = tokenText.ToLowerInvariant();

        if (regex == TokenRegexDefinition.KComment)
            return TokenType.Comment;
            
        if (regex == TokenRegexDefinition.Function)
            return TokenType.Function;

        if (regex == TokenRegexDefinition.KAliasedStar)
            return TokenType.AliasedStar;
            
        switch (loweredToken)
        {
            case DescToken.TokenText:
                return TokenType.Desc;
            case AscToken.TokenText:
                return TokenType.Asc;
            case AndToken.TokenText:
                return TokenType.And;
            case CommaToken.TokenText:
                return TokenType.Comma;
            case DiffToken.TokenText:
                return TokenType.Diff;
            case GreaterToken.TokenText:
                return TokenType.Greater;
            case GreaterEqualToken.TokenText:
                return TokenType.GreaterEqual;
            case HyphenToken.TokenText:
                return TokenType.Hyphen;
            case LeftParenthesisToken.TokenText:
                return TokenType.LeftParenthesis;
            case RightParenthesisToken.TokenText:
                return TokenType.RightParenthesis;
            case LessToken.TokenText:
                return TokenType.Less;
            case LessEqualToken.TokenText:
                return TokenType.LessEqual;
            case ModuloToken.TokenText:
                return TokenType.Mod;
            case NotToken.TokenText:
                return TokenType.Not;
            case OrToken.TokenText:
                return TokenType.Or;
            case PlusToken.TokenText:
                return TokenType.Plus;
            case FSlashToken.TokenText:
                return TokenType.FSlash;
            case StarToken.TokenText:
                return TokenType.Star;
            case WhereToken.TokenText:
                return TokenType.Where;
            case WhiteSpaceToken.TokenText:
                return TokenType.WhiteSpace;
            case SelectToken.TokenText:
                return TokenType.Select;
            case FromToken.TokenText:
                return TokenType.From;
            case EqualityToken.TokenText:
                return TokenType.Equality;
            case LikeToken.TokenText:
                return TokenType.Like;
            case RLikeToken.TokenText:
                return TokenType.RLike;
            case ContainsToken.TokenText:
                return TokenType.Contains;
            case AsToken.TokenText:
                return TokenType.As;
            case SetOperatorToken.ExceptOperatorText:
                return TokenType.Except;
            case SetOperatorToken.IntersectOperatorText:
                return TokenType.Intersect;
            case SetOperatorToken.UnionOperatorText:
                return TokenType.Union;
            case DotToken.TokenText:
                return TokenType.Dot;
            case HavingToken.TokenText:
                return TokenType.Having;
            case TakeToken.TokenText:
                return TokenType.Take;
            case SkipToken.TokenText:
                return TokenType.Skip;
            case WithToken.TokenText:
                return TokenType.With;
            case OnToken.TokenText:
                return TokenType.On;
            case IsToken.TokenText:
                return TokenType.Is;
            case NullToken.TokenText:
                return TokenType.Null;
            case TrueToken.TokenText:
                return TokenType.True;
            case FalseToken.TokenText:
                return TokenType.False;
            case InToken.TokenText:
                return TokenType.In;
            case TableToken.TokenText:
                return TokenType.Table;
            case LBracketToken.TokenText:
                return TokenType.LBracket;
            case RBracketToken.TokenText:
                return TokenType.RBracket;
            case SemicolonToken.TokenText:
                return TokenType.Semicolon;
            case CoupleToken.TokenText:
                return TokenType.Couple;
            case CaseToken.TokenText:
                return TokenType.Case;
            case WhenToken.TokenText:
                return TokenType.When;
            case ThenToken.TokenText:
                return TokenType.Then;
            case ElseToken.TokenText:
                return TokenType.Else;
            case EndToken.TokenText:
                return TokenType.End;
            case WordToken.EmptyTokenText:
                return TokenType.Word;
        }

        if (string.IsNullOrWhiteSpace(tokenText))
            return TokenType.WhiteSpace;
            
        if (regex == TokenRegexDefinition.KNotIn)
            return TokenType.NotIn;
        if (regex == TokenRegexDefinition.KNotLike)
            return TokenType.NotLike;
        if (regex == TokenRegexDefinition.KRNotLike)
            return TokenType.NotRLike;
        if (regex == TokenRegexDefinition.KMethodAccess)
            return TokenType.MethodAccess;
        if (regex == TokenRegexDefinition.KKeyObjectAccessConst)
            return TokenType.KeyAccess;
        if (regex == TokenRegexDefinition.KKeyObjectAccessVariable)
            return TokenType.KeyAccess;
        if (regex == TokenRegexDefinition.KNumericArrayAccess)
            return TokenType.NumericAccess;
        if (regex == TokenRegexDefinition.KGroupBy)
            return TokenType.GroupBy;
        if (regex == TokenRegexDefinition.KUnionAll)
            return TokenType.UnionAll;
        if (regex == TokenRegexDefinition.KOrderBy)
            return TokenType.OrderBy;
        if (regex == TokenRegexDefinition.Function)
            return TokenType.Function;
        var last = Current();
        if (regex == TokenRegexDefinition.KColumn && last is {TokenType: TokenType.Dot})
            return TokenType.Property;
        if (regex == TokenRegexDefinition.KColumn)
            return TokenType.Identifier;
        if (regex == TokenRegexDefinition.KInnerJoin)
            return TokenType.InnerJoin;
        if (regex == TokenRegexDefinition.KOuterJoin)
            return TokenType.OuterJoin;
        if (regex == TokenRegexDefinition.KCrossApply)
            return TokenType.CrossApply;
        if (regex == TokenRegexDefinition.KOuterApply)
            return TokenType.OuterApply;
        if (regex == TokenRegexDefinition.KHFrom)
            return TokenType.Word;
        if (regex == TokenRegexDefinition.KFieldLink)
            return TokenType.FieldLink;
        if (regex != TokenRegexDefinition.KDecimalOrInteger) 
            return TokenType.Word;
            
        if (_decimalCandidates.Any(decimalCandidate => decimalCandidate.IsMatch(tokenText)))
            return TokenType.Decimal;
                
        var regexSignedInteger = new Regex(TokenRegexDefinition.KSignedInteger);
                
        if (regexSignedInteger.IsMatch(tokenText))
            return TokenType.Integer;
            
        var regexUnsignedInteger = new Regex(TokenRegexDefinition.KUnsignedInteger);
            
        if (regexUnsignedInteger.IsMatch(tokenText))
            return TokenType.Integer;

        throw new NotSupportedException($"Token {tokenText} is not supported.");
    }

    /// <summary>
    ///     The token regexes set.
    /// </summary>
    public static class TokenRegexDefinition
    {
        private const string Keyword = @"(?<=[\s]{1,}|^){keyword}(?=[\s]{1,}|$)";
        public const string Function = @"[a-zA-Z_]{1,}[a-zA-Z1-9_-]{0,}[\d]*(?=[\(])";

        public static readonly string KAnd = Format(Keyword, AndToken.TokenText);
        public static readonly string KComma = CommaToken.TokenText;
        public static readonly string KDiff = DiffToken.TokenText;
        public static readonly string KFSlashToken = Format(Keyword, FSlashToken.TokenText);
        public static readonly string KGreater = Format(Keyword, GreaterToken.TokenText);
        public static readonly string KGreaterEqual = Format(Keyword, GreaterEqualToken.TokenText);
        public static readonly string KHyphen = $@"\{HyphenToken.TokenText}";
        public static readonly string KLeftParenthesis = $@"\{LeftParenthesisToken.TokenText}";
        public static readonly string KLess = Format(Keyword, LessToken.TokenText);
        public static readonly string KLessEqual = Format(Keyword, LessEqualToken.TokenText);
        public static readonly string KModulo = Format(Keyword, ModuloToken.TokenText);
        public static readonly string KNot = Format(Keyword, NotToken.TokenText);
        public static readonly string KOr = Format(Keyword, OrToken.TokenText);
        public static readonly string KPlus = $@"\{PlusToken.TokenText}";
        public static readonly string KRightParenthesis = $@"\{RightParenthesisToken.TokenText}";
        public static readonly string KIs = Format(Keyword, IsToken.TokenText);
        public static readonly string KNull = Format(Keyword, NullToken.TokenText);
        public static readonly string KStar = Format(Keyword, $@"\{StarToken.TokenText}");
        public static readonly string KWhere = Format(Keyword, WhereToken.TokenText);
        public static readonly string KWhiteSpace = @"[\s]{1,}";
        public static readonly string KWordSingleQuoted = @"'([^'\\]|\\.)*'";
        public static readonly string KEmptyString = "''";
        public static readonly string KEqual = Format(Keyword, EqualityToken.TokenText);
        public static readonly string KSelect = Format(Keyword, SelectToken.TokenText);
        public static readonly string KFrom = Format(Keyword, FromToken.TokenText);
        public static readonly string KColumn = @"\[[^\]]+\]|(\w+)|(\*)";
        public static readonly string KHFrom = @"#[\w*?_]{1,}";
        public static readonly string KLike = Format(Keyword, LikeToken.TokenText);
        public static readonly string KNotLike = @"(?<=[\s]{1,}|^)not[\s]{1,}like(?=[\s]{1,}|$)";
        public static readonly string KRLike = Format(Keyword, RLikeToken.TokenText);
        public static readonly string KRNotLike = @"(?<=[\s]{1,}|^)not[\s]{1,}rlike(?=[\s]{1,}|$)";
        public static readonly string KAs = Format(Keyword, AsToken.TokenText);
        public static readonly string KUnion = Format(Keyword, SetOperatorToken.UnionOperatorText);
        public static readonly string KDot = "\\.";
        public static readonly string KIntersect = Format(Keyword, SetOperatorToken.IntersectOperatorText);
        public static readonly string KExcept = Format(Keyword, SetOperatorToken.ExceptOperatorText);
        public static readonly string KUnionAll = @"(?<=[\s]{1,}|^)union[\s]{1,}all(?=[\s]{1,}|$)";
        public static readonly string KGroupBy = @"(?<=[\s]{1,}|^)group[\s]{1,}by(?=[\s]{1,}|$)";
        public static readonly string KHaving = Format(Keyword, HavingToken.TokenText);
        public static readonly string KContains = Format(Keyword, ContainsToken.TokenText);
        public static readonly string KFieldLink = "::[1-9]{1,}";
        public static readonly string KNumericArrayAccess = "([\\w*?_]{1,})\\[([-]?[0-9]{1,})\\]";
        public static readonly string KKeyObjectAccessVariable = "([\\w*?_]{1,})\\[([a-zA-Z0-9]{1,})\\]";
        public static readonly string KKeyObjectAccessConst = "([\\w*?_]{1,})\\[('[a-zA-Z0-9]{1,}')\\]";
        public static readonly string KDecimalWithSuffix = @"[\-]?([0-9]+)[dD]{1}";
        public static readonly string KDecimalWithDot = @"[\-]?([0-9]+\.[0-9]{1,})";
        public static readonly string KDecimalWithDotAndSuffix = @"[\-]?([0-9]+\.[0-9]{1,})[dD]{1}";
        public static readonly string KSignedInteger = @"-?\d+(?:I|i|L|l|S|s|B|b)?";
        public static readonly string KUnsignedInteger = "[0-9]+(?:UI|ui|UL|ul|US|us|UB|ub){1}";
        public static readonly string KDecimalOrInteger = $"({KDecimalWithDotAndSuffix}|{KDecimalWithDot}|{KDecimalWithSuffix}|{KUnsignedInteger}|{KSignedInteger})";
        public static readonly string KComment = "--[^\\r\\n]*|/\\*[\\s\\S]*?\\*/";

        public static readonly string KMethodAccess =
            "([a-zA-Z1-9_]{1,})(?=\\.[a-zA-Z_-]{1,}[a-zA-Z1-9_-]{1,}[\\d]*[\\(])";

        public static readonly string KSkip = Format(Keyword, SkipToken.TokenText);
        public static readonly string KTake = Format(Keyword, TakeToken.TokenText);
        public static readonly string KWith = Format(Keyword, WithToken.TokenText);
        public static readonly string KInnerJoin = @"(?<=[\s]{1,}|^)inner[\s]{1,}join(?=[\s]{1,}|$)";

        public static readonly string KOuterJoin =
            @"(?<=[\s]{1,}|^)(left|right)(?:\s+outer)?[\s]{1,}join(?=[\s]{1,}|$)";

        public static readonly string KCrossApply =
            @"(?<=[\s]{1,}|^)cross[\s]{1,}apply(?=[\s]{1,}|$)";

        public static readonly string KOuterApply =
            @"(?<=[\s]{1,}|^)outer[\s]{1,}apply(?=[\s]{1,}|$)";

        public static readonly string KOn = Format(Keyword, OnToken.TokenText);
        public static readonly string KOrderBy = @"(?<=[\s]{1,}|^)order[\s]{1,}by(?=[\s]{1,}|$)";
        public static readonly string KAsc = Format(Keyword, AscToken.TokenText);
        public static readonly string KDesc = Format(Keyword, DescToken.TokenText);
        public static readonly string KTrue = Format(Keyword, TrueToken.TokenText);
        public static readonly string KFalse = Format(Keyword, FalseToken.TokenText);
        public static readonly string KIn = Format(Keyword, InToken.TokenText);
        public static readonly string KNotIn = @"(?<=[\s]{1,}|^)not[\s]{1,}in(?=[\s]{1,}|$)";

        public static readonly string KTable = Format(Keyword, TableToken.TokenText);
        public static readonly string KLeftBracket = "\\{";
        public static readonly string KRightBracket = "\\}";
        public static readonly string KSemicolon = "\\;";
        public static readonly string KCouple = Format(Keyword, CoupleToken.TokenText);
        public static readonly string KCase = Format(Keyword, CaseToken.TokenText);
        public static readonly string KWhen = Format(Keyword, WhenToken.TokenText);
        public static readonly string KThen = Format(Keyword, ThenToken.TokenText);
        public static readonly string KElse = Format(Keyword, ElseToken.TokenText);
        public static readonly string KEnd = Format(Keyword, EndToken.TokenText);
        public static readonly string KAliasedStar = @"\b[a-zA-Z_]\w*\.\*";

        private static string Format(string keyword, string arg)
        {
            return keyword.Replace("{keyword}", arg);
        }
    }

    /// <summary>
    ///     The token definitions set.
    /// </summary>
    private static class DefinitionSets
    {
        /// <summary>
        ///     All supported by language keyword.
        /// </summary>
        public static TokenDefinition[] General =>
        [
            new(TokenRegexDefinition.KComment),
            new(TokenRegexDefinition.KDecimalOrInteger),
            new(TokenRegexDefinition.KDesc),
            new(TokenRegexDefinition.KAsc),
            new(TokenRegexDefinition.KLike, RegexOptions.IgnoreCase),
            new(TokenRegexDefinition.KNotLike, RegexOptions.IgnoreCase),
            new(TokenRegexDefinition.KRLike, RegexOptions.IgnoreCase),
            new(TokenRegexDefinition.KRNotLike, RegexOptions.IgnoreCase),
            new(TokenRegexDefinition.KNotIn, RegexOptions.IgnoreCase),
            new(TokenRegexDefinition.KAs, RegexOptions.IgnoreCase),
            new(TokenRegexDefinition.Function),
            new(TokenRegexDefinition.KAnd, RegexOptions.IgnoreCase),
            new(TokenRegexDefinition.KComma),
            new(TokenRegexDefinition.KDiff),
            new(TokenRegexDefinition.KFSlashToken),
            new(TokenRegexDefinition.KGreater),
            new(TokenRegexDefinition.KGreaterEqual),
            new(TokenRegexDefinition.KHyphen),
            new(TokenRegexDefinition.KLeftParenthesis),
            new(TokenRegexDefinition.KRightParenthesis),
            new(TokenRegexDefinition.KLess),
            new(TokenRegexDefinition.KLessEqual),
            new(TokenRegexDefinition.KEqual),
            new(TokenRegexDefinition.KModulo),
            new(TokenRegexDefinition.KNot),
            new(TokenRegexDefinition.KOr),
            new(TokenRegexDefinition.KPlus),
            new(TokenRegexDefinition.KAliasedStar),
            new(TokenRegexDefinition.KStar),
            new(TokenRegexDefinition.KIs),
            new(TokenRegexDefinition.KIn), 
            new(TokenRegexDefinition.KNull),
            new(TokenRegexDefinition.KWith, RegexOptions.IgnoreCase),
            new(TokenRegexDefinition.KWhere, RegexOptions.IgnoreCase),
            new(TokenRegexDefinition.KContains, RegexOptions.IgnoreCase),
            new(TokenRegexDefinition.KWhiteSpace),
            new(TokenRegexDefinition.KUnionAll, RegexOptions.IgnoreCase),
            new(TokenRegexDefinition.KEmptyString),
            new(TokenRegexDefinition.KWordSingleQuoted, RegexOptions.IgnoreCase),
            new(TokenRegexDefinition.KSelect, RegexOptions.IgnoreCase),
            new(TokenRegexDefinition.KFrom, RegexOptions.IgnoreCase),
            new(TokenRegexDefinition.KUnion, RegexOptions.IgnoreCase),
            new(TokenRegexDefinition.KExcept, RegexOptions.IgnoreCase),
            new(TokenRegexDefinition.KIntersect, RegexOptions.IgnoreCase),
            new(TokenRegexDefinition.KGroupBy, RegexOptions.IgnoreCase),
            new(TokenRegexDefinition.KHaving, RegexOptions.IgnoreCase),
            new(TokenRegexDefinition.KSkip, RegexOptions.IgnoreCase),
            new(TokenRegexDefinition.KTake, RegexOptions.IgnoreCase),
            new(TokenRegexDefinition.KNumericArrayAccess),
            new(TokenRegexDefinition.KKeyObjectAccessConst),
            new(TokenRegexDefinition.KKeyObjectAccessVariable),
            new(TokenRegexDefinition.KMethodAccess),
            new(TokenRegexDefinition.KInnerJoin, RegexOptions.IgnoreCase),
            new(TokenRegexDefinition.KOuterJoin, RegexOptions.IgnoreCase),
            new(TokenRegexDefinition.KCrossApply, RegexOptions.IgnoreCase),
            new(TokenRegexDefinition.KOuterApply, RegexOptions.IgnoreCase),
            new(TokenRegexDefinition.KOrderBy, RegexOptions.IgnoreCase),
            new(TokenRegexDefinition.KTrue, RegexOptions.IgnoreCase),
            new(TokenRegexDefinition.KFalse, RegexOptions.IgnoreCase),
            new(TokenRegexDefinition.KColumn),
            new(TokenRegexDefinition.KHFrom),
            new(TokenRegexDefinition.KDot),
            new(TokenRegexDefinition.KOn),
            new(TokenRegexDefinition.KTable),
            new(TokenRegexDefinition.KLeftBracket),
            new(TokenRegexDefinition.KRightBracket),
            new(TokenRegexDefinition.KSemicolon),
            new(TokenRegexDefinition.KCouple),
            new(TokenRegexDefinition.KCase),
            new(TokenRegexDefinition.KWhen),
            new(TokenRegexDefinition.KThen),
            new(TokenRegexDefinition.KElse),
            new(TokenRegexDefinition.KEnd),
            new(TokenRegexDefinition.KFieldLink)
        ];
    }

    #region Overrides of LexerBase<Token>

    /// <summary>
    ///     Gets the next token from tokens stream.
    /// </summary>
    /// <returns>The token.</returns>
    public override Token Next()
    {
        var token = base.Next();
        while (ShouldSkipToken(token))
            token = base.Next();
            
        _alreadyResolvedTokens.Add(token);
            
        return token;
    }

    /// <summary>
    ///     Gets the next token from tokens stream that matches the regex.
    /// </summary>
    /// <param name="regex">The regex.</param>
    /// <param name="getToken">Gets the arbitrary token.</param>
    /// <returns>The token.</returns>
    public override Token NextOf(Regex regex, Func<string, Token> getToken)
    {
        var token = base.NextOf(regex, getToken);
        while (ShouldSkipToken(token))
            token = base.NextOf(regex, getToken);
            
        _alreadyResolvedTokens.Add(token);
            
        return token;
    }

    /// <summary>
    ///     Gets EndOfFile token.
    /// </summary>
    /// <returns>End of file token.</returns>
    protected override Token GetEndOfFileToken()
    {
        return new EndOfFileToken(new TextSpan(Input.Length, 0));
    }

    /// <summary>
    ///     Gets the token.
    /// </summary>
    /// <param name="matchedDefinition">The definition of token type that fits requirements.</param>
    /// <param name="match">The match.</param>
    /// <returns>The token.</returns>
    protected override Token GetToken(TokenDefinition matchedDefinition, Match match)
    {
        var regex = matchedDefinition.Regex.ToString();
        var tokenText = match.Value;
        var token = GetTokenCandidate(tokenText, regex);

        switch (token)
        {
            case TokenType.Desc:
                return new DescToken(new TextSpan(Position, tokenText.Length));
            case TokenType.Asc:
                return new AscToken(new TextSpan(Position, tokenText.Length));
            case TokenType.And:
                return new AndToken(new TextSpan(Position, tokenText.Length));
            case TokenType.Comma:
                return new CommaToken(new TextSpan(Position, tokenText.Length));
            case TokenType.Diff:
                return new DiffToken(new TextSpan(Position, tokenText.Length));
            case TokenType.Equality:
                return new EqualityToken(new TextSpan(Position, tokenText.Length));
            case TokenType.FSlash:
                return new FSlashToken(new TextSpan(Position, tokenText.Length));
            case TokenType.Function:
                return new FunctionToken(tokenText, new TextSpan(Position, tokenText.Length));
            case TokenType.Greater:
                return new GreaterToken(new TextSpan(Position, tokenText.Length));
            case TokenType.GreaterEqual:
                return new GreaterEqualToken(new TextSpan(Position, tokenText.Length));
            case TokenType.Hyphen:
                return new HyphenToken(new TextSpan(Position, tokenText.Length));
            case TokenType.LeftParenthesis:
                return new LeftParenthesisToken(new TextSpan(Position, tokenText.Length));
            case TokenType.Less:
                return new LessToken(new TextSpan(Position, tokenText.Length));
            case TokenType.LessEqual:
                return new LessEqualToken(new TextSpan(Position, tokenText.Length));
            case TokenType.Mod:
                return new ModuloToken(new TextSpan(Position, tokenText.Length));
            case TokenType.Not:
                return new NotToken(new TextSpan(Position, tokenText.Length));
            case TokenType.Decimal:
                return new DecimalToken(tokenText.TrimEnd('d'), new TextSpan(Position, tokenText.Length));
            case TokenType.Integer:
                var abbreviation = GetAbbreviation(tokenText);
                var unAbbreviatedValue = abbreviation.Length > 0 ? tokenText.Replace(abbreviation, string.Empty) : tokenText;
                return new IntegerToken(unAbbreviatedValue, new TextSpan(Position, tokenText.Length), abbreviation);
            case TokenType.Or:
                return new OrToken(new TextSpan(Position, tokenText.Length));
            case TokenType.Plus:
                return new PlusToken(new TextSpan(Position, tokenText.Length));
            case TokenType.RightParenthesis:
                return new RightParenthesisToken(new TextSpan(Position, tokenText.Length));
            case TokenType.Star:
                return new StarToken(new TextSpan(Position, tokenText.Length));
            case TokenType.AliasedStar:
                return new AliasedStarToken(tokenText, new TextSpan(Position, tokenText.Length));
            case TokenType.Where:
                return new WhereToken(new TextSpan(Position, tokenText.Length));
            case TokenType.WhiteSpace:
                return new WhiteSpaceToken(new TextSpan(Position, tokenText.Length));
            case TokenType.From:
                return new FromToken(new TextSpan(Position, tokenText.Length));
            case TokenType.Select:
                return new SelectToken(new TextSpan(Position, tokenText.Length));
            case TokenType.Identifier:
                return new ColumnToken(tokenText, new TextSpan(Position, tokenText.Length));
            case TokenType.Property:
                return new AccessPropertyToken(tokenText, new TextSpan(Position, tokenText.Length));
            case TokenType.Like:
                return new LikeToken(new TextSpan(Position, tokenText.Length));
            case TokenType.NotLike:
                return new NotLikeToken(new TextSpan(Position, tokenText.Length));
            case TokenType.RLike:
                return new RLikeToken(new TextSpan(Position, tokenText.Length));
            case TokenType.NotRLike:
                return new NotRLikeToken(new TextSpan(Position, tokenText.Length));
            case TokenType.As:
                return new AsToken(new TextSpan(Position, tokenText.Length));
            case TokenType.Except:
                return new ExceptToken(new TextSpan(Position, tokenText.Length));
            case TokenType.Union:
                return new UnionToken(new TextSpan(Position, tokenText.Length));
            case TokenType.Intersect:
                return new IntersectToken(new TextSpan(Position, tokenText.Length));
            case TokenType.UnionAll:
                return new UnionAllToken(new TextSpan(Position, tokenText.Length));
            case TokenType.Dot:
                return new DotToken(new TextSpan(Position, tokenText.Length));
            case TokenType.GroupBy:
                return new GroupByToken(new TextSpan(Position, tokenText.Length));
            case TokenType.Having:
                return new HavingToken(new TextSpan(Position, tokenText.Length));
            case TokenType.NumericAccess:
                match = matchedDefinition.Regex.Match(tokenText);
                return new NumericAccessToken(match.Groups[1].Value, match.Groups[2].Value,
                    new TextSpan(Position, tokenText.Length));
            case TokenType.KeyAccess:
                match = matchedDefinition.Regex.Match(tokenText);
                return new KeyAccessToken(match.Groups[1].Value, match.Groups[2].Value,
                    new TextSpan(Position, tokenText.Length));
            case TokenType.Contains:
                return new ContainsToken(new TextSpan(Position, tokenText.Length));
            case TokenType.Skip:
                return new SkipToken(tokenText, new TextSpan(Position, tokenText.Length));
            case TokenType.Take:
                return new TakeToken(tokenText, new TextSpan(Position, tokenText.Length));
            case TokenType.With:
                return new WithToken(new TextSpan(Position, tokenText.Length));
            case TokenType.On:
                return new OnToken(new TextSpan(Position, tokenText.Length));
            case TokenType.InnerJoin:
                return new InnerJoinToken(new TextSpan(Position, tokenText.Length));
            case TokenType.OuterJoin:
                var type = match.Groups[1].Value.ToLowerInvariant() == "left"
                    ? OuterJoinType.Left
                    : OuterJoinType.Right;

                return new OuterJoinToken(type, new TextSpan(Position, tokenText.Length));
            case TokenType.CrossApply:
                return new CrossApplyToken(new TextSpan(Position, tokenText.Length));
            case TokenType.OuterApply:
                return new OuterApplyToken(new TextSpan(Position, tokenText.Length));
            case TokenType.MethodAccess:
                return new MethodAccessToken(match.Groups[1].Value,
                    new TextSpan(Position, match.Groups[1].Value.Length));
            case TokenType.Is:
                return new IsToken(new TextSpan(Position, tokenText.Length));
            case TokenType.Null:
                return new NullToken(new TextSpan(Position, tokenText.Length));
            case TokenType.OrderBy:
                return new OrderByToken(new TextSpan(Position, tokenText.Length));
            case TokenType.True:
                return new TrueToken(new TextSpan(Position, tokenText.Length));
            case TokenType.False:
                return new FalseToken(new TextSpan(Position, tokenText.Length));
            case TokenType.In:
                return new InToken(new TextSpan(Position, tokenText.Length));
            case TokenType.NotIn:
                return new NotInToken(new TextSpan(Position, tokenText.Length));
            case TokenType.Table:
                return new TableToken(new TextSpan(Position, tokenText.Length));
            case TokenType.LBracket:
                return new LBracketToken(new TextSpan(Position, tokenText.Length));
            case TokenType.RBracket:
                return new RBracketToken(new TextSpan(Position, tokenText.Length));
            case TokenType.Semicolon:
                return new SemicolonToken(new TextSpan(Position, tokenText.Length));
            case TokenType.Couple:
                return new CoupleToken(new TextSpan(Position, tokenText.Length));
            case TokenType.Case:
                return new CaseToken(new TextSpan(Position, tokenText.Length));
            case TokenType.When:
                return new WhenToken(new TextSpan(Position, tokenText.Length));
            case TokenType.Then:
                return new ThenToken(new TextSpan(Position, tokenText.Length));
            case TokenType.Else:
                return new ElseToken(new TextSpan(Position, tokenText.Length));
            case TokenType.End:
                return new EndToken(new TextSpan(Position, tokenText.Length));
            case TokenType.FieldLink:
                return new FieldLinkToken(tokenText, new TextSpan(Position, tokenText.Length));
            case TokenType.Comment:
                return new CommentToken(tokenText, new TextSpan(Position, tokenText.Length));
        }

        if (regex != TokenRegexDefinition.KWordSingleQuoted)
            return regex == TokenRegexDefinition.KEmptyString
                ? new WordToken(string.Empty, new TextSpan(Position + 1, 0))
                : new WordToken(tokenText, new TextSpan(Position, tokenText.Length));
        
        var subValue = match.Groups[0].Value[1..^1];
        var value = subValue.Unescape();
        return new WordToken(
            value,
            new TextSpan(Position + 1, match.Groups[0].Value.Length)
        );
    }

    private bool ShouldSkipToken(Token token)
    {
        return (_skipWhiteSpaces && token.TokenType == TokenType.WhiteSpace) || token.TokenType == TokenType.Comment;
    }

    private static string GetAbbreviation(string tokenText)
    {
        if (tokenText.Length == 1)
        {
            return string.Empty;
        }
            
        var position = 1;
        while (position < tokenText.Length && char.IsDigit(tokenText[position]))
        {
            position++;
        }
            
        return tokenText[position..];
    }

    #endregion
}