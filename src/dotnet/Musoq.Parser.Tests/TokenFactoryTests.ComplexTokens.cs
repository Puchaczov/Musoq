using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Lexing;
using Musoq.Parser.Tokens;

namespace Musoq.Parser.Tests;

public partial class TokenFactoryTests
{
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
    public void Create_DoubleColonToken_ReturnsCorrectType()
    {
        var token = TokenFactory.Create(TokenType.DoubleColon, 0, "::");
        Assert.IsNotNull(token);
        Assert.IsInstanceOfType<DoubleColonToken>(token);
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
        var regex = new Regex(@"(left|right|full)(?:\s+outer)?\s+join", RegexOptions.IgnoreCase);
        var match = regex.Match("left outer join");
        var token = TokenFactory.Create(TokenType.OuterJoin, 0, "left outer join", match);
        Assert.IsNotNull(token);
        Assert.IsInstanceOfType<OuterJoinToken>(token);
        var outerJoin = (OuterJoinToken)token;
        Assert.AreEqual(OuterJoinType.Left, outerJoin.Type);
    }

    [TestMethod]
    public void Create_OuterJoinToken_LeftJoinWithoutOuter_ReturnsCorrectType()
    {
        var regex = new Regex(@"(left|right|full)(?:\s+outer)?\s+join", RegexOptions.IgnoreCase);
        var match = regex.Match("left join");
        var token = TokenFactory.Create(TokenType.OuterJoin, 0, "left join", match);
        Assert.IsNotNull(token);
        Assert.IsInstanceOfType<OuterJoinToken>(token);
        var outerJoin = (OuterJoinToken)token;
        Assert.AreEqual(OuterJoinType.Left, outerJoin.Type);
    }

    [TestMethod]
    public void Create_OuterJoinToken_RightJoin_ReturnsCorrectType()
    {
        var regex = new Regex(@"(left|right|full)(?:\s+outer)?\s+join", RegexOptions.IgnoreCase);
        var match = regex.Match("right outer join");
        var token = TokenFactory.Create(TokenType.OuterJoin, 0, "right outer join", match);
        Assert.IsNotNull(token);
        Assert.IsInstanceOfType<OuterJoinToken>(token);
        var outerJoin = (OuterJoinToken)token;
        Assert.AreEqual(OuterJoinType.Right, outerJoin.Type);
    }

    [TestMethod]
    public void Create_OuterJoinToken_RightJoinWithoutOuter_ReturnsCorrectType()
    {
        var regex = new Regex(@"(left|right|full)(?:\s+outer)?\s+join", RegexOptions.IgnoreCase);
        var match = regex.Match("right join");
        var token = TokenFactory.Create(TokenType.OuterJoin, 0, "right join", match);
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
}
