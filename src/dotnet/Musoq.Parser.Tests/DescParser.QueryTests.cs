using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Lexing;
using Musoq.Parser.Nodes;

namespace Musoq.Parser.Tests;

[TestClass]
public class DescParserQueryTests : DescParserTestBase
{
    [TestMethod]
    public void DescQuery_WithSelect_ShouldParse()
    {
        var desc = ParseDesc("desc query (select Name from #schema.method())");

        Assert.AreEqual(DescForType.Query, desc.Type);
        Assert.IsNotNull(desc.Query);
    }

    [TestMethod]
    public void DescQuery_WithCte_ShouldParse()
    {
        var desc = ParseDesc("desc query (with cte as (select Name from #schema.method()) select Name from cte)");

        Assert.AreEqual(DescForType.Query, desc.Type);
        Assert.IsNotNull(desc.Query);
    }

    [TestMethod]
    [DataRow("desc query (select Name from #schema.method() union (Name) select Name from #schema.method())")]
    [DataRow("desc query (select Name from #schema.method() union all (Name) select Name from #schema.method())")]
    [DataRow("desc query (select Name from #schema.method() except (Name) select Name from #schema.method())")]
    [DataRow("desc query (select Name from #schema.method() intersect (Name) select Name from #schema.method())")]
    [DataRow("desc query (from #schema.method() select Name as Label)")]
    [DataRow("desc query (select * exclude (City) replace (Population * 2 as Population) rename (Name as EntityName, Population as WeightedPopulation) from #schema.method())")]
    [DataRow("desc query (with cte as (select Name from #schema.method()) select Name from cte)")]
    [DataRow("desc query (select a.Name, b.City from #schema.method() a inner join #schema.method() b on a.Id = b.Id)")]
    [DataRow("desc query (select a.Name, n.Value, n.Ordinal from #schema.method() a cross apply a.Numbers n with ordinality)")]
    [DataRow("desc query (select Name, RowNumber() over (order by NullableValue nulls last) as RowNo from #schema.method())")]
    public void DescQuery_WithBroadInnerQuerySyntax_ShouldParse(string query)
    {
        var desc = ParseDesc(query);

        Assert.AreEqual(DescForType.Query, desc.Type);
        Assert.IsNotNull(desc.Query);
    }

    [TestMethod]
    public void DescQuery_WithInvalidInnerExpression_ShouldFail()
    {
        Assert.Throws<SyntaxException>(() => ParseDesc("desc query (1 + 2)"));
    }

    [TestMethod]
    public void DescQuery_WithoutParentheses_ShouldFail()
    {
        Assert.Throws<SyntaxException>(() => ParseDesc("desc query select Name from #schema.method()"));
    }

    private static DescNode ParseDesc(string query)
    {
        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        return GetDescNode(parser.ComposeAll());
    }
}
