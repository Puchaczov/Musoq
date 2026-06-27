using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Lexing;
using Musoq.Parser.Tokens;

namespace Musoq.Parser.Tests;

public partial class TokenFactoryTests
{
    #region Simple Token Factory Tests
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
}
