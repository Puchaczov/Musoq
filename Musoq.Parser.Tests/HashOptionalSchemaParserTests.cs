using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Lexing;

namespace Musoq.Parser.Tests;

/// <summary>
/// Comprehensive parser tests for hash-optional schema syntax (from schema.method() without # prefix).
/// These tests cover all SQL syntax variations to ensure the parser correctly handles
/// both hash and hash-optional schema references.
/// </summary>
[TestClass]
public class HashOptionalSchemaParserTests
{
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
        var query = "select a.Col from schemaA.methodA() a inner join schemaB.methodB() b on a.Id = b.Id inner join schemaC.methodC() c on b.Id = c.Id";
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
        var query = "select a.Col from schemaA.methodA() a inner join schemaB.methodB() b on a.Id = b.Id and a.Type = b.Type";
        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();
        Assert.IsNotNull(result);
    }
    
    #endregion
    
    #region LEFT/RIGHT OUTER JOIN
    
    [TestMethod]
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
        var query = "select a.Col, b.Value, c.Data from schema.first() a cross apply schema.second(a.Key) b cross apply schema.third(b.Id) c";
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
        var query = "select Col1, Col2 from schemaA.methodA() union (Col1, Col2) select Col1, Col2 from schemaB.methodB()";
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
        var query = "select case when Col = 1 then 'one' when Col = 2 then 'two' when Col = 3 then 'three' else 'other' end from schema.method()";
        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();
        Assert.IsNotNull(result);
    }
    
    [TestMethod]
    public void HashOptional_NestedCaseWhen_ShouldParse()
    {
        var query = "select case when Col1 = 1 then case when Col2 = 2 then 'a' else 'b' end else 'c' end from schema.method()";
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
    
    #region Coupling Syntax
    
    // Note: Coupling syntax still requires the hash prefix (#) for schema names.
    // The hash-optional syntax only applies to the FROM clause in SELECT statements.
    
    [TestMethod]
    public void HashOptional_CoupleWithHashSyntax_ShouldParse()
    {
        var query = "couple #schema.method with table Test as SourceOfTestValues;";
        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();
        Assert.IsNotNull(result);
    }
    
    #endregion
    
    #region DESC Statement
    
    // Note: DESC syntax still requires the hash prefix (#) for schema names.
    // The hash-optional syntax only applies to the FROM clause in SELECT statements.
    
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
    
    #region Edge Cases
    
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
    
    #endregion
}
