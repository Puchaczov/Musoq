using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;
using Musoq.Parser.Diagnostics;
using static Musoq.Evaluator.Tests.MusoqExceptionAssertions;

namespace Musoq.Evaluator.Tests;

public partial class MalformedQueryErrorTests
{
    #region FROM-first query errors

    [TestMethod]
    public void WhenFromFirstWithoutSelect_ShouldThrowParseError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("FROM #test.people() WHERE Age > 25"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ2001_UnexpectedToken, DiagnosticPhase.Parse, "Expected token is Select");
        AssertHasGuidance(ex);
    }

    [TestMethod]
    public void WhenFromFirstWithWhereAfterSelect_ShouldThrowParseError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("FROM #test.people() SELECT Name WHERE Age > 25"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ2001_UnexpectedToken, DiagnosticPhase.Parse, "is not expected here");
        AssertHasGuidance(ex);
    }

    [TestMethod]
    public void WhenFromFirstValid_ShouldSucceed()
    {
        var vm = CompileQuery("FROM #test.people() WHERE Age > 25 SELECT Name");
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(4, table.Count);
    }

    [TestMethod]
    public void WhenFromFirstWithGroupByBeforeWhere_ShouldThrowParseError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("FROM #test.people() GROUP BY City WHERE Age > 25 SELECT City, Count(1)"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ2001_UnexpectedToken, DiagnosticPhase.Parse, "Expected token is Select");
        AssertHasGuidance(ex);
    }

    #endregion

    #region DESC statement errors

    [TestMethod]
    public void WhenDescWithoutIdentifier_ShouldThrowParseError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("DESC"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ2001_UnexpectedToken, DiagnosticPhase.Parse, "Expected schema name");
        AssertHasGuidance(ex);
    }

    [TestMethod]
    public void WhenDescFunctionsOnNonExistentSchema_ShouldThrowError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("DESC FUNCTIONS #nonexistent"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3010_UnknownSchema, DiagnosticPhase.Bind, "Unknown schema");
        AssertHasGuidance(ex);
    }

    [TestMethod]
    public void WhenDescWithValidSchema_ShouldSucceed()
    {
        var vm = CompileQuery("DESC #test");
        var table = vm.Run(TokenSource.Token);

        Assert.IsGreaterThanOrEqualTo(0, table.Count, "DESC #test should compile and execute");
    }

    [TestMethod]
    public void WhenDescWithValidSchemaTable_ShouldSucceed()
    {
        var vm = CompileQuery("DESC #test.people");
        var table = vm.Run(TokenSource.Token);

        Assert.IsGreaterThanOrEqualTo(0, table.Count, "DESC #test.people should compile and execute");
    }

    #endregion

    #region ORDER BY edge cases

    [TestMethod]
    public void WhenOrderByPositionNumber_ShouldReportDedicatedDiagnostic()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT Name, Age FROM #test.people() ORDER BY 1"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3093_OrderByOrdinalUnsupported, DiagnosticPhase.Bind,
            "ORDER BY column position is not supported");
        AssertHasGuidance(ex);
    }

    [TestMethod]
    public void WhenOrderByNonExistentColumnAfterGroupBy_ShouldThrowError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery(
                "SELECT City, Count(1) FROM #test.people() GROUP BY City ORDER BY NonExistent"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3001_UnknownColumn, DiagnosticPhase.Bind, "Unknown column 'NonExistent'");
    }

    #endregion

    #region HAVING edge cases

    [TestMethod]
    public void WhenHavingWithoutGroupBy_ShouldThrowError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT Name FROM #test.people() HAVING Count(1) > 1"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ2001_UnexpectedToken, DiagnosticPhase.Parse, "Having is not expected");
        AssertHasGuidance(ex);
    }

    [TestMethod]
    public void WhenHavingWithNonAggregateExpression_ShouldThrowError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT City, Count(1) FROM #test.people() GROUP BY City HAVING City"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3005_TypeMismatch, DiagnosticPhase.Bind,
            "HAVING clause requires a boolean expression");
        AssertHasGuidance(ex);
    }

    #endregion

    #region DISTINCT + GROUP BY

    [TestMethod]
    public void WhenDistinctWithGroupBy_ShouldCompileSuccessfully()
    {
        var vm = CompileQuery(
            "SELECT DISTINCT City, Count(1) FROM #test.people() GROUP BY City");
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(3, table.Count);
    }

    #endregion

    #region DESC with method form

    [TestMethod]
    public void WhenDescWithMethodForm_ShouldSucceed()
    {
        var vm = CompileQuery("DESC #test.people()");
        var table = vm.Run(TokenSource.Token);

        Assert.IsGreaterThanOrEqualTo(0, table.Count, "DESC method form should compile and execute");
    }

    #endregion
}
