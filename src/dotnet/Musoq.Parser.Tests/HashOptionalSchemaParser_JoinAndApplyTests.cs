using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Lexing;

namespace Musoq.Parser.Tests;

[TestClass]
public class HashOptionalSchemaParser_JoinAndApplyTests
{
    #region INNER JOIN

    [TestMethod]
    [DataRow("select a.Col from schemaA.methodA() a inner join schemaB.methodB() b on a.Id = b.Id")]
    [DataRow("select a.Col, b.Col from schemaA.methodA() a inner join schemaB.methodB() b on a.Key = b.Key")]
    public void HashOptional_InnerJoin_ShouldParse(string query)
    {
        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void HashOptional_MultipleInnerJoins_ShouldParse()
    {
        var query =
            "select a.Col from schemaA.methodA() a inner join schemaB.methodB() b on a.Id = b.Id inner join schemaC.methodC() c on b.Id = c.Id";
        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void HashOptional_InnerJoinMixedWithHash_ShouldParse()
    {
        var query = "select a.Col from #schemaA.methodA() a inner join schemaB.methodB() b on a.Id = b.Id";
        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void HashOptional_InnerJoinWithCompoundCondition_ShouldParse()
    {
        var query =
            "select a.Col from schemaA.methodA() a inner join schemaB.methodB() b on a.Id = b.Id and a.Type = b.Type";
        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();
        Assert.IsNotNull(result);
    }

    #endregion

    #region LEFT/RIGHT OUTER JOIN

    [TestMethod]
    [DataRow("select a.Col from schemaA.methodA() a left join schemaB.methodB() b on a.Id = b.Id")]
    [DataRow("select a.Col from schemaA.methodA() a right join schemaB.methodB() b on a.Id = b.Id")]
    [DataRow("select a.Col from schemaA.methodA() a left outer join schemaB.methodB() b on a.Id = b.Id")]
    [DataRow("select a.Col from schemaA.methodA() a right outer join schemaB.methodB() b on a.Id = b.Id")]
    public void HashOptional_OuterJoin_ShouldParse(string query)
    {
        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void HashOptional_LeftOuterJoinMixedWithHash_ShouldParse()
    {
        var query = "select a.Col from #schemaA.methodA() a left outer join schemaB.methodB() b on a.Id = b.Id";
        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void HashOptional_LeftJoinMixedWithHash_ShouldParse()
    {
        var query = "select a.Col from #schemaA.methodA() a left join schemaB.methodB() b on a.Id = b.Id";
        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();
        Assert.IsNotNull(result);
    }

    #endregion

    #region CROSS APPLY

    [TestMethod]
    [DataRow("select a.Col, b.Value from schema.first() a cross apply schema.second(a.Key) b")]
    [DataRow("select a.Col from schema.method() a cross apply schema.nested(a.Prop) b")]
    public void HashOptional_CrossApply_ShouldParse(string query)
    {
        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void HashOptional_CrossApplyChained_ShouldParse()
    {
        var query =
            "select a.Col, b.Value, c.Data from schema.first() a cross apply schema.second(a.Key) b cross apply schema.third(b.Id) c";
        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void HashOptional_CrossApplyMixedWithHash_ShouldParse()
    {
        var query = "select a.Col, b.Value from #schema.first() a cross apply schema.second(a.Key) b";
        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void HashOptional_CrossApplyWithNestedProperty_ShouldParse()
    {
        var query = "select 1 from schema.thing() r cross apply r.Prop.Nested c";
        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();
        Assert.IsNotNull(result);
    }

    #endregion

    #region OUTER APPLY

    [TestMethod]
    [DataRow("select a.Col, b.Value from schema.first() a outer apply schema.second(a.Key) b")]
    [DataRow("select a.Col from schema.method() a outer apply schema.nested(a.Prop) b")]
    public void HashOptional_OuterApply_ShouldParse(string query)
    {
        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void HashOptional_OuterApplyMixedWithHash_ShouldParse()
    {
        var query = "select a.Col, b.Value from #schema.first() a outer apply schema.second(a.Key) b";
        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();
        Assert.IsNotNull(result);
    }

    #endregion
}
