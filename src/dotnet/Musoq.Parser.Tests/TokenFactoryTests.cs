using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Lexing;
using Musoq.Parser.Tokens;

namespace Musoq.Parser.Tests;

/// <summary>
///     Tests for TokenFactory to improve branch coverage by testing all token types.
/// </summary>
[TestClass]
public partial class TokenFactoryTests
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
    #endregion
}
