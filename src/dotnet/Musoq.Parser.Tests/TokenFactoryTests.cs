using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Lexing;
using Musoq.Parser.Tokens;

namespace Musoq.Parser.Tests;

/// <summary>
///     Tests for TokenFactory to improve branch coverage by testing all token types.
/// </summary>
[TestClass]
public class TokenFactoryTests
{
    #region Simple Token Factory Tests

    [TestMethod]
    public void Create_DescToken_ReturnsCorrectType()
    {
        var token = TokenFactory.Create(TokenType.Desc, 0, "desc");
        Assert.IsNotNull(token);
        Assert.IsInstanceOfType<DescToken>(token);
    }

    [TestMethod]
    public void Create_AscToken_ReturnsCorrectType()
    {
        var token = TokenFactory.Create(TokenType.Asc, 0, "asc");
        Assert.IsNotNull(token);
        Assert.IsInstanceOfType<AscToken>(token);
    }

    [TestMethod]
    public void Create_AndToken_ReturnsCorrectType()
    {
        var token = TokenFactory.Create(TokenType.And, 0, "and");
        Assert.IsNotNull(token);
        Assert.IsInstanceOfType<AndToken>(token);
    }

    [TestMethod]
    public void Create_CommaToken_ReturnsCorrectType()
    {
        var token = TokenFactory.Create(TokenType.Comma, 0, ",");
        Assert.IsNotNull(token);
        Assert.IsInstanceOfType<CommaToken>(token);
    }

    [TestMethod]
    public void Create_DiffToken_ReturnsCorrectType()
    {
        var token = TokenFactory.Create(TokenType.Diff, 0, "<>");
        Assert.IsNotNull(token);
        Assert.IsInstanceOfType<DiffToken>(token);
    }

    [TestMethod]
    public void Create_EqualityToken_ReturnsCorrectType()
    {
        var token = TokenFactory.Create(TokenType.Equality, 0, "=");
        Assert.IsNotNull(token);
        Assert.IsInstanceOfType<EqualityToken>(token);
    }

    [TestMethod]
    public void Create_FSlashToken_ReturnsCorrectType()
    {
        var token = TokenFactory.Create(TokenType.FSlash, 0, "/");
        Assert.IsNotNull(token);
        Assert.IsInstanceOfType<FSlashToken>(token);
    }

    [TestMethod]
    public void Create_GreaterToken_ReturnsCorrectType()
    {
        var token = TokenFactory.Create(TokenType.Greater, 0, ">");
        Assert.IsNotNull(token);
        Assert.IsInstanceOfType<GreaterToken>(token);
    }

    [TestMethod]
    public void Create_GreaterEqualToken_ReturnsCorrectType()
    {
        var token = TokenFactory.Create(TokenType.GreaterEqual, 0, ">=");
        Assert.IsNotNull(token);
        Assert.IsInstanceOfType<GreaterEqualToken>(token);
    }

    [TestMethod]
    public void Create_HyphenToken_ReturnsCorrectType()
    {
        var token = TokenFactory.Create(TokenType.Hyphen, 0, "-");
        Assert.IsNotNull(token);
        Assert.IsInstanceOfType<HyphenToken>(token);
    }

    [TestMethod]
    public void Create_LeftParenthesisToken_ReturnsCorrectType()
    {
        var token = TokenFactory.Create(TokenType.LeftParenthesis, 0, "(");
        Assert.IsNotNull(token);
        Assert.IsInstanceOfType<LeftParenthesisToken>(token);
    }

    [TestMethod]
    public void Create_LessToken_ReturnsCorrectType()
    {
        var token = TokenFactory.Create(TokenType.Less, 0, "<");
        Assert.IsNotNull(token);
        Assert.IsInstanceOfType<LessToken>(token);
    }

    [TestMethod]
    public void Create_LessEqualToken_ReturnsCorrectType()
    {
        var token = TokenFactory.Create(TokenType.LessEqual, 0, "<=");
        Assert.IsNotNull(token);
        Assert.IsInstanceOfType<LessEqualToken>(token);
    }

    [TestMethod]
    public void Create_ModToken_ReturnsCorrectType()
    {
        var token = TokenFactory.Create(TokenType.Mod, 0, "%");
        Assert.IsNotNull(token);
        Assert.IsInstanceOfType<ModuloToken>(token);
    }

    [TestMethod]
    public void Create_NotToken_ReturnsCorrectType()
    {
        var token = TokenFactory.Create(TokenType.Not, 0, "not");
        Assert.IsNotNull(token);
        Assert.IsInstanceOfType<NotToken>(token);
    }

    [TestMethod]
    public void Create_OrToken_ReturnsCorrectType()
    {
        var token = TokenFactory.Create(TokenType.Or, 0, "or");
        Assert.IsNotNull(token);
        Assert.IsInstanceOfType<OrToken>(token);
    }

    [TestMethod]
    public void Create_PlusToken_ReturnsCorrectType()
    {
        var token = TokenFactory.Create(TokenType.Plus, 0, "+");
        Assert.IsNotNull(token);
        Assert.IsInstanceOfType<PlusToken>(token);
    }

    [TestMethod]
    public void Create_RightParenthesisToken_ReturnsCorrectType()
    {
        var token = TokenFactory.Create(TokenType.RightParenthesis, 0, ")");
        Assert.IsNotNull(token);
        Assert.IsInstanceOfType<RightParenthesisToken>(token);
    }

    [TestMethod]
    public void Create_StarToken_ReturnsCorrectType()
    {
        var token = TokenFactory.Create(TokenType.Star, 0, "*");
        Assert.IsNotNull(token);
        Assert.IsInstanceOfType<StarToken>(token);
    }

    [TestMethod]
    public void Create_WhereToken_ReturnsCorrectType()
    {
        var token = TokenFactory.Create(TokenType.Where, 0, "where");
        Assert.IsNotNull(token);
        Assert.IsInstanceOfType<WhereToken>(token);
    }

    [TestMethod]
    public void Create_WhiteSpaceToken_ReturnsCorrectType()
    {
        var token = TokenFactory.Create(TokenType.WhiteSpace, 0, " ");
        Assert.IsNotNull(token);
        Assert.IsInstanceOfType<WhiteSpaceToken>(token);
    }

    [TestMethod]
    public void Create_FromToken_ReturnsCorrectType()
    {
        var token = TokenFactory.Create(TokenType.From, 0, "from");
        Assert.IsNotNull(token);
        Assert.IsInstanceOfType<FromToken>(token);
    }

    [TestMethod]
    public void Create_SelectToken_ReturnsCorrectType()
    {
        var token = TokenFactory.Create(TokenType.Select, 0, "select");
        Assert.IsNotNull(token);
        Assert.IsInstanceOfType<SelectToken>(token);
    }

    [TestMethod]
    public void Create_LikeToken_ReturnsCorrectType()
    {
        var token = TokenFactory.Create(TokenType.Like, 0, "like");
        Assert.IsNotNull(token);
        Assert.IsInstanceOfType<LikeToken>(token);
    }

    [TestMethod]
    public void Create_NotLikeToken_ReturnsCorrectType()
    {
        var token = TokenFactory.Create(TokenType.NotLike, 0, "not like");
        Assert.IsNotNull(token);
        Assert.IsInstanceOfType<NotLikeToken>(token);
    }

    [TestMethod]
    public void Create_RLikeToken_ReturnsCorrectType()
    {
        var token = TokenFactory.Create(TokenType.RLike, 0, "rlike");
        Assert.IsNotNull(token);
        Assert.IsInstanceOfType<RLikeToken>(token);
    }

    [TestMethod]
    public void Create_NotRLikeToken_ReturnsCorrectType()
    {
        var token = TokenFactory.Create(TokenType.NotRLike, 0, "not rlike");
        Assert.IsNotNull(token);
        Assert.IsInstanceOfType<NotRLikeToken>(token);
    }

    [TestMethod]
    public void Create_AsToken_ReturnsCorrectType()
    {
        var token = TokenFactory.Create(TokenType.As, 0, "as");
        Assert.IsNotNull(token);
        Assert.IsInstanceOfType<AsToken>(token);
    }

    [TestMethod]
    public void Create_ExceptToken_ReturnsCorrectType()
    {
        var token = TokenFactory.Create(TokenType.Except, 0, "except");
        Assert.IsNotNull(token);
        Assert.IsInstanceOfType<ExceptToken>(token);
    }

    [TestMethod]
    public void Create_UnionToken_ReturnsCorrectType()
    {
        var token = TokenFactory.Create(TokenType.Union, 0, "union");
        Assert.IsNotNull(token);
        Assert.IsInstanceOfType<UnionToken>(token);
    }

    [TestMethod]
    public void Create_IntersectToken_ReturnsCorrectType()
    {
        var token = TokenFactory.Create(TokenType.Intersect, 0, "intersect");
        Assert.IsNotNull(token);
        Assert.IsInstanceOfType<IntersectToken>(token);
    }

    [TestMethod]
    public void Create_UnionAllToken_ReturnsCorrectType()
    {
        var token = TokenFactory.Create(TokenType.UnionAll, 0, "union all");
        Assert.IsNotNull(token);
        Assert.IsInstanceOfType<UnionAllToken>(token);
    }

    [TestMethod]
    public void Create_DotToken_ReturnsCorrectType()
    {
        var token = TokenFactory.Create(TokenType.Dot, 0, ".");
        Assert.IsNotNull(token);
        Assert.IsInstanceOfType<DotToken>(token);
    }

    [TestMethod]
    public void Create_GroupByToken_ReturnsCorrectType()
    {
        var token = TokenFactory.Create(TokenType.GroupBy, 0, "group by");
        Assert.IsNotNull(token);
        Assert.IsInstanceOfType<GroupByToken>(token);
    }

    [TestMethod]
    public void Create_HavingToken_ReturnsCorrectType()
    {
        var token = TokenFactory.Create(TokenType.Having, 0, "having");
        Assert.IsNotNull(token);
        Assert.IsInstanceOfType<HavingToken>(token);
    }

    [TestMethod]
    public void Create_ContainsToken_ReturnsCorrectType()
    {
        var token = TokenFactory.Create(TokenType.Contains, 0, "contains");
        Assert.IsNotNull(token);
        Assert.IsInstanceOfType<ContainsToken>(token);
    }

    [TestMethod]
    public void Create_WithToken_ReturnsCorrectType()
    {
        var token = TokenFactory.Create(TokenType.With, 0, "with");
        Assert.IsNotNull(token);
        Assert.IsInstanceOfType<WithToken>(token);
    }

    [TestMethod]
    public void Create_OnToken_ReturnsCorrectType()
    {
        var token = TokenFactory.Create(TokenType.On, 0, "on");
        Assert.IsNotNull(token);
        Assert.IsInstanceOfType<OnToken>(token);
    }

    [TestMethod]
    public void Create_InnerJoinToken_ReturnsCorrectType()
    {
        var token = TokenFactory.Create(TokenType.InnerJoin, 0, "inner join");
        Assert.IsNotNull(token);
        Assert.IsInstanceOfType<InnerJoinToken>(token);
    }

    [TestMethod]
    public void Create_CrossApplyToken_ReturnsCorrectType()
    {
        var token = TokenFactory.Create(TokenType.CrossApply, 0, "cross apply");
        Assert.IsNotNull(token);
        Assert.IsInstanceOfType<CrossApplyToken>(token);
    }

    [TestMethod]
    public void Create_OuterApplyToken_ReturnsCorrectType()
    {
        var token = TokenFactory.Create(TokenType.OuterApply, 0, "outer apply");
        Assert.IsNotNull(token);
        Assert.IsInstanceOfType<OuterApplyToken>(token);
    }

    [TestMethod]
    public void Create_IsToken_ReturnsCorrectType()
    {
        var token = TokenFactory.Create(TokenType.Is, 0, "is");
        Assert.IsNotNull(token);
        Assert.IsInstanceOfType<IsToken>(token);
    }

    [TestMethod]
    public void Create_FunctionsToken_ReturnsCorrectType()
    {
        var token = TokenFactory.Create(TokenType.Functions, 0, "functions");
        Assert.IsNotNull(token);
        Assert.IsInstanceOfType<FunctionsToken>(token);
    }

    [TestMethod]
    public void Create_NullToken_ReturnsCorrectType()
    {
        var token = TokenFactory.Create(TokenType.Null, 0, "null");
        Assert.IsNotNull(token);
        Assert.IsInstanceOfType<NullToken>(token);
    }

    [TestMethod]
    public void Create_OrderByToken_ReturnsCorrectType()
    {
        var token = TokenFactory.Create(TokenType.OrderBy, 0, "order by");
        Assert.IsNotNull(token);
        Assert.IsInstanceOfType<OrderByToken>(token);
    }

    [TestMethod]
    public void Create_TrueToken_ReturnsCorrectType()
    {
        var token = TokenFactory.Create(TokenType.True, 0, "true");
        Assert.IsNotNull(token);
        Assert.IsInstanceOfType<TrueToken>(token);
    }

    [TestMethod]
    public void Create_FalseToken_ReturnsCorrectType()
    {
        var token = TokenFactory.Create(TokenType.False, 0, "false");
        Assert.IsNotNull(token);
        Assert.IsInstanceOfType<FalseToken>(token);
    }

    [TestMethod]
    public void Create_InToken_ReturnsCorrectType()
    {
        var token = TokenFactory.Create(TokenType.In, 0, "in");
        Assert.IsNotNull(token);
        Assert.IsInstanceOfType<InToken>(token);
    }

    [TestMethod]
    public void Create_NotInToken_ReturnsCorrectType()
    {
        var token = TokenFactory.Create(TokenType.NotIn, 0, "not in");
        Assert.IsNotNull(token);
        Assert.IsInstanceOfType<NotInToken>(token);
    }

    [TestMethod]
    public void Create_ColonToken_ReturnsCorrectType()
    {
        var token = TokenFactory.Create(TokenType.Colon, 0, ":");
        Assert.IsNotNull(token);
        Assert.IsInstanceOfType<ColonToken>(token);
    }

    [TestMethod]
    public void Create_TableToken_ReturnsCorrectType()
    {
        var token = TokenFactory.Create(TokenType.Table, 0, "table");
        Assert.IsNotNull(token);
        Assert.IsInstanceOfType<TableToken>(token);
    }

    [TestMethod]
    public void Create_LBracketToken_ReturnsCorrectType()
    {
        var token = TokenFactory.Create(TokenType.LBracket, 0, "{");
        Assert.IsNotNull(token);
        Assert.IsInstanceOfType<LBracketToken>(token);
    }

    [TestMethod]
    public void Create_RBracketToken_ReturnsCorrectType()
    {
        var token = TokenFactory.Create(TokenType.RBracket, 0, "}");
        Assert.IsNotNull(token);
        Assert.IsInstanceOfType<RBracketToken>(token);
    }

    [TestMethod]
    public void Create_LeftSquareBracketToken_ReturnsCorrectType()
    {
        var token = TokenFactory.Create(TokenType.LeftSquareBracket, 0, "[");
        Assert.IsNotNull(token);
        Assert.IsInstanceOfType<LeftSquareBracketToken>(token);
    }

    [TestMethod]
    public void Create_RightSquareBracketToken_ReturnsCorrectType()
    {
        var token = TokenFactory.Create(TokenType.RightSquareBracket, 0, "]");
        Assert.IsNotNull(token);
        Assert.IsInstanceOfType<RightSquareBracketToken>(token);
    }

    [TestMethod]
    public void Create_SemicolonToken_ReturnsCorrectType()
    {
        var token = TokenFactory.Create(TokenType.Semicolon, 0, ";");
        Assert.IsNotNull(token);
        Assert.IsInstanceOfType<SemicolonToken>(token);
    }

    [TestMethod]
    public void Create_CoupleToken_ReturnsCorrectType()
    {
        var token = TokenFactory.Create(TokenType.Couple, 0, "couple");
        Assert.IsNotNull(token);
        Assert.IsInstanceOfType<CoupleToken>(token);
    }

    [TestMethod]
    public void Create_CaseToken_ReturnsCorrectType()
    {
        var token = TokenFactory.Create(TokenType.Case, 0, "case");
        Assert.IsNotNull(token);
        Assert.IsInstanceOfType<CaseToken>(token);
    }

    [TestMethod]
    public void Create_WhenToken_ReturnsCorrectType()
    {
        var token = TokenFactory.Create(TokenType.When, 0, "when");
        Assert.IsNotNull(token);
        Assert.IsInstanceOfType<WhenToken>(token);
    }

    [TestMethod]
    public void Create_ThenToken_ReturnsCorrectType()
    {
        var token = TokenFactory.Create(TokenType.Then, 0, "then");
        Assert.IsNotNull(token);
        Assert.IsInstanceOfType<ThenToken>(token);
    }

    [TestMethod]
    public void Create_ElseToken_ReturnsCorrectType()
    {
        var token = TokenFactory.Create(TokenType.Else, 0, "else");
        Assert.IsNotNull(token);
        Assert.IsInstanceOfType<ElseToken>(token);
    }

    [TestMethod]
    public void Create_EndToken_ReturnsCorrectType()
    {
        var token = TokenFactory.Create(TokenType.End, 0, "end");
        Assert.IsNotNull(token);
        Assert.IsInstanceOfType<EndToken>(token);
    }

    [TestMethod]
    public void Create_DistinctToken_ReturnsCorrectType()
    {
        var token = TokenFactory.Create(TokenType.Distinct, 0, "distinct");
        Assert.IsNotNull(token);
        Assert.IsInstanceOfType<DistinctToken>(token);
    }

    [TestMethod]
    public void Create_AmpersandToken_ReturnsCorrectType()
    {
        var token = TokenFactory.Create(TokenType.Ampersand, 0, "&");
        Assert.IsNotNull(token);
        Assert.IsInstanceOfType<AmpersandToken>(token);
    }

    [TestMethod]
    public void Create_PipeToken_ReturnsCorrectType()
    {
        var token = TokenFactory.Create(TokenType.Pipe, 0, "|");
        Assert.IsNotNull(token);
        Assert.IsInstanceOfType<PipeToken>(token);
    }

    [TestMethod]
    public void Create_CaretToken_ReturnsCorrectType()
    {
        var token = TokenFactory.Create(TokenType.Caret, 0, "^");
        Assert.IsNotNull(token);
        Assert.IsInstanceOfType<CaretToken>(token);
    }

    [TestMethod]
    public void Create_LeftShiftToken_ReturnsCorrectType()
    {
        var token = TokenFactory.Create(TokenType.LeftShift, 0, "<<");
        Assert.IsNotNull(token);
        Assert.IsInstanceOfType<LeftShiftToken>(token);
    }

    [TestMethod]
    public void Create_RightShiftToken_ReturnsCorrectType()
    {
        var token = TokenFactory.Create(TokenType.RightShift, 0, ">>");
        Assert.IsNotNull(token);
        Assert.IsInstanceOfType<RightShiftToken>(token);
    }

    [TestMethod]
    public void Create_FatArrowToken_ReturnsCorrectType()
    {
        var token = TokenFactory.Create(TokenType.FatArrow, 0, "=>");
        Assert.IsNotNull(token);
        Assert.IsInstanceOfType<FatArrowToken>(token);
    }

    #endregion

    #region Complex Token Factory Tests

    [TestMethod]
    public void Create_FunctionToken_ReturnsCorrectType()
    {
        var token = TokenFactory.Create(TokenType.Function, 0, "COUNT");
        Assert.IsNotNull(token);
        Assert.IsInstanceOfType<FunctionToken>(token);
    }

    [TestMethod]
    public void Create_DecimalToken_ReturnsCorrectType()
    {
        var token = TokenFactory.Create(TokenType.Decimal, 0, "123.45");
        Assert.IsNotNull(token);
        Assert.IsInstanceOfType<DecimalToken>(token);
    }

    [TestMethod]
    public void Create_DecimalTokenWithSuffix_ReturnsCorrectType()
    {
        var token = TokenFactory.Create(TokenType.Decimal, 0, "123.45d");
        Assert.IsNotNull(token);
        Assert.IsInstanceOfType<DecimalToken>(token);
    }

    [TestMethod]
    public void Create_IntegerToken_ReturnsCorrectType()
    {
        var token = TokenFactory.Create(TokenType.Integer, 0, "123");
        Assert.IsNotNull(token);
        Assert.IsInstanceOfType<IntegerToken>(token);
    }

    [TestMethod]
    public void Create_IntegerTokenWithAbbreviation_ReturnsCorrectType()
    {
        var token = TokenFactory.Create(TokenType.Integer, 0, "5KB");
        Assert.IsNotNull(token);
        Assert.IsInstanceOfType<IntegerToken>(token);
    }

    [TestMethod]
    public void Create_HexadecimalIntegerToken_ReturnsCorrectType()
    {
        var token = TokenFactory.Create(TokenType.HexadecimalInteger, 0, "0xFF");
        Assert.IsNotNull(token);
        Assert.IsInstanceOfType<HexIntegerToken>(token);
    }

    [TestMethod]
    public void Create_BinaryIntegerToken_ReturnsCorrectType()
    {
        var token = TokenFactory.Create(TokenType.BinaryInteger, 0, "0b101");
        Assert.IsNotNull(token);
        Assert.IsInstanceOfType<BinaryIntegerToken>(token);
    }

    [TestMethod]
    public void Create_OctalIntegerToken_ReturnsCorrectType()
    {
        var token = TokenFactory.Create(TokenType.OctalInteger, 0, "0o77");
        Assert.IsNotNull(token);
        Assert.IsInstanceOfType<OctalIntegerToken>(token);
    }

    [TestMethod]
    public void Create_AliasedStarToken_ReturnsCorrectType()
    {
        var token = TokenFactory.Create(TokenType.AliasedStar, 0, "alias.*");
        Assert.IsNotNull(token);
        Assert.IsInstanceOfType<AliasedStarToken>(token);
    }

    [TestMethod]
    public void Create_IdentifierToken_ReturnsColumnToken()
    {
        var token = TokenFactory.Create(TokenType.Identifier, 0, "columnName");
        Assert.IsNotNull(token);
        Assert.IsInstanceOfType<ColumnToken>(token);
    }

    [TestMethod]
    public void Create_PropertyToken_ReturnsAccessPropertyToken()
    {
        var token = TokenFactory.Create(TokenType.Property, 0, ".propertyName");
        Assert.IsNotNull(token);
        Assert.IsInstanceOfType<AccessPropertyToken>(token);
    }

    [TestMethod]
    public void Create_SkipToken_ReturnsCorrectType()
    {
        var token = TokenFactory.Create(TokenType.Skip, 0, "skip 10");
        Assert.IsNotNull(token);
        Assert.IsInstanceOfType<SkipToken>(token);
    }

    [TestMethod]
    public void Create_TakeToken_ReturnsCorrectType()
    {
        var token = TokenFactory.Create(TokenType.Take, 0, "take 10");
        Assert.IsNotNull(token);
        Assert.IsInstanceOfType<TakeToken>(token);
    }

    [TestMethod]
    public void Create_FieldLinkToken_ReturnsCorrectType()
    {
        var token = TokenFactory.Create(TokenType.FieldLink, 0, "@link");
        Assert.IsNotNull(token);
        Assert.IsInstanceOfType<FieldLinkToken>(token);
    }

    [TestMethod]
    public void Create_CommentToken_ReturnsCorrectType()
    {
        var token = TokenFactory.Create(TokenType.Comment, 0, "--comment");
        Assert.IsNotNull(token);
        Assert.IsInstanceOfType<CommentToken>(token);
    }

    [TestMethod]
    public void Create_WordToken_ReturnsCorrectType()
    {
        var token = TokenFactory.Create(TokenType.Word, 0, "word");
        Assert.IsNotNull(token);
        Assert.IsInstanceOfType<WordToken>(token);
    }

    [TestMethod]
    public void Create_NumericAccessToken_WithMatch_ReturnsCorrectType()
    {
        var regex = new Regex(@"(\w+)\[(\d+)\]");
        var match = regex.Match("array[5]");
        var token = TokenFactory.Create(TokenType.NumericAccess, 0, "array[5]", match);
        Assert.IsNotNull(token);
        Assert.IsInstanceOfType<NumericAccessToken>(token);
    }

    [TestMethod]
    public void Create_KeyAccessToken_WithMatch_ReturnsCorrectType()
    {
        var regex = new Regex(@"(\w+)\['([^']+)'\]");
        var match = regex.Match("dict['key']");
        var token = TokenFactory.Create(TokenType.KeyAccess, 0, "dict['key']", match);
        Assert.IsNotNull(token);
        Assert.IsInstanceOfType<KeyAccessToken>(token);
    }

    [TestMethod]
    public void Create_OuterJoinToken_LeftJoin_ReturnsCorrectType()
    {
        var regex = new Regex(@"(left|right)\s+outer\s+join", RegexOptions.IgnoreCase);
        var match = regex.Match("left outer join");
        var token = TokenFactory.Create(TokenType.OuterJoin, 0, "left outer join", match);
        Assert.IsNotNull(token);
        Assert.IsInstanceOfType<OuterJoinToken>(token);
        var outerJoin = (OuterJoinToken)token;
        Assert.AreEqual(OuterJoinType.Left, outerJoin.Type);
    }

    [TestMethod]
    public void Create_OuterJoinToken_RightJoin_ReturnsCorrectType()
    {
        var regex = new Regex(@"(left|right)\s+outer\s+join", RegexOptions.IgnoreCase);
        var match = regex.Match("right outer join");
        var token = TokenFactory.Create(TokenType.OuterJoin, 0, "right outer join", match);
        Assert.IsNotNull(token);
        Assert.IsInstanceOfType<OuterJoinToken>(token);
        var outerJoin = (OuterJoinToken)token;
        Assert.AreEqual(OuterJoinType.Right, outerJoin.Type);
    }

    [TestMethod]
    public void Create_MethodAccessToken_WithMatch_ReturnsCorrectType()
    {
        var regex = new Regex(@"\.(\w+)\(");
        var match = regex.Match(".MethodName(");
        var token = TokenFactory.Create(TokenType.MethodAccess, 0, ".MethodName(", match);
        Assert.IsNotNull(token);
        Assert.IsInstanceOfType<MethodAccessToken>(token);
    }

    #endregion

    #region Special Method Tests

    [TestMethod]
    public void CreateSchemaKeywordToken_ReturnsSchemaKeywordToken()
    {
        var token = TokenFactory.CreateSchemaKeywordToken(TokenType.Word, 0, "schema");
        Assert.IsNotNull(token);
        Assert.IsInstanceOfType<SchemaKeywordToken>(token);
    }

    [TestMethod]
    public void CreateStringLiteralToken_ReturnsStringLiteralToken()
    {
        var token = TokenFactory.CreateStringLiteralToken(0, "hello", 7);
        Assert.IsNotNull(token);
        Assert.IsInstanceOfType<StringLiteralToken>(token);
    }

    [TestMethod]
    public void CreateEmptyWordToken_ReturnsWordToken()
    {
        var token = TokenFactory.CreateEmptyWordToken(0);
        Assert.IsNotNull(token);
        Assert.IsInstanceOfType<WordToken>(token);
    }

    #endregion

    #region Edge Cases

    [TestMethod]
    public void Create_UnknownTokenType_ReturnsNull()
    {
        var token = TokenFactory.Create(TokenType.EndOfFile, 0, "");
        Assert.IsNull(token);
    }

    [TestMethod]
    public void Create_SingleDigitInteger_ReturnsIntegerToken()
    {
        var token = TokenFactory.Create(TokenType.Integer, 0, "5");
        Assert.IsNotNull(token);
        Assert.IsInstanceOfType<IntegerToken>(token);
    }

    [TestMethod]
    public void Create_IntegerWithLongAbbreviation_ReturnsIntegerToken()
    {
        var token = TokenFactory.Create(TokenType.Integer, 0, "100MB");
        Assert.IsNotNull(token);
        Assert.IsInstanceOfType<IntegerToken>(token);
    }

    #endregion
}
