using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

public sealed partial class IsDistinctFromTests
{
    private const string StringPairComparisons =
        "p.LeftValue is distinct from p.RightValue as IsDistinct, p.LeftValue is not distinct from p.RightValue as IsNotDistinct ";

    private const string StringPairsSource =
        "from values { (Case: 'null-null', LeftValue: null, RightValue: null), (Case: 'null-value', LeftValue: null, RightValue: 'x'), (Case: 'value-null', LeftValue: 'x', RightValue: null), (Case: 'same-value', LeftValue: 'x', RightValue: 'x'), (Case: 'different-value', LeftValue: 'x', RightValue: 'y') } p";

    [TestMethod]
    [TestProperty("CandidateId", "SPECSTRESS-2026-09-29-P1-0248")]
    public void OriginalNullSafeProjectionWithReservedCaseAlias_ShouldRejectDuringParsing()
    {
        const string query = "select p.Case as Case, " + StringPairComparisons + StringPairsSource;

        var exception = Assert.Throws<MusoqQueryException>(() => CreateAndRunVirtualMachine(query, EmptySources()));

        MusoqExceptionAssertions.AssertSingleError(exception, DiagnosticCode.MQ2022_InvalidAlias, DiagnosticPhase.Parse, "alias");
        MusoqExceptionAssertions.AssertHasGuidance(exception);
        Assert.AreEqual(17, exception.PrimaryEnvelope.Offset);
        Assert.AreEqual(4, exception.PrimaryEnvelope.Length);
    }

    [TestMethod]
    public void QuotingOnlyCaseOutputAlias_ShouldRejectReservedValuesField()
    {
        const string query = "select p.Case as [Case], " + StringPairComparisons + StringPairsSource;

        var exception = Assert.Throws<MusoqQueryException>(() => CreateAndRunVirtualMachine(query, EmptySources()));

        MusoqExceptionAssertions.AssertSingleError(exception, DiagnosticCode.MQ2001_UnexpectedToken, DiagnosticPhase.Parse, "field name");
        MusoqExceptionAssertions.AssertHasGuidance(exception);
        Assert.AreEqual(160, exception.PrimaryEnvelope.Offset);
        Assert.AreEqual(4, exception.PrimaryEnvelope.Length);
    }

    [TestMethod]
    public void ProjectedStringDistinctnessWithQuotedCaseIdentifiers_ShouldMatchNullSafeTruthTable()
    {
        var query = "select p.Case as [Case], " + StringPairComparisons +
                    StringPairsSource.Replace("(Case:", "([Case]:", StringComparison.Ordinal);
        using var compiled = CreateAndRunVirtualMachine(query, EmptySources());

        using var result = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(result,
            ("Case", typeof(string)), ("IsDistinct", typeof(bool)), ("IsNotDistinct", typeof(bool)));
        TableMaterializationTestHelper.AssertRowsUnordered(result,
            ["null-null", false, true],
            ["null-value", true, false],
            ["value-null", true, false],
            ["same-value", false, true],
            ["different-value", true, false]);
    }
}
