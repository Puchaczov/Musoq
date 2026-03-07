using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator.Tests.Schema.NegativeTests;
using Musoq.Parser.Diagnostics;
using static Musoq.Evaluator.Tests.MusoqExceptionAssertions;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class ParseErrorTests : NegativeTestsBase
{
    #region 1.1 Missing Clauses / Structural Errors

    [TestMethod]
    public void PE001_SelectWithNoColumnsAndNoStar_ShouldThrowParseError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT FROM #test.people()"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ2005_InvalidSelectList, DiagnosticPhase.Parse, "SELECT list cannot be empty");
    }

    [TestMethod]
    public void PE002_FromWithNoDataSource_ShouldThrowParseError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT Name FROM"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ2001_UnexpectedToken, DiagnosticPhase.Parse, "EndOfFile");
        AssertHasGuidance(ex);
    }

    [TestMethod]
    public void PE003_DanglingCommaInSelectList_ShouldThrowParseError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT Name, FROM #test.people()"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ2001_UnexpectedToken, DiagnosticPhase.Parse, "comma");
        AssertHasGuidance(ex);
    }

    [TestMethod]
    public void PE005_WhereWithoutCondition_ShouldThrowParseError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT Name FROM #test.people() WHERE"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ2001_UnexpectedToken, DiagnosticPhase.Parse, "EndOfFile");
        AssertHasGuidance(ex);
    }

    [TestMethod]
    public void PE006_GroupByWithNoColumns_ShouldThrowParseError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT Count(1) FROM #test.people() GROUP BY"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ2001_UnexpectedToken, DiagnosticPhase.Parse, "Group by clause does not have any fields");
        AssertHasGuidance(ex);
    }

    [TestMethod]
    public void PE007_OrderByWithNoColumns_ShouldThrowParseError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT Name FROM #test.people() ORDER BY"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ2001_UnexpectedToken, DiagnosticPhase.Parse, "EndOfFile");
        AssertHasGuidance(ex);
    }

    [TestMethod]
    public void PE009_TakeWithNoValue_ShouldThrowParseError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT Name FROM #test.people() TAKE"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ2001_UnexpectedToken, DiagnosticPhase.Parse, "Integer");
        AssertHasGuidance(ex);
    }

    [TestMethod]
    public void PE010_SkipWithNoValue_ShouldThrowParseError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT Name FROM #test.people() SKIP"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ2001_UnexpectedToken, DiagnosticPhase.Parse, "Integer");
        AssertHasGuidance(ex);
    }

    [TestMethod]
    public void PE011_TakeWithNonInteger_ShouldThrowParseError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT Name FROM #test.people() TAKE 'five'"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ2001_UnexpectedToken, DiagnosticPhase.Parse, "Integer");
        AssertHasGuidance(ex);
    }

    #endregion

    #region 1.2 Clause Ordering Errors

    [TestMethod]
    public void PE020_WhereAfterGroupBy_ShouldThrowParseError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT City, Count(1) FROM #test.people() GROUP BY City WHERE Age > 10"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ2001_UnexpectedToken, DiagnosticPhase.Parse, "not expected");
        AssertHasGuidance(ex);
    }

    [TestMethod]
    public void PE021_GroupByAfterOrderBy_ShouldThrowParseError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT City, Count(1) FROM #test.people() ORDER BY City GROUP BY City"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ2001_UnexpectedToken, DiagnosticPhase.Parse, "GroupBy");
        AssertHasGuidance(ex);
    }

    [TestMethod]
    public void PE025_DuplicateWhereClause_ShouldThrowParseError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT Name FROM #test.people() WHERE Age > 10 WHERE City = 'London'"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ2001_UnexpectedToken, DiagnosticPhase.Parse, "Where");
        AssertHasGuidance(ex);
    }

    #endregion

    #region 1.3 Expression Syntax Errors

    [TestMethod]
    public void PE030_UnclosedParenthesis_ShouldThrowParseError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT (Name FROM #test.people()"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ2001_UnexpectedToken, DiagnosticPhase.Parse, "RightParenthesis");
        AssertHasGuidance(ex);
    }

    [TestMethod]
    public void PE031_ExtraClosingParenthesis_ShouldThrowParseError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT Name) FROM #test.people()"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ2001_UnexpectedToken, DiagnosticPhase.Parse, "RightParenthesis");
        AssertHasGuidance(ex);
    }

    [TestMethod]
    public void PE033_DoubleOperator_ShouldThrowError()
    {
        // Known quality gap: produces MQ9999 CompilationException instead of a proper type/parse error
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT * FROM #test.people() WHERE Age >> 10"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ9999_Unknown, DiagnosticPhase.Runtime);
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
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT * FROM #test.people() WHERE Name LIKE"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ2001_UnexpectedToken, DiagnosticPhase.Parse, "EndOfFile");
        AssertHasGuidance(ex);
    }

    [TestMethod]
    public void PE037_RlikeWithNoPattern_ShouldThrowParseError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT * FROM #test.people() WHERE Name RLIKE"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ2001_UnexpectedToken, DiagnosticPhase.Parse, "EndOfFile");
        AssertHasGuidance(ex);
    }

    [TestMethod]
    public void PE040_EmptyCaseExpression_ShouldThrowParseError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT CASE END FROM #test.people()"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ2001_UnexpectedToken, DiagnosticPhase.Parse);
        AssertHasGuidance(ex);
    }

    [TestMethod]
    public void PE041_CaseWhenWithoutThen_ShouldThrowParseError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT CASE WHEN Age > 10 ELSE 'old' END FROM #test.people()"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ2001_UnexpectedToken, DiagnosticPhase.Parse, "Then");
        AssertHasGuidance(ex);
    }

    [TestMethod]
    public void PE042_CaseWithoutEnd_ShouldThrowParseError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT CASE WHEN Age > 10 THEN 'old' FROM #test.people()"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ2001_UnexpectedToken, DiagnosticPhase.Parse, "Else");
        AssertHasGuidance(ex);
    }

    [TestMethod]
    public void PE043_TrailingCommaInSelectList_ShouldThrowParseError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT Name, Age, FROM #test.people()"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ2001_UnexpectedToken, DiagnosticPhase.Parse, "comma");
        AssertHasGuidance(ex);
    }

    [TestMethod]
    public void PE044_DoubleCommaInSelectList_ShouldThrowParseError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT Name,, Age FROM #test.people()"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ2001_UnexpectedToken, DiagnosticPhase.Parse, "Comma");
        AssertHasGuidance(ex);
    }

    #endregion

    #region 1.4 Schema Reference Syntax Errors

    [TestMethod]
    public void PE053_MissingParenthesesOnSchemaReference_ShouldThrowParseError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT * FROM #test.people"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ2001_UnexpectedToken, DiagnosticPhase.Parse);
        AssertHasGuidance(ex);
    }

    [TestMethod]
    public void PE054_UnclosedParenthesesOnSchemaReference_ShouldThrowParseError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT * FROM #test.people("));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ2001_UnexpectedToken, DiagnosticPhase.Parse, "EndOfFile");
        AssertHasGuidance(ex);
    }

    #endregion

    #region 1.5 CTE Syntax Errors

    [TestMethod]
    public void PE060_CteWithoutAsKeyword_ShouldThrowParseError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("WITH MyData (SELECT Name FROM #test.people()) SELECT * FROM MyData m"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ2001_UnexpectedToken, DiagnosticPhase.Parse, "As");
        AssertHasGuidance(ex);
    }

    [TestMethod]
    public void PE061_CteWithoutParenthesesAroundQuery_ShouldThrowParseError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("WITH MyData AS SELECT Name FROM #test.people() SELECT * FROM MyData m"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ2001_UnexpectedToken, DiagnosticPhase.Parse, "LeftParenthesis");
        AssertHasGuidance(ex);
    }

    [TestMethod]
    public void PE062_CteWithUnclosedParenthesis_ShouldThrowParseError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("WITH MyData AS (SELECT Name FROM #test.people() SELECT * FROM MyData m"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ2001_UnexpectedToken, DiagnosticPhase.Parse, "RightParenthesis");
        AssertHasGuidance(ex);
    }

    [TestMethod]
    public void PE064_CteMissingBodyQuery_ShouldThrowParseError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("WITH MyData AS () SELECT * FROM MyData m"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ2001_UnexpectedToken, DiagnosticPhase.Parse);
        AssertHasGuidance(ex);
    }

    [TestMethod]
    public void PE065_TrailingCommaAfterLastCte_ShouldThrowParseError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("WITH A AS (SELECT 1 AS X FROM #test.single()), SELECT * FROM A a"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ2001_UnexpectedToken, DiagnosticPhase.Parse);
        AssertHasGuidance(ex);
    }

    #endregion

    #region 1.6 JOIN / APPLY Syntax Errors

    [TestMethod]
    public void PE070_JoinWithoutOnClause_ShouldThrowParseError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT * FROM #test.people() p INNER JOIN #test.orders() o"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ2001_UnexpectedToken, DiagnosticPhase.Parse, "On");
        AssertHasGuidance(ex);
    }

    [TestMethod]
    public void PE072_IncompleteJoinKeyword_ShouldThrowParseError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT * FROM #test.people() p LEFT #test.orders() o ON p.Id = o.PersonId"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ2001_UnexpectedToken, DiagnosticPhase.Parse, "not expected");
        AssertHasGuidance(ex);
    }

    #endregion

    #region 1.7 Set Operation Syntax Errors

    [TestMethod]
    public void PE080_UnionWithoutColumnList_ShouldThrowError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT Name FROM #test.people() UNION SELECT Name FROM #test.people()"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3031_SetOperatorMissingKeys, DiagnosticPhase.Bind, "Union");
        AssertHasGuidance(ex);
    }

    [TestMethod]
    public void PE081_UnionWithEmptyColumnList_ShouldThrowError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT Name FROM #test.people() UNION () SELECT Name FROM #test.people()"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3031_SetOperatorMissingKeys, DiagnosticPhase.Bind, "Union");
        AssertHasGuidance(ex);
    }

    [TestMethod]
    public void PE082_ExceptWithoutSecondQuery_ShouldThrowParseError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT Name FROM #test.people() EXCEPT (Name)"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ2001_UnexpectedToken, DiagnosticPhase.Parse);
        AssertHasGuidance(ex);
    }

    [TestMethod]
    public void PE083_IntersectWithoutColumnList_ShouldThrowError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT Name FROM #test.people() INTERSECT SELECT Name FROM #test.people()"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3031_SetOperatorMissingKeys, DiagnosticPhase.Bind, "Intersect");
        AssertHasGuidance(ex);
    }

    #endregion
}
