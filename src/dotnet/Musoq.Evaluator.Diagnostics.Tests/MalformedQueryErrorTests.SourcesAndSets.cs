using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;
using Musoq.Parser.Diagnostics;
using static Musoq.Evaluator.Tests.MusoqExceptionAssertions;

namespace Musoq.Evaluator.Tests;

public partial class MalformedQueryErrorTests
{
    #region APPLY semantic errors

    [TestMethod]
    public void WhenCrossApplyOnNonExistentMethod_ShouldThrowError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT * FROM #test.people() a CROSS APPLY #test.nonexistent() AS t"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3085_UnknownSource, DiagnosticPhase.Bind, "nonexistent");
        AssertHasGuidance(ex);
    }

    [TestMethod]
    public void WhenOuterApplyWithoutExplicitAlias_ShouldCompileSuccessfully()
    {
        var vm = CompileQuery("SELECT * FROM #test.people() a OUTER APPLY #test.orders() AS o");
        var table = vm.Run(TokenSource.Token);

        Assert.IsGreaterThanOrEqualTo(0, table.Count, "OUTER APPLY should compile and execute");
    }

    #endregion

    #region TABLE/COUPLE semantic errors

    [TestMethod]
    public void WhenCoupleReferencesUndefinedTable_ShouldThrowError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("couple #test.people with table UndefinedTable as Source; select * from Source()"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3023_TableNotDefined, DiagnosticPhase.Bind, "'UndefinedTable'");
    }

    [TestMethod]
    public void WhenUsingCoupledAliasBeforeCouple_ShouldThrowError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("select * from Source(); table MyTable { Name: string }; couple #test.people with table MyTable as Source"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3102_InvalidStatementOrder, DiagnosticPhase.Bind, "COUPLE");
    }

    [TestMethod]
    public void WhenTableWithUnknownType_ShouldThrowError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery(
                "table MyTable { Col: banana }; couple #test.people with table MyTable as Source; select Col from Source()"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3005_TypeMismatch, DiagnosticPhase.Bind, "banana");
    }

    [TestMethod]
    public void WhenCoupleWithoutWithKeyword_ShouldThrowError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery(
                "table MyTable { Name: string }; couple #test.people table MyTable as Source; select Name from Source()"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ2001_UnexpectedToken, DiagnosticPhase.Parse, "Expected token is With but received Table");
        AssertHasGuidance(ex);
    }

    [TestMethod]
    public void WhenCoupleWithoutAsKeyword_ShouldThrowError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery(
                "table MyTable { Name: string }; couple #test.people with table MyTable Source; select Name from Source()"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ2001_UnexpectedToken, DiagnosticPhase.Parse, "Expected token is As but received Identifier");
        AssertHasGuidance(ex);
    }

    #endregion

    #region Set operation key column errors

    [TestMethod]
    public void WhenUnionWithNonExistentKeyColumn_ShouldThrowUnknownColumnError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT Name FROM #test.people() UNION (NonExistent) SELECT Name FROM #test.people()"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3001_UnknownColumn, DiagnosticPhase.Bind, "NonExistent");
        AssertHasGuidance(ex);
    }

    [TestMethod]
    public void WhenExceptWithNonExistentKeyColumn_ShouldThrowUnknownColumnError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT Name FROM #test.people() EXCEPT (NonExistent) SELECT Name FROM #test.people()"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3001_UnknownColumn, DiagnosticPhase.Bind, "NonExistent");
        AssertHasGuidance(ex);
    }

    [TestMethod]
    public void WhenIntersectWithNonExistentKeyColumn_ShouldThrowUnknownColumnError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT Name FROM #test.people() INTERSECT (NonExistent) SELECT Name FROM #test.people()"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3001_UnknownColumn, DiagnosticPhase.Bind, "NonExistent");
        AssertHasGuidance(ex);
    }

    [TestMethod]
    public void WhenUnionWithDifferentColumnCounts_ShouldThrowError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery(
                "SELECT Name, Age FROM #test.people() UNION (Name) SELECT Name FROM #test.people()"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3019_SetOperatorColumnCount, DiagnosticPhase.Bind, "same quantity of columns");
        AssertHasGuidance(ex);
    }

    #endregion

    #region Chained set operation errors

    [TestMethod]
    public void WhenChainedUnionWithMismatchedColumnCounts_ShouldThrowError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery(
                "SELECT Name FROM #test.people() UNION (Name) SELECT Name FROM #test.people() UNION (Name) SELECT Name, Age FROM #test.people()"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3019_SetOperatorColumnCount, DiagnosticPhase.Bind, "same quantity of columns");
    }

    #endregion
}
