using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;
using Musoq.Parser.Diagnostics;
using static Musoq.Evaluator.Tests.MusoqExceptionAssertions;

namespace Musoq.Evaluator.Tests;

public partial class MalformedQueryErrorTests
{
    #region RowNumber misuse

    [TestMethod]
    public void WhenRowNumberWithArguments_ShouldThrowError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT RowNumber(1) FROM #test.people()"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3087_InvalidCallableArity, DiagnosticPhase.Bind, "RowNumber");
        AssertHasGuidance(ex);
    }

    #endregion

    #region Additional semantic gaps

    [TestMethod]
    public void WhenGroupByOnNonExistentColumnWithHaving_ShouldThrowError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery(
                "SELECT Count(1) FROM #test.people() GROUP BY NonExistent HAVING Count(1) > 0"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3001_UnknownColumn, DiagnosticPhase.Bind, "Unknown column 'NonExistent'");
    }

    [TestMethod]
    public void WhenOrderByOnAliasedColumnCorrectly_ShouldSucceed()
    {
        var vm = CompileQuery(
            "SELECT Name AS PersonName FROM #test.people() ORDER BY PersonName");
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(5, table.Count);
    }

    [TestMethod]
    public void WhenMultipleAggregatesWithNonGroupedColumn_ShouldThrowError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery(
                "SELECT Name, Count(1), Sum(Age) FROM #test.people() GROUP BY City"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3012_NonAggregateInSelect, DiagnosticPhase.Bind, "must appear in the GROUP BY");
        AssertHasGuidance(ex);
    }

    [TestMethod]
    public void WhenCteWithSameNameAsSchemaSource_ShouldCompileSuccessfully()
    {
        var vm = CompileQuery(
            "WITH people AS (SELECT Name FROM #test.people()) SELECT Name FROM people");
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(5, table.Count);
    }

    [TestMethod]
    public void WhenJoinOnConditionAlwaysFalse_ShouldSucceedWithEmptyResult()
    {
        var vm = CompileQuery(
            "SELECT a.Name FROM #test.people() a INNER JOIN #test.orders() o ON 1 = 0");
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(0, table.Count);
    }

    [TestMethod]
    public void WhenLeftJoinWithNonExistentColumnInOn_ShouldThrowError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery(
                "SELECT a.Name FROM #test.people() a LEFT JOIN #test.orders() o ON a.NonExistent = o.PersonId"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3001_UnknownColumn, DiagnosticPhase.Bind, "Unknown column 'NonExistent'");
    }

    [TestMethod]
    public void WhenRightJoinWithNonExistentColumnInOn_ShouldThrowError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery(
                "SELECT a.Name FROM #test.people() a RIGHT JOIN #test.orders() o ON a.Id = o.NonExistent"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3001_UnknownColumn, DiagnosticPhase.Bind, "Unknown column 'NonExistent'");
    }

    #endregion

    #region Cross-feature edge cases

    [TestMethod]
    public void WhenCteWithGroupByAndNonAggregatedColumn_ShouldThrowError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery(
                "WITH Grouped AS (SELECT City, Name, Count(1) AS Cnt FROM #test.people() GROUP BY City) SELECT * FROM Grouped"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3012_NonAggregateInSelect, DiagnosticPhase.Bind, "must appear in the GROUP BY");
        AssertHasGuidance(ex);
    }

    [TestMethod]
    public void WhenSetOperationInsideCteWithMismatchedColumns_ShouldThrowError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery(
                "WITH Combined AS (SELECT Name FROM #test.people() UNION (Name) SELECT Name, Age FROM #test.people()) SELECT * FROM Combined"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3019_SetOperatorColumnCount, DiagnosticPhase.Bind, "same quantity of columns");
        AssertHasGuidance(ex);
    }

    [TestMethod]
    public void WhenJoinBetweenCteAndSchemaWithNonExistentColumn_ShouldThrowError()
    {
        // Known quality gap: primary error is a runtime InvalidCastException
        // but the secondary envelope correctly identifies the unknown column
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery(
                "WITH Cte AS (SELECT Name FROM #test.people()) SELECT c.Name FROM Cte c INNER JOIN #test.orders() o ON c.NonExistent = o.PersonId"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3001_UnknownColumn, DiagnosticPhase.Bind, "NonExistent");
    }

    #endregion

    #region Missing alias prefix in multi-table context

    [TestMethod]
    public void WhenFunctionCallWithoutAliasPrefixInJoin_WhenSharedMethod_ShouldAutoResolve()
    {
        var vm = CompileQuery(
            "SELECT ToUpper(Name) FROM #test.people() a INNER JOIN #test.orders() o ON a.Id = o.PersonId");
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(5, table.Count);
    }

    [TestMethod]
    public void WhenColumnWithoutAliasPrefixInJoin_WhenUnambiguous_ShouldSucceed()
    {
        var vm = CompileQuery(
            "SELECT Name FROM #test.people() a INNER JOIN #test.orders() o ON a.Id = o.PersonId");
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(5, table.Count);
    }

    #endregion
}
