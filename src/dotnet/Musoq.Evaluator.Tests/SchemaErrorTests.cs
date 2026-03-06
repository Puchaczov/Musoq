using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator.Tests.Schema.NegativeTests;
using Musoq.Parser.Diagnostics;
using static Musoq.Evaluator.Tests.MusoqExceptionAssertions;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class SchemaErrorTests : NegativeTestsBase
{
    #region 2.4 Property Access Errors

    [TestMethod]
    public void SE031_DeepPropertyAccessWithInvalidIntermediate_ShouldThrowError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT Info.Label.Nonexistent FROM #test.nested()"));

        AssertSingleError(ex, DiagnosticCode.MQ3001_UnknownColumn, DiagnosticPhase.Bind, "Nonexistent");
    }

    #endregion

    #region 2.6 CTE Forward Reference / Scope Errors

    [TestMethod]
    public void SE050_CteForwardReference_ShouldThrowError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery(
                "WITH First AS (SELECT * FROM Second s), Second AS (SELECT Name FROM #test.people()) SELECT * FROM First f"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3003_UnknownTable, DiagnosticPhase.Bind, "Second");
    }

    #endregion

    #region 2.1 Unknown Schema / Table / Column

    [TestMethod]
    public void SE001_NonexistentSchema_ShouldThrowSchemaError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT * FROM #nonexistent.table()"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3010_UnknownSchema, DiagnosticPhase.Bind, "#nonexistent");
        AssertHasGuidance(ex);
    }

    [TestMethod]
    public void SE002_NonexistentTableInValidSchema_ShouldThrowSchemaError()
    {
        // Known quality gap: produces MQ9999 wrapping TableNotFoundException
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT * FROM #test.nonexistent()"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ9999_Unknown, DiagnosticPhase.Runtime);
    }

    [TestMethod]
    public void SE003_NonexistentColumn_ShouldThrowUnknownColumnError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT NonExistentColumn FROM #test.people()"));

        AssertSingleError(ex, DiagnosticCode.MQ3001_UnknownColumn, DiagnosticPhase.Bind, "NonExistentColumn");
    }

    [TestMethod]
    public void SE004_NonexistentColumnInWhere_ShouldThrowUnknownColumnError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT Name FROM #test.people() WHERE FakeColumn > 10"));

        AssertSingleError(ex, DiagnosticCode.MQ3001_UnknownColumn, DiagnosticPhase.Bind, "FakeColumn");
    }

    [TestMethod]
    public void SE005_NonexistentColumnInGroupBy_ShouldThrowUnknownColumnError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT Count(1) FROM #test.people() GROUP BY Nonexistent"));

        AssertSingleError(ex, DiagnosticCode.MQ3001_UnknownColumn, DiagnosticPhase.Bind, "Nonexistent");
    }

    [TestMethod]
    public void SE006_NonexistentColumnInOrderBy_ShouldThrowUnknownColumnError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT Name FROM #test.people() ORDER BY Nonexistent"));

        AssertSingleError(ex, DiagnosticCode.MQ3001_UnknownColumn, DiagnosticPhase.Bind, "Nonexistent");
    }

    [TestMethod]
    public void SE007_NonexistentColumnInHaving_ShouldThrowUnknownColumnError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT City, Count(1) FROM #test.people() GROUP BY City HAVING Sum(Nonexistent) > 100"));

        AssertSingleError(ex, DiagnosticCode.MQ3001_UnknownColumn, DiagnosticPhase.Bind, "Nonexistent");
    }

    [TestMethod]
    public void SE008_NonexistentColumnInJoinCondition_ShouldThrowError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT * FROM #test.people() p INNER JOIN #test.orders() o ON p.FakeId = o.PersonId"));

        AssertSingleError(ex, DiagnosticCode.MQ3001_UnknownColumn, DiagnosticPhase.Bind, "FakeId");
    }

    [TestMethod]
    public void SE009_NonexistentColumnInCaseExpression_ShouldThrowError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT CASE NonExistent WHEN 1 THEN 'a' ELSE 'b' END FROM #test.people()"));

        AssertSingleError(ex, DiagnosticCode.MQ3001_UnknownColumn, DiagnosticPhase.Bind, "NonExistent");
    }

    #endregion

    #region 2.2 Alias Errors

    [TestMethod]
    public void SE010_ReferenceToUndefinedAlias_ShouldThrowError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT x.Name FROM #test.people() p"));

        AssertSingleError(ex, DiagnosticCode.MQ3015_UnknownAlias, DiagnosticPhase.Bind, "x");
    }

    [TestMethod]
    public void SE013_DuplicateCteNames_ShouldThrowError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery(
                "WITH A AS (SELECT 1 AS X FROM #test.single()), A AS (SELECT 2 AS X FROM #test.single()) SELECT * FROM A a"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3021_DuplicateAlias, DiagnosticPhase.Bind);
        AssertHasGuidance(ex);
    }

    [TestMethod]
    public void SE014_DuplicateTableAliases_ShouldThrowError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT * FROM #test.people() p INNER JOIN #test.orders() p ON p.Id = p.PersonId"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3021_DuplicateAlias, DiagnosticPhase.Bind);
        AssertHasGuidance(ex);
    }

    [TestMethod]
    public void SE016_WrongAliasPrefixForColumn_ShouldThrowError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT p.Amount FROM #test.people() p INNER JOIN #test.orders() o ON p.Id = o.PersonId"));

        AssertSingleError(ex, DiagnosticCode.MQ3001_UnknownColumn, DiagnosticPhase.Bind, "Amount");
    }

    #endregion
}
