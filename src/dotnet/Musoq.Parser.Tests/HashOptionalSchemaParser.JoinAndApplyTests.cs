using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Lexing;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;

namespace Musoq.Parser.Tests;

[TestClass]
public class HashOptionalSchemaParserJoinAndApplyTests
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
    [DataRow("select a.Col from schemaA.methodA() a full join schemaB.methodB() b on a.Id = b.Id")]
    [DataRow("select a.Col from schemaA.methodA() a full outer join schemaB.methodB() b on a.Id = b.Id")]
    [DataRow("select a.Col from schemaA.methodA() a FULL OUTER JOIN schemaB.methodB() b ON a.Id = b.Id")]
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

    #region SEMI/ANTI/CROSS JOIN

    [TestMethod]
    [DataRow("select a.Col from schemaA.methodA() a semi join schemaB.methodB() b on a.Id = b.Id")]
    [DataRow("select a.Col from schemaA.methodA() a left semi join schemaB.methodB() b on a.Id = b.Id")]
    [DataRow("select a.Col from schemaA.methodA() a SEMI JOIN schemaB.methodB() b ON a.Id = b.Id")]
    public void HashOptional_SemiJoin_ShouldParse(string query)
    {
        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();
        Assert.IsNotNull(result);
    }

    [TestMethod]
    [DataRow("select a.Col from schemaA.methodA() a anti join schemaB.methodB() b on a.Id = b.Id")]
    [DataRow("select a.Col from schemaA.methodA() a anti semi join schemaB.methodB() b on a.Id = b.Id")]
    [DataRow("select a.Col from schemaA.methodA() a left anti semi join schemaB.methodB() b on a.Id = b.Id")]
    [DataRow("select a.Col from schemaA.methodA() a ANTI JOIN schemaB.methodB() b ON a.Id = b.Id")]
    public void HashOptional_AntiJoin_ShouldParse(string query)
    {
        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();
        Assert.IsNotNull(result);
    }

    [TestMethod]
    [DataRow("select a.Col, b.Col from schemaA.methodA() a cross join schemaB.methodB() b")]
    [DataRow("select a.Col, b.Col from schemaA.methodA() a CROSS JOIN schemaB.methodB() b")]
    public void HashOptional_CrossJoin_ShouldParse(string query)
    {
        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void HashOptional_CrossJoinThenCrossApply_ShouldParse()
    {
        var query =
            "select a.Col, b.Col, c.Value from schemaA.methodA() a cross join schemaB.methodB() b cross apply schemaC.methodC(b.Id) c";
        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void HashOptional_AsOfJoinWithTieBreak_ShouldParse()
    {
        var query =
            "select a.Col from schemaA.methodA() a asof join schemaB.methodB() b on a.Id >= b.Id tie break by b.Score desc nulls last";
        var result = ParseSingleQuery(query);
        var expressionFrom = Assert.IsInstanceOfType<ExpressionFromNode>(result.From);
        var joinNode = Assert.IsInstanceOfType<JoinNode>(expressionFrom.Expression);

        Assert.IsNotNull(joinNode.Join.TieBreak);
        Assert.AreEqual(Order.Descending, joinNode.Join.TieBreak.Order);
        Assert.AreEqual(NullOrdering.Last, joinNode.Join.TieBreak.NullOrdering);
        Assert.Contains("tie break by", joinNode.Join.ToString());
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

    [TestMethod]
    public void HashOptional_CrossApplyWithOrdinality_ShouldParse()
    {
        var query = "select b.Ordinal from schema.first() a cross apply a.Values b with ordinality";
        var result = ParseSingleQuery(query);
        var expressionFrom = Assert.IsInstanceOfType<ExpressionFromNode>(result.From);
        var applyNode = Assert.IsInstanceOfType<ApplyNode>(expressionFrom.Expression);

        Assert.IsTrue(applyNode.Apply.WithOrdinality);
        Assert.Contains("with ordinality", applyNode.Apply.ToString());
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

    [TestMethod]
    public void HashOptional_OuterApplyWithOrdinality_ShouldParse()
    {
        var query = "select b.Ordinal from schema.first() a outer apply a.Values b with ordinality";
        var result = ParseSingleQuery(query);
        var expressionFrom = Assert.IsInstanceOfType<ExpressionFromNode>(result.From);
        var applyNode = Assert.IsInstanceOfType<ApplyNode>(expressionFrom.Expression);

        Assert.IsTrue(applyNode.Apply.WithOrdinality);
        Assert.AreEqual(ApplyType.Outer, applyNode.Apply.ApplyType);
    }

    #endregion

    private static QueryNode ParseSingleQuery(string query)
    {
        var root = new Parser(new Lexer(query, true)).ComposeAll();
        var statements = Assert.IsInstanceOfType<StatementsArrayNode>(root.Expression);
        var singleSet = Assert.IsInstanceOfType<SingleSetNode>(statements.Statements[0].Node);

        return singleSet.Query;
    }
}
