using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Lexing;
using Musoq.Parser.Nodes;

namespace Musoq.Parser.Tests;

[TestClass]
public sealed class CteColumnListParserTests
{
    [TestMethod]
    public void CteColumnList_ShouldBeRetainedInAst()
    {
        var lexer = new Lexer(
            "with places (Name, Nation) as (select City, Country from #A.entities()) select Name from places",
            true);
        var root = new Parser(lexer).ComposeAll();

        var statements = (StatementsArrayNode)((RootNode)root).Expression;
        var cte = (CteExpressionNode)statements.Statements[0].Node;

        CollectionAssert.AreEqual(
            new[] { "Name", "Nation" },
            cte.InnerExpression[0].ColumnNames);
        StringAssert.Contains(cte.ToString(), "places (Name, Nation) as");
    }

    [TestMethod]
    public void WithRecursive_ShouldBeRetainedAsContextualCteModifier()
    {
        var lexer = new Lexer(
            "with recursive counter (Value) as (" +
            "select Value from values {{ Value: 1 }} seed " +
            "union all select c.Value + 1 from counter c where c.Value < 3) " +
            "select Value from counter",
            true);
        var root = new Parser(lexer).ComposeAll();

        var statements = (StatementsArrayNode)((RootNode)root).Expression;
        var cte = (CteExpressionNode)statements.Statements[0].Node;

        Assert.IsTrue(cte.IsRecursive);
        StringAssert.StartsWith(cte.ToString(), "with recursive ");
    }

    [TestMethod]
    public void Recursive_WhenFollowedByAs_ShouldRemainAnOrdinaryCteName()
    {
        var lexer = new Lexer(
            "with recursive as (select Value from values {{ Value: 1 }} seed) " +
            "select Value from recursive",
            true);
        var root = new Parser(lexer).ComposeAll();

        var statements = (StatementsArrayNode)((RootNode)root).Expression;
        var cte = (CteExpressionNode)statements.Statements[0].Node;

        Assert.IsFalse(cte.IsRecursive);
        Assert.AreEqual("recursive", cte.InnerExpression[0].Name);
    }

    [TestMethod]
    public void Recursive_WhenFollowedByColumnList_ShouldRemainAnOrdinaryCteName()
    {
        var lexer = new Lexer(
            "with recursive (Exported) as (select Value from values {{ Value: 1 }} seed) " +
            "select Exported from recursive",
            true);
        var root = new Parser(lexer).ComposeAll();

        var statements = (StatementsArrayNode)((RootNode)root).Expression;
        var cte = (CteExpressionNode)statements.Statements[0].Node;

        Assert.IsFalse(cte.IsRecursive);
        Assert.AreEqual("recursive", cte.InnerExpression[0].Name);
        CollectionAssert.AreEqual(new[] { "Exported" }, cte.InnerExpression[0].ColumnNames);
    }
}
