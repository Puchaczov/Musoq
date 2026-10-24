using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Lexing;
using Musoq.Parser.Nodes;

namespace Musoq.Parser.Tests;

[TestClass]
public sealed class SqlConcatenationParserTests
{
    [TestMethod]
    public void Concatenation_ShouldAssociateToTheLeft()
    {
        var expression = (StringConcatenationNode)Parse("'a' || 'b' || 'c'");

        Assert.IsInstanceOfType<StringConcatenationNode>(expression.Left);
        Assert.IsInstanceOfType<WordNode>(expression.Right);
    }

    [TestMethod]
    public void Concatenation_ShouldShareAdditivePrecedence()
    {
        var expression = (AddNode)Parse("'a' || 'b' + 'c'");

        Assert.IsInstanceOfType<StringConcatenationNode>(expression.Left);
        Assert.IsInstanceOfType<WordNode>(expression.Right);
    }

    [TestMethod]
    public void Multiplication_ShouldBindBeforeConcatenation()
    {
        var expression = (StringConcatenationNode)Parse("'a' || 2 * 3");

        Assert.IsInstanceOfType<StarNode>(expression.Right);
    }

    [TestMethod]
    public void SinglePipe_ShouldRemainBitwiseOr()
    {
        Assert.IsInstanceOfType<BitwiseOrNode>(Parse("1 | 2"));
    }

    private static Node Parse(string expression)
    {
        var root = new Parser(new Lexer($"select {expression} from #test.rows()", true)).ComposeAll();
        var statements = (StatementsArrayNode)root.Expression;
        return ((SingleSetNode)statements.Statements[0].Node).Query.Select.Fields[0].Expression;
    }
}
