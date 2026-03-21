using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Lexing;

namespace Musoq.Parser.Tests;

[TestClass]
public class HashOptionalSchemaParser_EdgeCaseTests
{
    [TestMethod]
    public void HashOptional_EmptyArgs_ShouldParse()
    {
        var query = "select 1 from schema.method()";
        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void HashOptional_SingleCharSchemaAndMethod_ShouldParse()
    {
        var query = "select 1 from a.b()";
        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void HashOptional_SchemaNameWithNumbers_ShouldParse()
    {
        var query = "select 1 from schema123.method456()";
        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void HashOptional_CrossApplyWithMethodCallOnAlias_ShouldParse()
    {
        var query = "select b.Value from schema.first() a cross apply a.Split(a.Text, ' ') b";
        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void HashOptional_CrossApplyWithChainedMethodCalls_ShouldParse()
    {
        var query = "select b.Value from schema.first() a cross apply a.Take(a.Skip(a.Split(a.Text, ' '), 1), 6) b";
        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void HashOptional_CrossApplyWithNestedProperty_ShouldNotInjectHash()
    {
        var query = "select c.Value from schema.thing() a cross apply a.Prop.Nested c";
        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void HashOptional_RightOuterJoin_ShouldParse()
    {
        var query = "select a.Col, b.Col from schema.first() a right outer join schema.second() b on a.Key = b.Key";
        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void HashOptional_MultipleStatements_ShouldParse()
    {
        var query = "select 1 from schema.first(); select 2 from schema.second()";
        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void HashOptional_DescWithCouple_ShouldParse()
    {
        var query =
            "table T { Id: int }; couple schema.method with table T as Source; select Id from Source()";
        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void HashOptional_CteWithCrossApply_ShouldParse()
    {
        var query = @"
            with p as (select Text from schema.first())
            select b.Value from p a cross apply a.Split(a.Text, ' ') b";
        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void HashOptional_MultipleCrossApplies_ShouldParse()
    {
        var query =
            "select c.Value from schema.first() a cross apply a.Split(a.Text, ' ') b cross apply b.ToCharArray(b.Value) c";
        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void HashOptional_CrossApplyBetweenTwoSchemas_ShouldParse()
    {
        var query = "select b.Col from schemaA.first() a cross apply schemaB.second(a.Key) b";
        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void HashOptional_NestedCteWithSetOperator_ShouldParse()
    {
        var query = @"
            with 
                cte1 as (select Name from schema.first()),
                cte2 as (select Name from schema.second()),
                cte3 as (select Name from cte1 union (Name) select Name from cte2)
            select Name from cte3";
        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void HashOptional_JoinWithSubqueryAlias_ShouldParse()
    {
        var query =
            "select a.Col, b.Col from schema.first() a inner join schema.second() b on a.Id = b.Id where a.Value > 5";
        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void HashOptional_RowNumber_ShouldParse()
    {
        var query = "select RowNumber() from schema.method()";
        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void HashOptional_RowNumberWithGroupBy_ShouldParse()
    {
        var query = "select Col, Count(Col), RowNumber() from schema.method() group by Col";
        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void HashOptional_RowNumberWithOrderBy_ShouldParse()
    {
        var query = "select Col, RowNumber() from schema.method() order by Col";
        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void HashOptional_AliasedStar_ShouldParse()
    {
        var query = "select a.* from schema.method() a";
        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void HashOptional_MultipleStars_ShouldParse()
    {
        var query = "select *, * from schema.method() a";
        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void HashOptional_StarWithExplicitColumn_ShouldParse()
    {
        var query = "select *, a.Col, Col from schema.method() a";
        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void HashOptional_CastFunction_ShouldParse()
    {
        var query = "select Cast(Col, int) from schema.method()";
        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void HashOptional_ToStringFunction_ShouldParse()
    {
        var query = "select ToString(Col) from schema.method()";
        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void HashOptional_ArrayAccess_ShouldParse()
    {
        var query = "select Col[0] from schema.method()";
        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void HashOptional_NegativeArrayAccess_ShouldParse()
    {
        var query = "select Col[-1] from schema.method()";
        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void HashOptional_MultipleSchemasInQuery_ShouldParse()
    {
        var query = "select a.Col, b.Col from schemaA.first() a inner join schemaB.second() b on a.Id = b.Id";
        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void HashOptional_BitwiseOperations_ShouldParse()
    {
        var query = "select And(Col, 1), Or(Col, 2), Xor(Col, 3), Not(Col) from schema.method()";
        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void HashOptional_NegativeNumbers_ShouldParse()
    {
        var query = "select -1, Col - 5, -Col from schema.method()";
        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void HashOptional_EscapeSequences_ShouldParse()
    {
        var query = @"select 'test\nvalue' from schema.method()";
        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void HashOptional_ConcatFunction_ShouldParse()
    {
        var query = "select Concat(Col1, Col2, Col3) from schema.method()";
        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void HashOptional_FieldLinkSyntax_ShouldParse()
    {
        var query = "select ::1, Count(::1), ::2 from schema.method() group by Col1, Col2";
        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void HashOptional_ImplicitBooleanConversion_ShouldParse()
    {
        var query = "select case when Match(Col, 'pattern') then 'yes' else 'no' end from schema.method()";
        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void HashOptional_StringOperations_ShouldParse()
    {
        var query = "select ToLower(Col), ToUpper(Col), Trim(Col), Length(Col) from schema.method()";
        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void HashOptional_DateTimeFunctions_ShouldParse()
    {
        var query = "select Year(DateCol), Month(DateCol), Day(DateCol) from schema.method()";
        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void HashOptional_NullHandling_ShouldParse()
    {
        var query = "select IsNull(Col, 'default'), IfNull(Col1, Col2) from schema.method()";
        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void HashOptional_ThreeWayJoin_ShouldParse()
    {
        var query = @"
            select a.Col, b.Col, c.Col 
            from schemaA.first() a 
            inner join schemaB.second() b on a.Id = b.Id 
            inner join schemaC.third() c on b.Id = c.Id";
        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();
        Assert.IsNotNull(result);
    }
}
