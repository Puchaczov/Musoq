using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator.Tests.Schema.NegativeTests;
using Musoq.Parser.Diagnostics;
using static Musoq.Evaluator.Tests.MusoqExceptionAssertions;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class ErrorMessageQualityTests : NegativeTestsBase
{
    [TestMethod]
    public void EQ001_CountStar_ShouldCompile()
    {
        var vm = CompileQuery("SELECT Count(*) FROM #test.people()");
        var table = TableMaterializationTestHelper.Materialize(vm.Run(TokenSource.Token));

        TableMaterializationTestHelper.AssertRowsInOrder(table, [5L]);
    }

    [TestMethod]
    public void EQ002_LimitInsteadOfTake_ShouldProduceError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT * FROM #test.people() LIMIT 10"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ2001_UnexpectedToken, DiagnosticPhase.Parse, "LIMIT");
        AssertHasGuidance(ex);
    }

    [TestMethod]
    public void EQ003_OffsetInsteadOfSkip_ShouldProduceError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT * FROM #test.people() ORDER BY Name OFFSET 5"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ2009_InvalidOrderByExpression, DiagnosticPhase.Parse, "OFFSET");
        AssertHasGuidance(ex);
    }

    [TestMethod]
    public void EQ004_StandardUnionWithoutColumnList_ShouldCompile()
    {
        var vm = CompileQuery("SELECT Name FROM #test.people() UNION SELECT Name FROM #test.people()");
        var table = TableMaterializationTestHelper.Materialize(vm.Run(TokenSource.Token));

        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Alice"],
            ["Bob"],
            ["Charlie"],
            ["Diana"],
            ["Eve"]);
    }

    [TestMethod]
    public void EQ005_NotEqualOperator_IsSupported()
    {
        var vm = CompileQuery("SELECT Name FROM #test.people() WHERE Age != 25");
        var table = TableMaterializationTestHelper.Materialize(vm.Run(TokenSource.Token));

        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Bob"],
            ["Charlie"],
            ["Diana"],
            ["Eve"]);
    }

    [TestMethod]
    public void EQ006_SubqueryInWhere_MultiColumnSubquery_ShouldProduceError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT * FROM #test.people() WHERE Id IN (SELECT Id, Name FROM #test.people())"));

        AssertErrorEnvelope(
            ex,
            DiagnosticCode.MQ3049_InSubqueryMultipleColumns,
            DiagnosticPhase.Bind,
            "exactly one column");
        AssertHasGuidance(ex);
    }

    [TestMethod]
    public void EQ009_ColumnCaseSensitivityMistake_ShouldProduceError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT name FROM #test.people()"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3001_UnknownColumn, DiagnosticPhase.Bind, "name");
    }

    [TestMethod]
    public void EQ010_GroupByAlias_ShouldCompileAndGroupByAlias()
    {
        var vm = CompileQuery(
            "SELECT ToUpper(City) AS UpperCity, Count(1) FROM #test.people() GROUP BY UpperCity ORDER BY UpperCity");
        var table = TableMaterializationTestHelper.Materialize(vm.Run(TokenSource.Token));

        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["BERLIN", 1L],
            ["LONDON", 2L],
            ["PARIS", 2L]);
    }

    [TestMethod]
    public void EQ011_RecursiveCte_ShouldProduceError()
    {
        var query = @"
            WITH R AS (
                SELECT Id, ManagerId FROM #test.people() WHERE ManagerId IS NULL
                UNION ALL (Id, ManagerId)
                SELECT p.Id, p.ManagerId FROM #test.people() p INNER JOIN R r ON p.ManagerId = r.Id
            )
            SELECT * FROM R r";

        var ex = Assert.Throws<MusoqQueryException>(() => CompileQuery(query));

        AssertErrorEnvelope(
            ex,
            DiagnosticCode.MQ3072_RecursiveCteRequiresKeyword,
            DiagnosticPhase.Bind,
            "WITH RECURSIVE");
        AssertHasGuidance(ex);
    }
}
