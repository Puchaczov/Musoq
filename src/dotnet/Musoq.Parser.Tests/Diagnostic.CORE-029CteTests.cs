using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Lexing;
using Musoq.Parser.Nodes;

namespace Musoq.Parser.Tests;

[TestClass]
public sealed class DiagnosticCore029CteTests
{
    [TestMethod]
    public void CteDeclarations_ShouldPreserveDeclarationOrderAndOutputNameList()
    {
        const string query =
            "with first (CityName) as (select City from #A.Entities()), " +
            "second (PlaceName) as (select CityName from first) " +
            "select PlaceName from second";

        var cte = ParseCte(query);

        Assert.IsFalse(cte.IsRecursive);
        Assert.HasCount(2, cte.InnerExpression);
        Assert.AreEqual("first", cte.InnerExpression[0].Name);
        Assert.AreEqual("second", cte.InnerExpression[1].Name);
        CollectionAssert.AreEqual(new[] { "CityName" }, cte.InnerExpression[0].ColumnNames);
        CollectionAssert.AreEqual(new[] { "PlaceName" }, cte.InnerExpression[1].ColumnNames);
        Assert.AreEqual(
            new TextSpan(query.IndexOf("CityName", StringComparison.Ordinal), "CityName".Length),
            cte.InnerExpression[0].Columns[0].Span);
        Assert.AreEqual(
            new TextSpan(query.IndexOf("PlaceName", StringComparison.Ordinal), "PlaceName".Length),
            cte.InnerExpression[1].Columns[0].Span);
    }

    [TestMethod]
    public void CteDeclarationBoundary_ShouldKeepTheOuterQueryOutsideTheDefinitionList()
    {
        const string query =
            "with source as (select City from #A.Entities()) " +
            "select City from source";
        var cte = ParseCte(query);

        Assert.IsInstanceOfType(cte.OuterExpression, typeof(SingleSetNode));
        var outer = (SingleSetNode)cte.OuterExpression;
        Assert.IsInstanceOfType(outer.Query, typeof(QueryNode));
        Assert.AreEqual("source", cte.InnerExpression[0].Name);
        StringAssert.Contains(cte.ToString(), "source as");
        StringAssert.EndsWith(cte.ToString(), "select City from source");
    }

    private static CteExpressionNode ParseCte(string query)
    {
        var lexer = new Lexer(query, true);
        var root = new Parser(lexer).ComposeAll();
        var statements = (StatementsArrayNode)((RootNode)root).Expression;
        return (CteExpressionNode)statements.Statements[0].Node;
    }
}
