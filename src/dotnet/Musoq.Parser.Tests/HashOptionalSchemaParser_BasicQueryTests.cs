using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Lexing;

namespace Musoq.Parser.Tests;

[TestClass]
public class HashOptionalSchemaParser_BasicQueryTests
{
    #region Table Aliases

    [TestMethod]
    [DataRow("select t.Col from schema.method() t")]
    [DataRow("select t.Col from schema.method() as t")]
    [DataRow("select alias.Col from schema.method() alias")]
    [DataRow("select longAlias.Col from schema.method() as longAlias")]
    public void HashOptional_WithTableAlias_ShouldParse(string query)
    {
        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();
        Assert.IsNotNull(result);
    }

    #endregion

    #region SKIP and TAKE

    [TestMethod]
    [DataRow("select Col from schema.method() skip 10")]
    [DataRow("select Col from schema.method() take 20")]
    [DataRow("select Col from schema.method() skip 10 take 20")]
    [DataRow("select Col from schema.method() order by Col skip 5 take 10")]
    public void HashOptional_WithSkipTake_ShouldParse(string query)
    {
        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();
        Assert.IsNotNull(result);
    }

    #endregion

    #region DISTINCT

    [TestMethod]
    [DataRow("select distinct Col from schema.method()")]
    [DataRow("select distinct Col1, Col2 from schema.method()")]
    [DataRow("select distinct * from schema.method()")]
    [DataRow("SELECT DISTINCT Col FROM schema.method()")]
    public void HashOptional_WithDistinct_ShouldParse(string query)
    {
        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();
        Assert.IsNotNull(result);
    }

    #endregion

    #region Comments

    [TestMethod]
    [DataRow("-- Comment\nselect 1 from schema.method()")]
    [DataRow("select 1 from schema.method() -- comment")]
    [DataRow("/* comment */ select 1 from schema.method()")]
    [DataRow("select /* comment */ 1 from schema.method()")]
    public void HashOptional_WithComments_ShouldParse(string query)
    {
        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();
        Assert.IsNotNull(result);
    }

    #endregion

    #region Semicolons

    [TestMethod]
    [DataRow("select 1 from schema.method();")]
    [DataRow("select 1 from schema.method() ;")]
    [DataRow("with cte as (select 1 from schema.method()) select * from cte;")]
    public void HashOptional_WithSemicolon_ShouldParse(string query)
    {
        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();
        Assert.IsNotNull(result);
    }

    #endregion

    #region Column Aliases

    [TestMethod]
    [DataRow("select Col as Alias from schema.method()")]
    [DataRow("select Col Alias from schema.method()")]
    [DataRow("select Col1 as A1, Col2 as A2 from schema.method()")]
    [DataRow("select 1 + 2 as Sum from schema.method()")]
    public void HashOptional_ColumnAliases_ShouldParse(string query)
    {
        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();
        Assert.IsNotNull(result);
    }

    #endregion

    #region Basic SELECT Queries

    [TestMethod]
    [DataRow("select 1 from schema.method()")]
    [DataRow("select * from schema.method()")]
    [DataRow("select a, b, c from schema.method()")]
    [DataRow("SELECT 1 FROM schema.method()")]
    [DataRow("Select 1 From schema.method()")]
    public void HashOptional_BasicSelect_ShouldParse(string query)
    {
        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();
        Assert.IsNotNull(result);
    }

    [TestMethod]
    [DataRow("select 1 from a.b()")]
    [DataRow("select 1 from longSchemaName.longMethodName()")]
    [DataRow("select 1 from UPPER.case()")]
    [DataRow("select 1 from lower.CASE()")]
    [DataRow("select 1 from MixedCase.MixedMethod()")]
    public void HashOptional_DifferentNamingConventions_ShouldParse(string query)
    {
        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();
        Assert.IsNotNull(result);
    }

    [TestMethod]
    [DataRow("select 1 from schema.method('param')")]
    [DataRow("select 1 from schema.method(123)")]
    [DataRow("select 1 from schema.method(1.5d)")]
    [DataRow("select 1 from schema.method(true)")]
    [DataRow("select 1 from schema.method(false)")]
    [DataRow("select 1 from schema.method('a', 'b', 'c')")]
    [DataRow("select 1 from schema.method(1, 2, 3)")]
    [DataRow("select 1 from schema.method('text', 123, true)")]
    public void HashOptional_WithMethodParameters_ShouldParse(string query)
    {
        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();
        Assert.IsNotNull(result);
    }

    #endregion

    #region Reordered (FROM-first) Queries

    [TestMethod]
    [DataRow("from schema.method() select 1")]
    [DataRow("from schema.method() select *")]
    [DataRow("from schema.method() select a, b, c")]
    [DataRow("FROM schema.method() SELECT 1")]
    public void HashOptional_ReorderedQuery_ShouldParse(string query)
    {
        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void HashOptional_ReorderedWithWhereGroupByOrderBy_ShouldParse()
    {
        var query = "from schema.method() where x > 5 group by y select z order by z desc";
        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();
        Assert.IsNotNull(result);
    }

    #endregion
}
