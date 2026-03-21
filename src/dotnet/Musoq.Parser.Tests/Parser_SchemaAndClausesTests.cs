using System.Diagnostics;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Lexing;
using Musoq.Parser.Nodes;
using Musoq.Parser.Tokens;

namespace Musoq.Parser.Tests;

/// <summary>
///     Parser tests: Schema syntax, BETWEEN clause, and COUNT(DISTINCT).
/// </summary>
[TestClass]
public class Parser_SchemaAndClausesTests
{
    [TestMethod]
    public void SchemaWithoutHash_BasicSelect_ShouldParse()
    {
        var query = "select 1 from some.a()";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();

        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void SchemaWithoutHash_SelectWithColumns_ShouldParse()
    {
        var query = "select Name, Age from schema.entities()";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();

        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void SchemaWithoutHash_ReorderedQuery_ShouldParse()
    {
        var query = "from some.a() select 1";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();

        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void SchemaWithoutHash_WithWhereClause_ShouldParse()
    {
        var query = "select Name from some.a() where Name = 'test'";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();

        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void SchemaWithoutHash_WithGroupBy_ShouldParse()
    {
        var query = "select Name, Count(Name) from some.a() group by Name";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();

        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void SchemaWithoutHash_WithOrderBy_ShouldParse()
    {
        var query = "select Name from some.a() order by Name desc";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();

        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void SchemaWithoutHash_InnerJoin_ShouldParse()
    {
        var query = "select a.Name from some.a() a inner join other.b() b on a.Id = b.Id";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();

        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void SchemaWithoutHash_LeftJoin_ShouldParse()
    {
        var query = "select a.Name from some.a() a left join other.b() b on a.Id = b.Id";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();

        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void SchemaWithoutHash_LeftOuterJoin_ShouldParse()
    {
        var query = "select a.Name from some.a() a left outer join other.b() b on a.Id = b.Id";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();

        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void SchemaWithoutHash_CrossApply_ShouldParse()
    {
        var query = "select a.Name, b.Value from some.first() a cross apply some.second(a.Country) b";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();

        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void SchemaWithoutHash_OuterApply_ShouldParse()
    {
        var query = "select a.Name, b.Value from some.first() a outer apply some.second(a.Country) b";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();

        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void SchemaWithoutHash_WithCte_ShouldParse()
    {
        var query = "with cte as (select Name from some.a()) select * from cte";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();

        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void SchemaWithoutHash_WithSkipTake_ShouldParse()
    {
        var query = "select Name from some.a() skip 10 take 5";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();

        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void SchemaWithoutHash_UnionOperator_ShouldParse()
    {
        var query = "select Name from some.a() union (Name) select Name from other.b()";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();

        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void SchemaWithHash_StillWorks_ShouldParse()
    {
        var query = "select 1 from #some.a()";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();

        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void SchemaMixedSyntax_WithAndWithoutHash_ShouldParse()
    {
        var query = "select a.Name from #some.a() a inner join other.b() b on a.Id = b.Id";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();

        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void SchemaWithoutHash_ComplexQuery_ShouldParse()
    {
        var query = @"
            select a.Name, b.Value, Count(a.Name)
            from schema.entities() a 
            inner join other.data() b on a.Id = b.Id
            where a.Active = true
            group by a.Name, b.Value
            having Count(a.Name) > 1
            order by a.Name desc
            skip 5 take 10";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();

        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void SchemaWithoutHash_WithAlias_ShouldParse()
    {
        var query = "select s.Name from some.a() s";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();

        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void SchemaWithoutHash_WithAsAlias_ShouldParse()
    {
        var query = "select s.Name from some.a() as s";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();

        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void SchemaWithoutHash_WithMethodParameters_ShouldParse()
    {
        var query = "select 1 from some.method('param1', 123)";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();

        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void SchemaWithoutHash_CaseInsensitiveKeywords_ShouldParse()
    {
        var query = "SELECT Name FROM some.a() WHERE Name = 'test'";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();

        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void Between_WithIntegers_ShouldParse()
    {
        var query = "select 1 from #some.a() where value between 1 and 10";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();

        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void Between_WithColumnsAsMinMax_ShouldParse()
    {
        var query = "select 1 from #some.a() where age between min_age and max_age";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();

        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void Between_Uppercase_ShouldParse()
    {
        var query = "SELECT 1 FROM #some.a() WHERE VALUE BETWEEN 1 AND 10";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();

        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void Between_WithDecimalValues_ShouldParse()
    {
        var query = "select 1 from #some.a() where price between 10.5 and 99.99";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();

        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void Between_WithStrings_ShouldParse()
    {
        var query = "select 1 from #some.a() where name between 'A' and 'Z'";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();

        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void Between_InSelectClause_ShouldParse()
    {
        var query = "select value between 1 and 10 as IsInRange from #some.a()";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();

        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void Between_CombinedWithAnd_ShouldParse()
    {
        var query = "select 1 from #some.a() where value between 1 and 10 and name = 'test'";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();

        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void Between_CombinedWithOr_ShouldParse()
    {
        var query = "select 1 from #some.a() where value between 1 and 10 or value = 0";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();

        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void Between_MultipleBetweensWithAnd_ShouldParse()
    {
        var query = "select 1 from #some.a() where a between 1 and 10 and b between 20 and 30";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();

        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void Between_WithParentheses_ShouldParse()
    {
        var query = "select 1 from #some.a() where (value between 1 and 10) and active = true";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();

        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void Between_InCrossApply_ShouldParse()
    {
        var query = @"
            select c.col1 
            from #some.a() a 
            cross apply #some.b(c.x) c 
            where c.col1 between 1 and 100";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();

        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void CountDistinct_ShouldParseSuccessfully()
    {
        var query = "select Count(distinct Name) from #some.a()";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();

        Assert.IsNotNull(result);

        var statementsArray = result.Expression as StatementsArrayNode;
        Assert.IsNotNull(statementsArray);

        var singleSet = statementsArray.Statements[0].Node as SingleSetNode;
        Assert.IsNotNull(singleSet);

        var select = singleSet.Query.Select;
        var field = select.Fields[0];
        var accessMethod = field.Expression as AccessMethodNode;

        Assert.IsNotNull(accessMethod);
        Assert.AreEqual("Count", accessMethod.Name);
        Assert.IsTrue(accessMethod.IsDistinct);
    }

    [TestMethod]
    public void CountDistinct_WithGroupBy_ShouldParse()
    {
        var query = "select Category, Count(distinct Name) from #some.a() group by Category";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();

        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void CountDistinct_WithCrossApply_ShouldParse()
    {
        var query = @"
            select Count(distinct t.Value) as UniqueTagCount
            from #some.a() p
            cross apply p.Tags t";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();

        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void CountDistinct_CaseInsensitive_ShouldParse()
    {
        var query = "select COUNT(DISTINCT Name) from #some.a()";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();

        Assert.IsNotNull(result);

        var statementsArray = result.Expression as StatementsArrayNode;
        Assert.IsNotNull(statementsArray);

        var singleSet = statementsArray.Statements[0].Node as SingleSetNode;
        Assert.IsNotNull(singleSet);

        var select = singleSet.Query.Select;
        var field = select.Fields[0];
        var accessMethod = field.Expression as AccessMethodNode;

        Assert.IsNotNull(accessMethod);
        Assert.IsTrue(accessMethod.IsDistinct);
    }

    [TestMethod]
    public void RegularCount_ShouldNotBeDistinct()
    {
        var query = "select Count(Name) from #some.a()";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();

        Assert.IsNotNull(result);

        var statementsArray = result.Expression as StatementsArrayNode;
        Assert.IsNotNull(statementsArray);

        var singleSet = statementsArray.Statements[0].Node as SingleSetNode;
        Assert.IsNotNull(singleSet);

        var select = singleSet.Query.Select;
        var field = select.Fields[0];
        var accessMethod = field.Expression as AccessMethodNode;

        Assert.IsNotNull(accessMethod);
        Assert.AreEqual("Count", accessMethod.Name);
        Assert.IsFalse(accessMethod.IsDistinct);
    }
}
