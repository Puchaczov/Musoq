using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator.Tests.Schema.Generic;
using Musoq.Parser.Diagnostics;
using static Musoq.Evaluator.Tests.MusoqExceptionAssertions;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class DiagnosticREC101EnumContextTests : GenericEntityTestBase
{
    private const string EnumPrefix =
        "enum JobStatus : short { Queued = 10, Running = 20, Finished = 30 };";

    private static readonly REC101EnumEntity[] Rows =
    [
        new(REC101PrimaryStatus.Running, REC101SecondaryStatus.Running, "Running", new object(), 20)
    ];

    public static IEnumerable<object[]> ContextualMemberCases()
    {
        yield return new object[]
        {
            "REC-101-CTX01",
            EnumPrefix + "select e.Status from #schema.first() e where 'running' = e.Status",
            "'running'",
            DiagnosticCode.MQ3108_UnknownEnumMember,
            "Core Spec - Enum Types"
        };
        yield return new object[]
        {
            "REC-101-CTX02",
            EnumPrefix + "select e.Status from #schema.first() e where e.Status != 'RUNNING'",
            "'RUNNING'",
            DiagnosticCode.MQ3108_UnknownEnumMember,
            "Core Spec - Enum Types"
        };
        yield return new object[]
        {
            "REC-101-CTX03",
            EnumPrefix + "select e.Status from #schema.first() e where e.Status is distinct from 'running'",
            "'running'",
            DiagnosticCode.MQ3108_UnknownEnumMember,
            "Core Spec - Enum Types"
        };
        yield return new object[]
        {
            "REC-101-CTX04",
            EnumPrefix + "select e.Status from #schema.first() e where e.Status is not distinct from 'RUNNING'",
            "'RUNNING'",
            DiagnosticCode.MQ3108_UnknownEnumMember,
            "Core Spec - Enum Types"
        };
        yield return new object[]
        {
            "REC-101-CTX05",
            EnumPrefix + "select e.Status from #schema.first() e where e.Status in ('Running', 'running')",
            "'running'",
            DiagnosticCode.MQ3108_UnknownEnumMember,
            "Core Spec - Enum Types"
        };
        yield return new object[]
        {
            "REC-101-CTX06",
            EnumPrefix + "select e.Status from #schema.first() e where e.Status not in ('Finished', 'finished')",
            "'finished'",
            DiagnosticCode.MQ3108_UnknownEnumMember,
            "Core Spec - Enum Types"
        };
    }

    public static IEnumerable<object[]> NonCoerciveCases()
    {
        yield return new object[]
        {
            "REC-101-NC01",
            EnumPrefix + "select Running from #schema.first() e",
            "Running",
            DiagnosticCode.MQ3001_UnknownColumn,
            "Core Spec - Column References"
        };
        yield return new object[]
        {
            "REC-101-NC02",
            EnumPrefix + "select JobStatus.Running from #schema.first() e",
            "JobStatus",
            DiagnosticCode.MQ3110_UnsupportedEnumOperator,
            "Core Spec - Enum Types"
        };
        yield return new object[]
        {
            "REC-101-NC03",
            EnumPrefix + "select e.Status from #schema.first() e where e.Status = 20",
            "20",
            DiagnosticCode.MQ3110_UnsupportedEnumOperator,
            "Core Spec - Enum Types"
        };
        yield return new object[]
        {
            "REC-101-NC04",
            EnumPrefix + "select e.Status from #schema.first() e where e.Status = e.Label",
            "Label",
            DiagnosticCode.MQ3110_UnsupportedEnumOperator,
            "Core Spec - Enum Types"
        };
        yield return new object[]
        {
            "REC-101-NC05",
            EnumPrefix + "select e.Status from #schema.first() e where e.Status = e.Payload",
            "Payload",
            DiagnosticCode.MQ3110_UnsupportedEnumOperator,
            "Core Spec - Enum Types"
        };
        yield return new object[]
        {
            "REC-101-NC06",
            EnumPrefix + "select e.Status::JobStatus as Value from #schema.first() e",
            "Status::JobStatus",
            DiagnosticCode.MQ3110_UnsupportedEnumOperator,
            "Core Spec - Enum Types"
        };
        yield return new object[]
        {
            "REC-101-NC07",
            EnumPrefix + "select e.Number::JobStatus as Value from #schema.first() e",
            "Number::JobStatus",
            DiagnosticCode.MQ3110_UnsupportedEnumOperator,
            "Core Spec - Enum Types"
        };
        yield return new object[]
        {
            "REC-101-NC08",
            EnumPrefix + "select e.Status from #schema.first() e where e.Status = true",
            "true",
            DiagnosticCode.MQ3110_UnsupportedEnumOperator,
            "Core Spec - Enum Types"
        };
    }

    public static IEnumerable<object[]> ContextlessLiteralCases()
    {
        yield return new object[]
        {
            "REC-101-ORD01",
            EnumPrefix + "select 'Queued' as Value from #schema.first() e"
        };
        yield return new object[]
        {
            "REC-101-ORD02",
            EnumPrefix + "select 1 from #schema.first() e where 'Status' = 'Running'"
        };
        yield return new object[]
        {
            "REC-101-ORD03",
            EnumPrefix + "select 1 from #schema.first() e where 'Running' in ('Queued', 'Running')"
        };
        yield return new object[]
        {
            "REC-101-ORD04",
            EnumPrefix + "select case when true then 'Running' else 'Queued' end as Value from #schema.first() e"
        };
    }

    public static IEnumerable<object[]> ExplicitBridgeCases()
    {
        yield return new object[]
        {
            "REC-101-BRIDGE01",
            EnumPrefix + "select EnumValue(e.Status) as Value from #schema.first() e",
            true
        };
        yield return new object[]
        {
            "REC-101-BRIDGE02",
            EnumPrefix + "select EnumName(e.Status) as Value from #schema.first() e",
            true
        };
        yield return new object[]
        {
            "REC-101-BRIDGE03",
            EnumPrefix + "select EnumValue(e.Status) as Value, EnumName(e.Status) as Name from #schema.first() e",
            true
        };
        yield return new object[]
        {
            "REC-101-BRIDGE04",
            EnumPrefix + "select e.Status from #schema.first() e where EnumValue(e.Status) = 20",
            true
        };
        yield return new object[]
        {
            "REC-101-BRIDGE05",
            EnumPrefix + "select e.Status from #schema.first() e where e.Status is distinct from e.Other",
            false
        };
        yield return new object[]
        {
            "REC-101-BRIDGE06",
            EnumPrefix + "select case when true then e.Status else e.Other end as Value from #schema.first() e",
            false
        };
    }

    [TestMethod]
    [DynamicData(nameof(ContextualMemberCases))]
    public void ContextualEnumMembers_ShouldRejectUnknownAndMisCasedLiterals(
        string caseId,
        string query,
        string expectedSpanText,
        DiagnosticCode expectedCode,
        string expectedDocsReference)
    {
        AssertSemanticDiagnostic(caseId, query, expectedSpanText, expectedCode, expectedDocsReference);
    }

    [TestMethod]
    [DynamicData(nameof(NonCoerciveCases))]
    public void NonCoerciveEnumExpressions_ShouldReportStableDiagnostics(
        string caseId,
        string query,
        string expectedSpanText,
        DiagnosticCode expectedCode,
        string expectedDocsReference)
    {
        AssertSemanticDiagnostic(caseId, query, expectedSpanText, expectedCode, expectedDocsReference);
    }

    [TestMethod]
    [DynamicData(nameof(ContextlessLiteralCases))]
    public void ContextlessQuotedLiterals_ShouldRemainOrdinaryStrings(string caseId, string query)
    {
        Assert.IsFalse(string.IsNullOrWhiteSpace(caseId));
        using var compiledQuery = CreateAndRunVirtualMachine(query, Rows);
        using var table = compiledQuery.Run(TestContext.CancellationToken);
        Assert.IsNotNull(table);
    }

    [TestMethod]
    [DynamicData(nameof(ExplicitBridgeCases))]
    public void ExplicitEnumHelpers_ShouldBeTheOnlyNumericOrTextualBridge(
        string caseId,
        string query,
        bool shouldRun)
    {
        Assert.IsFalse(string.IsNullOrWhiteSpace(caseId));

        if (shouldRun)
        {
            using var compiledQuery = CreateAndRunVirtualMachine(query, Rows);
            using var table = compiledQuery.Run(TestContext.CancellationToken);
            Assert.IsNotNull(table);
            return;
        }

        var exception = Assert.Throws<MusoqQueryException>(() => CreateAndRunVirtualMachine(query, Rows));
        AssertSingleError(exception, DiagnosticCode.MQ3109_EnumIdentityMismatch, DiagnosticPhase.Bind);
        AssertHasGuidance(exception);
        Assert.IsTrue(exception.PrimaryEnvelope.SuggestedFixes.Any(fix =>
            fix.Contains("EnumValue", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void ExactContextualMembers_ShouldBindInEqualityAndMembership()
    {
        using var compiledQuery = CreateAndRunVirtualMachine(
            EnumPrefix +
            "select e.Status from #schema.first() e " +
            "where 'Running' = e.Status and e.Status in ('Queued', 'Running')",
            Rows);
        using var table = compiledQuery.Run(TestContext.CancellationToken);

        Assert.HasCount(1, table);
    }

    [TestMethod]
    public void EnumHelpers_ShouldExposeExplicitBridges()
    {
        using var compiledQuery = CreateAndRunVirtualMachine(
            EnumPrefix +
            "select EnumValue(e.Status) as NumericValue, EnumName(e.Status) as TextValue " +
            "from #schema.first() e",
            Rows);
        using var table = compiledQuery.Run(TestContext.CancellationToken);

        Assert.HasCount(1, table);
        CollectionAssert.AreEqual(new object?[] { (short)20, "Running" }, table[0].Values);
    }

    [TestMethod]
    public void CrossIdentityFailure_ShouldNotSuggestChangingTheMemberCase()
    {
        var exception = Assert.Throws<MusoqQueryException>(() => CreateAndRunVirtualMachine(
            EnumPrefix + "select e.Status from #schema.first() e where e.Status = e.Other",
            Rows));

        AssertSingleError(exception, DiagnosticCode.MQ3109_EnumIdentityMismatch, DiagnosticPhase.Bind);
        Assert.IsTrue(exception.PrimaryEnvelope.SuggestedFixes.All(fix =>
            !fix.Contains("case", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void EnumCastFailure_ShouldNotSuggestAContextualQuotedMember()
    {
        var exception = Assert.Throws<MusoqQueryException>(() => CreateAndRunVirtualMachine(
            EnumPrefix + "select e.Status::JobStatus from #schema.first() e",
            Rows));

        AssertSingleError(exception, DiagnosticCode.MQ3110_UnsupportedEnumOperator, DiagnosticPhase.Bind);
        Assert.IsTrue(exception.PrimaryEnvelope.SuggestedFixes.All(fix =>
            !fix.Contains("quoted", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void CandidateMatrix_ShouldContainTwentyFourRegisteredCasesAcrossFourFamilies()
    {
        var ids = ContextualMemberCases()
            .Concat(NonCoerciveCases())
            .Concat(ContextlessLiteralCases())
            .Concat(ExplicitBridgeCases())
            .Select(static values => (string)values[0])
            .ToArray();

        Assert.HasCount(24, ids);
        Assert.AreEqual(ids.Length, ids.Distinct(StringComparer.Ordinal).Count());
        Assert.AreEqual(6, ids.Count(static id => id.Contains("-CTX", StringComparison.Ordinal)));
        Assert.AreEqual(8, ids.Count(static id => id.Contains("-NC", StringComparison.Ordinal)));
        Assert.AreEqual(4, ids.Count(static id => id.Contains("-ORD", StringComparison.Ordinal)));
        Assert.AreEqual(6, ids.Count(static id => id.Contains("-BRIDGE", StringComparison.Ordinal)));
    }

    private void AssertSemanticDiagnostic(
        string caseId,
        string query,
        string expectedSpanText,
        DiagnosticCode expectedCode,
        string expectedDocsReference)
    {
        Assert.IsFalse(string.IsNullOrWhiteSpace(caseId));

        var exception = Assert.Throws<MusoqQueryException>(() => CreateAndRunVirtualMachine(query, Rows));

        AssertSingleError(exception, expectedCode, DiagnosticPhase.Bind);
        AssertHasGuidance(exception);

        AssertExactSpan(exception, caseId, query, expectedSpanText);

        var envelope = exception.PrimaryEnvelope;
        Assert.AreEqual(expectedDocsReference, envelope.DocsReference);
    }

    private static void AssertExactSpan(
        MusoqQueryException exception,
        string caseId,
        string query,
        string expectedSpanText)
    {
        var envelope = exception.PrimaryEnvelope;
        var expectedStart = query.LastIndexOf(expectedSpanText, StringComparison.Ordinal);
        Assert.IsGreaterThanOrEqualTo(0, expectedStart);
        var actualText = envelope.Offset.HasValue && envelope.Length.HasValue
            ? query.Substring(envelope.Offset.Value, envelope.Length.Value)
            : "<unknown>";
        Assert.AreEqual(
            expectedStart,
            envelope.Offset,
            $"{caseId}: expected span '{expectedSpanText}' at {expectedStart}, actual '{actualText}' at {envelope.Offset} length {envelope.Length}.");
        Assert.AreEqual(
            expectedSpanText.Length,
            envelope.Length,
            $"{caseId}: expected span '{expectedSpanText}', actual '{actualText}' at {envelope.Offset} length {envelope.Length}.");
    }
}

public sealed record REC101EnumEntity(
    REC101PrimaryStatus Status,
    REC101SecondaryStatus Other,
    string Label,
    object Payload,
    int Number);

public enum REC101PrimaryStatus : short
{
    Queued = 10,
    Running = 20,
    Finished = 30
}

public enum REC101SecondaryStatus : short
{
    Queued = 10,
    Running = 20,
    Finished = 30
}
