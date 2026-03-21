using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Lexing;

namespace Musoq.Parser.Tests;

[TestClass]
public class HashOptionalSchemaParser_ClauseAndExpressionTests
{
    #region WHERE Clause

    [TestMethod]
    [DataRow("select 1 from schema.method() where Col = 'value'")]
    [DataRow("select 1 from schema.method() where Col = 1")]
    [DataRow("select 1 from schema.method() where Col > 5")]
    [DataRow("select 1 from schema.method() where Col >= 5")]
    [DataRow("select 1 from schema.method() where Col < 5")]
    [DataRow("select 1 from schema.method() where Col <= 5")]
    [DataRow("select 1 from schema.method() where Col <> 5")]
    [DataRow("select 1 from schema.method() where Col is null")]
    [DataRow("select 1 from schema.method() where Col is not null")]
    [DataRow("select 1 from schema.method() where Col like '%pattern%'")]
    [DataRow("select 1 from schema.method() where Col not like '%pattern%'")]
    [DataRow("select 1 from schema.method() where Col rlike '.*pattern.*'")]
    [DataRow("select 1 from schema.method() where Col not rlike '.*pattern.*'")]
    public void HashOptional_WithWhereClause_ShouldParse(string query)
    {
        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();
        Assert.IsNotNull(result);
    }

    [TestMethod]
    [DataRow("select 1 from schema.method() where Col1 = 1 and Col2 = 2")]
    [DataRow("select 1 from schema.method() where Col1 = 1 or Col2 = 2")]
    [DataRow("select 1 from schema.method() where Col1 = 1 and Col2 = 2 and Col3 = 3")]
    [DataRow("select 1 from schema.method() where Col1 = 1 or Col2 = 2 or Col3 = 3")]
    [DataRow("select 1 from schema.method() where (Col1 = 1 and Col2 = 2) or Col3 = 3")]
    [DataRow("select 1 from schema.method() where Col1 = 1 and (Col2 = 2 or Col3 = 3)")]
    public void HashOptional_WithCompoundWhereConditions_ShouldParse(string query)
    {
        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();
        Assert.IsNotNull(result);
    }

    [TestMethod]
    [DataRow("select 1 from schema.method() where Col in (1, 2, 3)")]
    [DataRow("select 1 from schema.method() where Col not in (1, 2, 3)")]
    [DataRow("select 1 from schema.method() where Col in ('a', 'b', 'c')")]
    [DataRow("select 1 from schema.method() where Col contains (1, 2, 3)")]
    public void HashOptional_WithInOperator_ShouldParse(string query)
    {
        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();
        Assert.IsNotNull(result);
    }

    #endregion

    #region GROUP BY and HAVING

    [TestMethod]
    [DataRow("select Col, Count(Col) from schema.method() group by Col")]
    [DataRow("select Col1, Col2, Sum(Col3) from schema.method() group by Col1, Col2")]
    [DataRow("select Col, Avg(Value) from schema.method() group by Col")]
    [DataRow("select Col, Min(Value), Max(Value) from schema.method() group by Col")]
    public void HashOptional_WithGroupBy_ShouldParse(string query)
    {
        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();
        Assert.IsNotNull(result);
    }

    [TestMethod]
    [DataRow("select Col, Count(Col) from schema.method() group by Col having Count(Col) > 1")]
    [DataRow("select Col, Sum(Value) from schema.method() group by Col having Sum(Value) > 100")]
    [DataRow("select Col, Avg(Value) from schema.method() group by Col having Avg(Value) < 50")]
    public void HashOptional_WithHaving_ShouldParse(string query)
    {
        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();
        Assert.IsNotNull(result);
    }

    #endregion

    #region ORDER BY

    [TestMethod]
    [DataRow("select Col from schema.method() order by Col")]
    [DataRow("select Col from schema.method() order by Col asc")]
    [DataRow("select Col from schema.method() order by Col desc")]
    [DataRow("select Col from schema.method() order by Col1 asc, Col2 desc")]
    [DataRow("select Col from schema.method() order by Col1, Col2, Col3")]
    public void HashOptional_WithOrderBy_ShouldParse(string query)
    {
        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();
        Assert.IsNotNull(result);
    }

    [TestMethod]
    [DataRow("select Col from schema.method() order by Length(Col) desc")]
    [DataRow("select Col from schema.method() order by (Col1 + Col2) desc")]
    [DataRow("select Col from schema.method() order by case when Col > 0 then 1 else 0 end")]
    public void HashOptional_OrderByWithExpressions_ShouldParse(string query)
    {
        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();
        Assert.IsNotNull(result);
    }

    #endregion

    #region CASE WHEN

    [TestMethod]
    [DataRow("select case when Col = 1 then 'one' else 'other' end from schema.method()")]
    [DataRow("select case when Col > 0 then 'positive' else 'negative' end from schema.method()")]
    public void HashOptional_CaseWhen_ShouldParse(string query)
    {
        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void HashOptional_CaseWhenMultipleBranches_ShouldParse()
    {
        var query =
            "select case when Col = 1 then 'one' when Col = 2 then 'two' when Col = 3 then 'three' else 'other' end from schema.method()";
        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void HashOptional_NestedCaseWhen_ShouldParse()
    {
        var query =
            "select case when Col1 = 1 then case when Col2 = 2 then 'a' else 'b' end else 'c' end from schema.method()";
        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();
        Assert.IsNotNull(result);
    }

    #endregion

    #region Arithmetic Expressions

    [TestMethod]
    [DataRow("select 1 + 2 from schema.method()")]
    [DataRow("select 1 - 2 from schema.method()")]
    [DataRow("select 2 * 3 from schema.method()")]
    [DataRow("select 6 / 2 from schema.method()")]
    [DataRow("select 7 % 3 from schema.method()")]
    [DataRow("select 1 + 2 * 3 from schema.method()")]
    [DataRow("select (1 + 2) * 3 from schema.method()")]
    public void HashOptional_ArithmeticExpressions_ShouldParse(string query)
    {
        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();
        Assert.IsNotNull(result);
    }

    [TestMethod]
    [DataRow("select 0xFF from schema.method()")]
    [DataRow("select 0b101 from schema.method()")]
    [DataRow("select 0o77 from schema.method()")]
    [DataRow("select 0xFF + 0b101 + 0o77 from schema.method()")]
    public void HashOptional_NumberFormats_ShouldParse(string query)
    {
        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();
        Assert.IsNotNull(result);
    }

    [TestMethod]
    [DataRow("select 5 & 3 from schema.method()")]
    [DataRow("select 5 | 3 from schema.method()")]
    [DataRow("select 5 ^ 3 from schema.method()")]
    [DataRow("select 5 << 2 from schema.method()")]
    [DataRow("select 5 >> 1 from schema.method()")]
    [DataRow("select (Flags & 0x01) = 0x01 from schema.method()")]
    [DataRow("select (Value >> 4) & 0x0F from schema.method()")]
    public void HashOptional_BitwiseOperators_ShouldParse(string query)
    {
        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();
        Assert.IsNotNull(result);
    }

    #endregion

    #region Function Calls

    [TestMethod]
    [DataRow("select Trim(Col) from schema.method()")]
    [DataRow("select Length(Col) from schema.method()")]
    [DataRow("select Substring(Col, 0, 5) from schema.method()")]
    [DataRow("select ToUpper(Col) from schema.method()")]
    [DataRow("select ToLower(Col) from schema.method()")]
    [DataRow("select Coalesce(Col1, Col2, 'default') from schema.method()")]
    public void HashOptional_FunctionCalls_ShouldParse(string query)
    {
        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();
        Assert.IsNotNull(result);
    }

    [TestMethod]
    [DataRow("select Count(*) from schema.method()")]
    [DataRow("select Count(Col) from schema.method() group by OtherCol")]
    [DataRow("select Sum(Col) from schema.method() group by OtherCol")]
    [DataRow("select Avg(Col) from schema.method() group by OtherCol")]
    [DataRow("select Min(Col) from schema.method() group by OtherCol")]
    [DataRow("select Max(Col) from schema.method() group by OtherCol")]
    public void HashOptional_AggregateFunctions_ShouldParse(string query)
    {
        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();
        Assert.IsNotNull(result);
    }

    #endregion
}
