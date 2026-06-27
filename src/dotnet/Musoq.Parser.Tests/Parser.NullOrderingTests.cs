using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Lexing;
using Musoq.Parser.Nodes;

namespace Musoq.Parser.Tests;

[TestClass]
public sealed class ParserNullOrderingTests
{
    [TestMethod]
    public void OrderBy_NullsLast_ShouldParseTopLevelFieldModifier()
    {
        var query = ParseQuery("select Name from #A.entities() order by City nulls last");
        var field = query.OrderBy?.Fields[0] ?? throw new InvalidOperationException("Expected ORDER BY.");

        Assert.AreEqual(Order.Ascending, field.Order);
        Assert.AreEqual(NullOrdering.Last, field.NullOrdering);
        Assert.AreEqual("City nulls last", field.ToString());
    }

    [TestMethod]
    public void OrderBy_DescNullsFirst_ShouldParseTopLevelFieldModifier()
    {
        var query = ParseQuery("select Name from #A.entities() order by City desc nulls first");
        var field = query.OrderBy?.Fields[0] ?? throw new InvalidOperationException("Expected ORDER BY.");

        Assert.AreEqual(Order.Descending, field.Order);
        Assert.AreEqual(NullOrdering.First, field.NullOrdering);
        Assert.AreEqual("City desc nulls first", field.ToString());
    }

    [TestMethod]
    public void WindowOrderBy_NullsLast_ShouldParseWindowFieldModifier()
    {
        var expression = ParseSingleSelectExpression(
            "RowNumber() over (partition by Country order by City desc nulls last)");
        var window = Assert.IsInstanceOfType<WindowFunctionNode>(expression);
        var field = window.WindowSpecification?.OrderByFields[0] ??
                    throw new InvalidOperationException("Expected window ORDER BY.");

        Assert.AreEqual(Order.Descending, field.Order);
        Assert.AreEqual(NullOrdering.Last, field.NullOrdering);
    }

    [TestMethod]
    public void OrderBy_NullsWithoutFirstOrLast_ShouldThrowSyntaxException()
    {
        Assert.ThrowsExactly<SyntaxException>(() =>
            ParseQuery("select Name from #A.entities() order by City nulls middle"));
    }

    private static Node ParseSingleSelectExpression(string expression)
    {
        var query = ParseQuery($"select {expression} from #A.entities()");
        return query.Select.Fields[0].Expression;
    }

    private static QueryNode ParseQuery(string query)
    {
        var root = new Parser(new Lexer(query, true)).ComposeAll();
        var statements = (StatementsArrayNode)root.Expression;
        var singleSet = (SingleSetNode)statements.Statements[0].Node;
        return singleSet.Query;
    }
}
