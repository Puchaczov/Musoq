using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Tests.Schema.Generic;
using Musoq.Parser.Diagnostics;
using Musoq.Schema;
using static Musoq.Evaluator.Tests.MusoqExceptionAssertions;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class DiagnosticREC100EnumSemanticsTests : GenericEntityTestBase
{
    private static readonly REC100EnumEntity[] Rows =
    [
        new(REC100PrimaryStatus.Running, REC100SecondaryStatus.Running, REC100PermissionMask.ReadWrite)
    ];

    public static IEnumerable<object[]> MemberCasingCases()
    {
        yield return new object[]
        {
            "REC-100-MEM01",
            "select e.Status from #schema.first() e where e.Status = 'running'",
            "'running'"
        };
        yield return new object[]
        {
            "REC-100-MEM02",
            "select e.Status from #schema.first() e where e.Status <> 'RUNNING'",
            "'RUNNING'"
        };
        yield return new object[]
        {
            "REC-100-MEM03",
            "select e.Status from #schema.first() e where e.Status in ('queued')",
            "'queued'"
        };
        yield return new object[]
        {
            "REC-100-MEM04",
            "select e.Status from #schema.first() e where e.Status not in ('finished')",
            "'finished'"
        };
        yield return new object[]
        {
            "REC-100-MEM05",
            "select case when true then e.Status else 'running' end as Value from #schema.first() e",
            "'running'"
        };
        yield return new object[]
        {
            "REC-100-MEM06",
            "select HasAnyFlags(e.Access, 'read') as HasRead from #schema.first() e",
            "'read'"
        };
    }

    public static IEnumerable<object[]> IdentityCases()
    {
        yield return new object[]
        {
            "REC-100-ID01",
            "select e.Status from #schema.first() e where e.Status = e.Other",
            "Status = e.Other",
            false
        };
        yield return new object[]
        {
            "REC-100-ID02",
            "select e.Status from #schema.first() e where e.Status <> e.Other",
            "Status <> e.Other",
            false
        };
        yield return new object[]
        {
            "REC-100-ID03",
            "select e.Status from #schema.first() e where e.Status in (e.Other)",
            "Other",
            false
        };
        yield return new object[]
        {
            "REC-100-ID04",
            "select e.Status from #schema.first() e where e.Status not in (e.Other)",
            "Other",
            false
        };
        yield return new object[]
        {
            "REC-100-ID05",
            "select case when true then e.Status else e.Other end as Value from #schema.first() e",
            "Other",
            false
        };
        yield return new object[]
        {
            "REC-100-ID06",
            "select e.Status as Value from #schema.first() e inner join #schema.second() peer on e.Status = peer.Other",
            "Status = peer.Other",
            true
        };
    }

    public static IEnumerable<object[]> UnsupportedOperatorCases()
    {
        yield return new object[]
        {
            "REC-100-OP01",
            "select 1 from #schema.first() e where e.Status > 'Running'",
            "Status > 'Running'"
        };
        yield return new object[]
        {
            "REC-100-OP02",
            "select e.Status + 1 as Value from #schema.first() e",
            "Status + 1"
        };
        yield return new object[]
        {
            "REC-100-OP03",
            "select 1 from #schema.first() e where e.Status like 'R%'",
            "Status like 'R%'"
        };
        yield return new object[]
        {
            "REC-100-OP04",
            "select 1 from #schema.first() e where e.Status between 'Queued' and 'Finished'",
            "Status between 'Queued' and 'Finished'"
        };
        yield return new object[]
        {
            "REC-100-OP05",
            "select e.Status::int as Numeric from #schema.first() e",
            "Status::int"
        };
        yield return new object[]
        {
            "REC-100-OP06",
            "select EnumValue(e.Status) as Value from #schema.first() e order by e.Status",
            "Status"
        };
    }

    [TestMethod]
    [DynamicData(nameof(MemberCasingCases))]
    public void QuotedEnumMembers_ShouldUseExactCaseSensitiveBinding(
        string caseId,
        string query,
        string expectedSpanText)
    {
        AssertSemanticDiagnostic(
            caseId,
            query,
            expectedSpanText,
            DiagnosticCode.MQ3108_UnknownEnumMember,
            useSecondSource: false);
    }

    [TestMethod]
    [DynamicData(nameof(IdentityCases))]
    public void EnumOperations_ShouldRejectCrossIdentityValues(
        string caseId,
        string query,
        string expectedSpanText,
        bool useSecondSource)
    {
        AssertSemanticDiagnostic(
            caseId,
            query,
            expectedSpanText,
            DiagnosticCode.MQ3109_EnumIdentityMismatch,
            useSecondSource);
    }

    [TestMethod]
    [DynamicData(nameof(UnsupportedOperatorCases))]
    public void UnsupportedEnumOperators_ShouldReportTheEnumContractDiagnostic(
        string caseId,
        string query,
        string expectedSpanText)
    {
        AssertSemanticDiagnostic(
            caseId,
            query,
            expectedSpanText,
            DiagnosticCode.MQ3110_UnsupportedEnumOperator,
            useSecondSource: false);
    }

    [TestMethod]
    public void NativeEnumAliases_ShouldUseTheFirstDeclaredCanonicalName()
    {
        var table = CreateAndRunVirtualMachine(
            "select EnumValue(e.Status) as StatusValue, EnumName(e.Status) as StatusName, " +
            "IsDefined(e.Status) as StatusDefined from #schema.first() e",
            [
                new REC100EnumEntity(REC100PrimaryStatus.Running, REC100SecondaryStatus.Running, REC100PermissionMask.None),
                new REC100EnumEntity(REC100PrimaryStatus.Active, REC100SecondaryStatus.Running, REC100PermissionMask.None)
            ]).Run(TestContext.CancellationToken);

        Assert.HasCount(2, table);
        foreach (var row in table)
            CollectionAssert.AreEqual(new object?[] { (short)20, "Running", true }, row.Values);

        var status = table.Columns.Single(column => column.ColumnName == "StatusValue");
        Assert.AreEqual(typeof(short), status.ColumnType);
        Assert.IsNull(status.EnumType);
    }

    [TestMethod]
    public void NativeNamedComposite_ShouldRemainAnExactFlagsMember()
    {
        var table = CreateAndRunVirtualMachine(
            "select EnumName(e.Access) as AccessName, " +
            "HasAnyFlags(e.Access, 'ReadWrite') as HasComposite, " +
            "HasAllFlags(e.Access, 'Read', 'Write') as HasBits, " +
            "EnumValue(e.Access) as AccessValue from #schema.first() e",
            Rows).Run(TestContext.CancellationToken);

        Assert.HasCount(1, table);
        CollectionAssert.AreEqual(new object?[] { "ReadWrite", true, true, 3u }, table[0].Values);
        Assert.AreEqual(typeof(uint), table.Columns.Single(column => column.ColumnName == "AccessValue").ColumnType);
    }

    private void AssertSemanticDiagnostic(
        string caseId,
        string query,
        string expectedSpanText,
        DiagnosticCode expectedCode,
        bool useSecondSource)
    {
        Assert.IsFalse(string.IsNullOrWhiteSpace(caseId));

        var exception = useSecondSource
            ? Assert.Throws<MusoqQueryException>(() => CreateAndRunVirtualMachine(query, Rows, Rows))
            : Assert.Throws<MusoqQueryException>(() => CreateAndRunVirtualMachine(query, Rows));

        AssertSingleError(exception, expectedCode, DiagnosticPhase.Bind);
        AssertHasGuidance(exception);

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
        Assert.AreEqual("Core Spec - Enum Types", envelope.DocsReference);
        Assert.IsTrue(envelope.SuggestedFixes.All(fix =>
            !fix.Contains(nameof(REC100SecondaryStatus), StringComparison.Ordinal)));
    }
}

public sealed record REC100EnumEntity(
    REC100PrimaryStatus Status,
    REC100SecondaryStatus Other,
    REC100PermissionMask Access);

public enum REC100PrimaryStatus : short
{
    Queued = 10,
    Running = 20,
    Active = 20,
    Finished = 30
}

public enum REC100SecondaryStatus : short
{
    Queued = 10,
    Running = 20,
    Finished = 30
}

[Flags]
public enum REC100PermissionMask : uint
{
    None = 0,
    Read = 1,
    Write = 2,
    ReadWrite = Read | Write
}
