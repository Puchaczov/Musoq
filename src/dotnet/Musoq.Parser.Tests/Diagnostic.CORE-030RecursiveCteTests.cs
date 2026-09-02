using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Lexing;
using Musoq.Parser.Nodes;

namespace Musoq.Parser.Tests;

[TestClass]
public sealed class DiagnosticCore030RecursiveCteTests
{
    [TestMethod]
    public void WithRecursive_ShouldExposeOneAnchorAndOneRecursiveMemberAtTopLevel()
    {
        const string query =
            "with recursive counter (Value) as (" +
            "select Value from values {{ Value: 1 }} seed " +
            "union all select c.Value + 1 from counter c where c.Value < 3) " +
            "select Value from counter";

        var cte = ParseCte(query);
        var definition = cte.InnerExpression[0];

        Assert.IsTrue(cte.IsRecursive);
        Assert.AreEqual("counter", definition.Name);
        CollectionAssert.AreEqual(new[] { "Value" }, definition.ColumnNames);
        Assert.IsInstanceOfType<UnionAllNode>(definition.Value);

        var boundary = (UnionAllNode)definition.Value;
        Assert.IsInstanceOfType<QueryNode>(boundary.Left);
        Assert.IsInstanceOfType<QueryNode>(boundary.Right);
        Assert.IsFalse(boundary.Left == boundary.Right);
        Assert.AreEqual(
            new TextSpan(query.IndexOf("Value", StringComparison.Ordinal), "Value".Length),
            definition.Columns[0].Span);
    }

    [TestMethod]
    public void RecursiveTokenBeforeAs_ShouldRemainAnOrdinaryCteName()
    {
        const string query =
            "with recursive as (select Value from values {{ Value: 1 }} seed) " +
            "select Value from recursive";

        var cte = ParseCte(query);

        Assert.IsFalse(cte.IsRecursive);
        Assert.AreEqual("recursive", cte.InnerExpression[0].Name);
    }

    private static CteExpressionNode ParseCte(string query)
    {
        var lexer = new Lexer(query, true);
        var root = new Parser(lexer).ComposeAll();
        var statements = (StatementsArrayNode)((RootNode)root).Expression;
        return (CteExpressionNode)statements.Statements[0].Node;
    }
}
