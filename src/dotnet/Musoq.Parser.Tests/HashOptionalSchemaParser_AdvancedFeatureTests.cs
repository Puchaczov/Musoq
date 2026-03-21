using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Lexing;

namespace Musoq.Parser.Tests;

[TestClass]
public class HashOptionalSchemaParser_AdvancedFeatureTests
{
    #region SET Operators (UNION, EXCEPT, INTERSECT)

    [TestMethod]
    [DataRow("select Col from schemaA.methodA() union (Col) select Col from schemaB.methodB()")]
    [DataRow("select Col from schemaA.methodA() union all (Col) select Col from schemaB.methodB()")]
    [DataRow("select Col from schemaA.methodA() except (Col) select Col from schemaB.methodB()")]
    [DataRow("select Col from schemaA.methodA() intersect (Col) select Col from schemaB.methodB()")]
    public void HashOptional_SetOperators_ShouldParse(string query)
    {
        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void HashOptional_MultipleUnions_ShouldParse()
    {
        var query = @"
            select Col from schemaA.methodA() 
            union (Col) select Col from schemaB.methodB()
            union (Col) select Col from schemaC.methodC()";
        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void HashOptional_MixedSetOperators_ShouldParse()
    {
        var query = @"
            select Col from schemaA.methodA() 
            union (Col) select Col from schemaB.methodB()
            except (Col) select Col from schemaC.methodC()
            intersect (Col) select Col from schemaD.methodD()";
        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void HashOptional_SetOperatorMixedWithHash_ShouldParse()
    {
        var query = "select Col from #schemaA.methodA() union (Col) select Col from schemaB.methodB()";
        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void HashOptional_SetOperatorWithMultipleKeys_ShouldParse()
    {
        var query =
            "select Col1, Col2 from schemaA.methodA() union (Col1, Col2) select Col1, Col2 from schemaB.methodB()";
        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();
        Assert.IsNotNull(result);
    }

    #endregion

    #region CTE (Common Table Expressions)

    [TestMethod]
    [DataRow("with cte as (select Col from schema.method()) select Col from cte")]
    [DataRow("with cte as (select * from schema.method()) select * from cte")]
    public void HashOptional_SimpleCte_ShouldParse(string query)
    {
        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void HashOptional_MultipleCtes_ShouldParse()
    {
        var query = @"
            with cte1 as (select Col1 from schemaA.methodA()),
            cte2 as (select Col2 from schemaB.methodB())
            select cte1.Col1, cte2.Col2 from cte1 inner join cte2 on 1 = 1";
        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void HashOptional_CteWithSetOperators_ShouldParse()
    {
        var query = @"
            with cte as (
                select Col from schemaA.methodA() 
                union (Col) 
                select Col from schemaB.methodB()
            ) 
            select Col from cte";
        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void HashOptional_CteWithGroupBy_ShouldParse()
    {
        var query = @"
            with cte as (
                select Col, Count(*) as Cnt from schema.method() group by Col
            ) 
            select Col, Cnt from cte";
        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void HashOptional_CteReferencingOtherCte_ShouldParse()
    {
        var query = @"
            with cte1 as (select Col from schema.method()),
            cte2 as (select Col from cte1 where Col = 'test')
            select Col from cte2";
        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void HashOptional_CteWithMixedHashSyntax_ShouldParse()
    {
        var query = @"
            with cte1 as (select Col from #schemaA.methodA()),
            cte2 as (select Col from schemaB.methodB())
            select * from cte1 inner join cte2 on cte1.Col = cte2.Col";
        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();
        Assert.IsNotNull(result);
    }

    #endregion

    #region Coupling Syntax

    [TestMethod]
    public void HashOptional_CoupleWithHashSyntax_ShouldParse()
    {
        var query = "couple #schema.method with table Test as SourceOfTestValues;";
        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void HashOptional_CoupleWithoutHashSyntax_ShouldParse()
    {
        var query = "couple schema.method with table Test as SourceOfTestValues;";
        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();
        Assert.IsNotNull(result);
    }

    [TestMethod]
    [DataRow("couple schemaA.methodA with table TestTable as Source;")]
    [DataRow("couple MySchema.MyMethod with table Data as DataSource;")]
    [DataRow("couple schema123.method456 with table Numbers as NumSource;")]
    public void HashOptional_CoupleVariations_ShouldParse(string query)
    {
        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();
        Assert.IsNotNull(result);
    }

    #endregion

    #region DESC Statement

    [TestMethod]
    [DataRow("desc #schema")]
    [DataRow("desc #schema.method")]
    [DataRow("desc #schema.method()")]
    public void HashOptional_DescStatementWithHash_ShouldParse(string query)
    {
        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();
        Assert.IsNotNull(result);
    }

    [TestMethod]
    [DataRow("desc schema")]
    [DataRow("desc schema.method")]
    [DataRow("desc schema.method()")]
    public void HashOptional_DescStatementWithoutHash_ShouldParse(string query)
    {
        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();
        Assert.IsNotNull(result);
    }

    [TestMethod]
    [DataRow("desc functions #schema")]
    [DataRow("desc functions #schema.method")]
    [DataRow("desc functions #schema.method()")]
    public void HashOptional_DescFunctionsWithHash_ShouldParse(string query)
    {
        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();
        Assert.IsNotNull(result);
    }

    [TestMethod]
    [DataRow("desc functions schema")]
    [DataRow("desc functions schema.method")]
    [DataRow("desc functions schema.method()")]
    public void HashOptional_DescFunctionsWithoutHash_ShouldParse(string query)
    {
        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();
        Assert.IsNotNull(result);
    }

    [TestMethod]
    [DataRow("desc myschema")]
    [DataRow("desc MySchema123")]
    [DataRow("desc schema_with_underscore")]
    public void HashOptional_DescSchemaVariations_ShouldParse(string query)
    {
        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();
        Assert.IsNotNull(result);
    }

    [TestMethod]
    [DataRow("desc myschema.mymethod")]
    [DataRow("desc MySchema.MyMethod")]
    [DataRow("desc schema123.method456")]
    public void HashOptional_DescMethodVariations_ShouldParse(string query)
    {
        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();
        Assert.IsNotNull(result);
    }

    [TestMethod]
    [DataRow("desc myschema.mymethod()")]
    [DataRow("desc myschema.mymethod('param')")]
    [DataRow("desc myschema.mymethod(1, 2, 'text')")]
    public void HashOptional_DescMethodWithArgsVariations_ShouldParse(string query)
    {
        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();
        Assert.IsNotNull(result);
    }

    #endregion

    #region Complex Queries

    [TestMethod]
    public void HashOptional_ComplexQueryWithAllFeatures_ShouldParse()
    {
        var query = @"
            with cte as (
                select 
                    a.Name,
                    a.Value,
                    Count(a.Name) as Cnt
                from schemaA.methodA() a 
                inner join schemaB.methodB() b on a.Id = b.Id
                where a.Active = true
                group by a.Name, a.Value
                having Count(a.Name) > 1
            )
            select 
                cte.Name, 
                cte.Value, 
                cte.Cnt,
                case when cte.Cnt > 5 then 'high' else 'low' end as Category
            from cte
            order by cte.Cnt desc
            skip 5 take 10";
        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void HashOptional_MixedHashAndNonHash_ComplexQuery_ShouldParse()
    {
        var query = @"
            with cte1 as (select Col from #schemaA.methodA()),
            cte2 as (select Col from schemaB.methodB())
            select c1.Col, c2.Col 
            from cte1 c1 
            inner join cte2 c2 on c1.Col = c2.Col
            union (Col)
            select Col, Col from #schemaC.methodC()";
        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();
        Assert.IsNotNull(result);
    }

    #endregion
}
