using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Tests.Schema.NegativeTests;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class ParseErrorTests : NegativeTestsBase
{
    #region 1.1 Missing Clauses / Structural Errors

    [TestMethod]
    public void PE001_SelectWithNoColumnsAndNoStar_ShouldThrowParseError()
    {
        Assert.Throws<AstValidationException>(() =>
            CompileQuery("SELECT FROM #test.people()"));
    }

    [TestMethod]
    public void PE002_FromWithNoDataSource_ShouldThrowParseError()
    {
        Assert.Throws<AstValidationException>(() =>
            CompileQuery("SELECT Name FROM"));
    }

    [TestMethod]
    public void PE003_DanglingCommaInSelectList_ShouldThrowParseError()
    {
        Assert.Throws<AstValidationException>(() =>
            CompileQuery("SELECT Name, FROM #test.people()"));
    }

    [TestMethod]
    public void PE005_WhereWithoutCondition_ShouldThrowParseError()
    {
        Assert.Throws<AstValidationException>(() =>
            CompileQuery("SELECT Name FROM #test.people() WHERE"));
    }

    [TestMethod]
    public void PE006_GroupByWithNoColumns_ShouldThrowParseError()
    {
        Assert.Throws<AstValidationException>(() =>
            CompileQuery("SELECT Count(1) FROM #test.people() GROUP BY"));
    }

    [TestMethod]
    public void PE007_OrderByWithNoColumns_ShouldThrowParseError()
    {
        Assert.Throws<AstValidationException>(() =>
            CompileQuery("SELECT Name FROM #test.people() ORDER BY"));
    }

    [TestMethod]
    public void PE009_TakeWithNoValue_ShouldThrowParseError()
    {
        Assert.Throws<AstValidationException>(() =>
            CompileQuery("SELECT Name FROM #test.people() TAKE"));
    }

    [TestMethod]
    public void PE010_SkipWithNoValue_ShouldThrowParseError()
    {
        Assert.Throws<AstValidationException>(() =>
            CompileQuery("SELECT Name FROM #test.people() SKIP"));
    }

    [TestMethod]
    public void PE011_TakeWithNonInteger_ShouldThrowParseError()
    {
        Assert.Throws<AstValidationException>(() =>
            CompileQuery("SELECT Name FROM #test.people() TAKE 'five'"));
    }

    #endregion

    #region 1.2 Clause Ordering Errors

    [TestMethod]
    public void PE020_WhereAfterGroupBy_ShouldThrowParseError()
    {
        Assert.Throws<AstValidationException>(() =>
            CompileQuery("SELECT City, Count(1) FROM #test.people() GROUP BY City WHERE Age > 10"));
    }

    [TestMethod]
    public void PE021_GroupByAfterOrderBy_ShouldThrowParseError()
    {
        Assert.Throws<AstValidationException>(() =>
            CompileQuery("SELECT City, Count(1) FROM #test.people() ORDER BY City GROUP BY City"));
    }

    [TestMethod]
    public void PE025_DuplicateWhereClause_ShouldThrowParseError()
    {
        Assert.Throws<AstValidationException>(() =>
            CompileQuery("SELECT Name FROM #test.people() WHERE Age > 10 WHERE City = 'London'"));
    }

    #endregion

    #region 1.3 Expression Syntax Errors

    [TestMethod]
    public void PE030_UnclosedParenthesis_ShouldThrowParseError()
    {
        Assert.Throws<AstValidationException>(() =>
            CompileQuery("SELECT (Name FROM #test.people()"));
    }

    [TestMethod]
    public void PE031_ExtraClosingParenthesis_ShouldThrowParseError()
    {
        Assert.Throws<AstValidationException>(() =>
            CompileQuery("SELECT Name) FROM #test.people()"));
    }

    [TestMethod]
    public void PE033_DoubleOperator_ShouldThrowError()
    {
        Assert.Throws<CompilationException>(() =>
            CompileQuery("SELECT * FROM #test.people() WHERE Age >> 10"));
    }

    [TestMethod]
    public void PE035_EmptyInList_ShouldCompileWithoutError()
    {
        var vm = CompileQuery("SELECT * FROM #test.people() WHERE City IN ()");
        Assert.IsNotNull(vm, "Empty IN () compiles successfully in Musoq.");
    }

    [TestMethod]
    public void PE036_LikeWithNoPattern_ShouldThrowParseError()
    {
        Assert.Throws<AstValidationException>(() =>
            CompileQuery("SELECT * FROM #test.people() WHERE Name LIKE"));
    }

    [TestMethod]
    public void PE037_RlikeWithNoPattern_ShouldThrowParseError()
    {
        Assert.Throws<AstValidationException>(() =>
            CompileQuery("SELECT * FROM #test.people() WHERE Name RLIKE"));
    }

    [TestMethod]
    public void PE040_EmptyCaseExpression_ShouldThrowParseError()
    {
        Assert.Throws<AstValidationException>(() =>
            CompileQuery("SELECT CASE END FROM #test.people()"));
    }

    [TestMethod]
    public void PE041_CaseWhenWithoutThen_ShouldThrowParseError()
    {
        Assert.Throws<AstValidationException>(() =>
            CompileQuery("SELECT CASE WHEN Age > 10 ELSE 'old' END FROM #test.people()"));
    }

    [TestMethod]
    public void PE042_CaseWithoutEnd_ShouldThrowParseError()
    {
        Assert.Throws<AstValidationException>(() =>
            CompileQuery("SELECT CASE WHEN Age > 10 THEN 'old' FROM #test.people()"));
    }

    [TestMethod]
    public void PE043_TrailingCommaInSelectList_ShouldThrowParseError()
    {
        Assert.Throws<AstValidationException>(() =>
            CompileQuery("SELECT Name, Age, FROM #test.people()"));
    }

    [TestMethod]
    public void PE044_DoubleCommaInSelectList_ShouldThrowParseError()
    {
        Assert.Throws<AstValidationException>(() =>
            CompileQuery("SELECT Name,, Age FROM #test.people()"));
    }

    #endregion

    #region 1.4 Schema Reference Syntax Errors

    [TestMethod]
    public void PE053_MissingParenthesesOnSchemaReference_ShouldThrowParseError()
    {
        Assert.Throws<AstValidationException>(() =>
            CompileQuery("SELECT * FROM #test.people"));
    }

    [TestMethod]
    public void PE054_UnclosedParenthesesOnSchemaReference_ShouldThrowParseError()
    {
        Assert.Throws<AstValidationException>(() =>
            CompileQuery("SELECT * FROM #test.people("));
    }

    #endregion

    #region 1.5 CTE Syntax Errors

    [TestMethod]
    public void PE060_CteWithoutAsKeyword_ShouldThrowParseError()
    {
        Assert.Throws<AstValidationException>(() =>
            CompileQuery("WITH MyData (SELECT Name FROM #test.people()) SELECT * FROM MyData m"));
    }

    [TestMethod]
    public void PE061_CteWithoutParenthesesAroundQuery_ShouldThrowParseError()
    {
        Assert.Throws<AstValidationException>(() =>
            CompileQuery("WITH MyData AS SELECT Name FROM #test.people() SELECT * FROM MyData m"));
    }

    [TestMethod]
    public void PE062_CteWithUnclosedParenthesis_ShouldThrowParseError()
    {
        Assert.Throws<AstValidationException>(() =>
            CompileQuery("WITH MyData AS (SELECT Name FROM #test.people() SELECT * FROM MyData m"));
    }

    [TestMethod]
    public void PE064_CteMissingBodyQuery_ShouldThrowParseError()
    {
        Assert.Throws<AstValidationException>(() =>
            CompileQuery("WITH MyData AS () SELECT * FROM MyData m"));
    }

    [TestMethod]
    public void PE065_TrailingCommaAfterLastCte_ShouldThrowParseError()
    {
        Assert.Throws<AstValidationException>(() =>
            CompileQuery("WITH A AS (SELECT 1 AS X FROM #test.single()), SELECT * FROM A a"));
    }

    #endregion

    #region 1.6 JOIN / APPLY Syntax Errors

    [TestMethod]
    public void PE070_JoinWithoutOnClause_ShouldThrowParseError()
    {
        Assert.Throws<AstValidationException>(() =>
            CompileQuery("SELECT * FROM #test.people() p INNER JOIN #test.orders() o"));
    }

    [TestMethod]
    public void PE072_IncompleteJoinKeyword_ShouldThrowParseError()
    {
        Assert.Throws<AstValidationException>(() =>
            CompileQuery("SELECT * FROM #test.people() p LEFT #test.orders() o ON p.Id = o.PersonId"));
    }

    #endregion

    #region 1.7 Set Operation Syntax Errors

    [TestMethod]
    public void PE080_UnionWithoutColumnList_ShouldThrowError()
    {
        Assert.Throws<SetOperatorMustHaveKeyColumnsException>(() =>
            CompileQuery("SELECT Name FROM #test.people() UNION SELECT Name FROM #test.people()"));
    }

    [TestMethod]
    public void PE081_UnionWithEmptyColumnList_ShouldThrowError()
    {
        Assert.Throws<SetOperatorMustHaveKeyColumnsException>(() =>
            CompileQuery("SELECT Name FROM #test.people() UNION () SELECT Name FROM #test.people()"));
    }

    [TestMethod]
    public void PE082_ExceptWithoutSecondQuery_ShouldThrowParseError()
    {
        Assert.Throws<AstValidationException>(() =>
            CompileQuery("SELECT Name FROM #test.people() EXCEPT (Name)"));
    }

    [TestMethod]
    public void PE083_IntersectWithoutColumnList_ShouldThrowError()
    {
        Assert.Throws<SetOperatorMustHaveKeyColumnsException>(() =>
            CompileQuery("SELECT Name FROM #test.people() INTERSECT SELECT Name FROM #test.people()"));
    }

    #endregion
}
