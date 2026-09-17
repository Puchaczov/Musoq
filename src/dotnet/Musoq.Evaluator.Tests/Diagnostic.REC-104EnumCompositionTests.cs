using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator.Tests.Schema.Generic;
using Musoq.Parser.Diagnostics;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using static Musoq.Evaluator.Tests.MusoqExceptionAssertions;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class DiagnosticREC104EnumCompositionTests : GenericEntityTestBase
{
    private static readonly REC104Entity[] Rows =
    [
        new(REC104PrimaryStatus.Running, REC104SecondaryStatus.Running, REC104PrimaryStatus.Queued, 1, "one"),
        new(REC104PrimaryStatus.Queued, REC104SecondaryStatus.Queued, null, 2, "two")
    ];

    private static readonly REC104Entity[] PeerRows =
    [
        new(REC104PrimaryStatus.Running, REC104SecondaryStatus.Running, REC104PrimaryStatus.Finished, 1, "peer"),
        new(REC104PrimaryStatus.Finished, REC104SecondaryStatus.Finished, null, 3, "other")
    ];

    public static IEnumerable<object[]> CandidateCases()
    {
        foreach (var candidate in CreateCandidates())
            yield return [candidate];
    }

    [TestMethod]
    [DynamicData(nameof(CandidateCases))]
    public void CompositionCandidates_ShouldHonorFrozenContract(REC104Candidate candidate)
    {
        Assert.IsFalse(string.IsNullOrWhiteSpace(candidate.CaseId));

        switch (candidate.Kind)
        {
            case REC104CandidateKind.IdentityConflict:
            case REC104CandidateKind.CarrierConflict:
                AssertDiagnostic(candidate);
                break;
            case REC104CandidateKind.WarmReplay:
                AssertRepeatedCompilation(candidate.Query);
                break;
            default:
                AssertCompatible(candidate.Query);
                break;
        }
    }

    [TestMethod]
    public void CandidateMatrix_ShouldContainFortyEightRegisteredCasesAcrossCompositionFamilies()
    {
        var cases = CreateCandidates().ToArray();

        Assert.HasCount(48, cases);
        Assert.AreEqual(cases.Length, cases.Select(static candidate => candidate.CaseId)
            .Distinct(StringComparer.Ordinal).Count());
        Assert.AreEqual(8, cases.Count(static candidate => candidate.CaseId.Contains("-C", StringComparison.Ordinal)));
        Assert.AreEqual(8, cases.Count(static candidate => candidate.CaseId.Contains("-D", StringComparison.Ordinal)));
        Assert.AreEqual(8, cases.Count(static candidate => candidate.CaseId.Contains("-J", StringComparison.Ordinal)));
        Assert.AreEqual(8, cases.Count(static candidate => candidate.CaseId.Contains("-S", StringComparison.Ordinal)));
        Assert.AreEqual(8, cases.Count(static candidate => candidate.CaseId.Contains("-I", StringComparison.Ordinal)));
        Assert.AreEqual(8, cases.Count(static candidate => candidate.CaseId.Contains("-W", StringComparison.Ordinal)));
        Assert.HasCount(7, cases.Where(static candidate => candidate.Kind == REC104CandidateKind.IdentityConflict));
        Assert.HasCount(1, cases.Where(static candidate => candidate.Kind == REC104CandidateKind.CarrierConflict));
    }

    [TestMethod]
    public void CompatibleComposition_Control_ShouldRetainNativeIdentityAfterEachRewrite()
    {
        foreach (var candidate in CreateCandidates().Where(static candidate =>
                     candidate.Kind is REC104CandidateKind.Compatible or REC104CandidateKind.WarmReplay))
        {
            using var compiled = CreateAndRunVirtualMachine(candidate.Query, Rows, PeerRows);
            using var table = compiled.Run(TestContext.CancellationToken);
            Assert.IsTrue(table.Columns.Any(column => column.EnumType?.DisplayName == typeof(REC104PrimaryStatus).FullName),
                candidate.CaseId);
            Assert.IsFalse(table.SelectMany(static row => row.Values).Any(static value => value is Enum),
                candidate.CaseId);
        }
    }

    [TestMethod]
    public void NullableComposition_Control_ShouldKeepNullableCarrierAndNativeIdentity()
    {
        const string query =
            "with states as (select e.OptionalStatus as Status from #schema.first() e) " +
            "select Status as Status from states";
        using var compiled = CreateAndRunVirtualMachine(query, Rows);
        using var table = compiled.Run(TestContext.CancellationToken);

        var column = table.Columns.Single(static candidate => candidate.ColumnName == "Status");
        Assert.AreEqual(typeof(short), Nullable.GetUnderlyingType(column.ColumnType) ?? column.ColumnType);
        Assert.IsNotNull(column.EnumType);
        Assert.AreEqual(typeof(REC104PrimaryStatus).FullName, column.EnumType.DisplayName);
        Assert.AreEqual(EnumTypeOrigin.NativeClr, column.EnumType.Origin);
    }

    private void AssertCompatible(string query)
    {
        using var compiled = CreateAndRunVirtualMachine(query, Rows, PeerRows);
        using var table = compiled.Run(TestContext.CancellationToken);

        Assert.IsGreaterThan(0, table.Count, query);
        var enumColumns = table.Columns
            .Where(static column => column.EnumType != null)
            .ToArray();
        Assert.IsGreaterThan(0, enumColumns.Length,
            query + " Columns=" + string.Join(", ", table.Columns.Select(static column =>
                $"{column.ColumnName}:{column.ColumnType.Name}/{column.SourceReadType.Name}/{column.EnumType?.DisplayName ?? "<none>"}")));

        foreach (var column in enumColumns)
        {
            Assert.AreEqual(typeof(short), Nullable.GetUnderlyingType(column.ColumnType) ?? column.ColumnType, query);
            Assert.AreEqual(typeof(short), Nullable.GetUnderlyingType(column.SourceReadType) ?? column.SourceReadType, query);
            Assert.AreEqual(typeof(REC104PrimaryStatus).FullName, column.EnumType!.DisplayName, query);
            Assert.AreEqual(EnumTypeOrigin.NativeClr, column.EnumType.Origin, query);
            Assert.IsFalse(string.IsNullOrWhiteSpace(column.EnumType.Fingerprint), query);
        }

        Assert.IsFalse(table.SelectMany(static row => row.Values).Any(static value => value is Enum), query);
    }

    private void AssertDiagnostic(REC104Candidate candidate)
    {
        var exception = candidate.ExpectedCode == DiagnosticCode.MQ3110_UnsupportedEnumOperator
            ? Assert.Throws<MusoqQueryException>(() => CreateAndRunVirtualMachine(candidate.Query, Rows, PeerRows))
            : Assert.Throws<MusoqQueryException>(() => CreateAndRunVirtualMachine(candidate.Query, Rows, PeerRows));

        AssertSingleError(exception, candidate.ExpectedCode!.Value, DiagnosticPhase.Bind);
        AssertHasGuidance(exception);
        Assert.IsFalse(exception.PrimaryEnvelope.Code is DiagnosticCode.MQ9001_InternalCompilerError or DiagnosticCode.MQ8001_CodeGenerationFailed,
            candidate.CaseId);

        if (!string.IsNullOrWhiteSpace(candidate.ExpectedSpan))
        {
            var expectedStart = candidate.Query.LastIndexOf(candidate.ExpectedSpan, StringComparison.Ordinal);
            Assert.IsGreaterThanOrEqualTo(0, expectedStart, candidate.CaseId);
            Assert.AreEqual(expectedStart, exception.PrimaryEnvelope.Offset, candidate.CaseId);
            Assert.AreEqual(candidate.ExpectedSpan.Length, exception.PrimaryEnvelope.Length, candidate.CaseId);
        }
    }

    private void AssertRepeatedCompilation(string query)
    {
        using var first = CreateAndRunVirtualMachine(query, Rows, PeerRows);
        using var firstTable = first.Run(TestContext.CancellationToken);
        using var second = CreateAndRunVirtualMachine(query, PeerRows, Rows);
        using var secondTable = second.Run(TestContext.CancellationToken);

        Assert.IsGreaterThan(0, firstTable.Count);
        Assert.IsGreaterThan(0, secondTable.Count);
        var firstEnum = firstTable.Columns.Single(static column => column.EnumType != null).EnumType!;
        var secondEnum = secondTable.Columns.Single(static column => column.EnumType != null).EnumType!;
        Assert.AreEqual(firstEnum.Fingerprint, secondEnum.Fingerprint);
        Assert.AreEqual(firstEnum.DisplayName, secondEnum.DisplayName);
        Assert.AreEqual(firstEnum.UnderlyingKind, secondEnum.UnderlyingKind);
    }

    private static IEnumerable<REC104Candidate> CreateCandidates()
    {
        yield return Candidate("REC-104-C01", REC104CandidateKind.Compatible, "with states as (select e.Status as Status from #schema.first() e) select states.Status as Status from states");
        yield return Candidate("REC-104-C02", REC104CandidateKind.Compatible, "with states as (select e.Status as Status from #schema.first() e) select Status as Status from states");
        yield return Candidate("REC-104-C03", REC104CandidateKind.Compatible, "with states as (select e.Status as Status from #schema.first() e), renamed as (select Status as CurrentStatus from states) select CurrentStatus as Status from renamed");
        yield return Candidate("REC-104-C04", REC104CandidateKind.Compatible, "with states as (select e.OptionalStatus as Status from #schema.first() e) select Status as Status from states");
        yield return Candidate("REC-104-C05", REC104CandidateKind.Compatible, "with states as (select e.Status as Status from #schema.first() e where e.Status = 'Running') select Status as Status from states");
        yield return Candidate("REC-104-C06", REC104CandidateKind.Compatible, "with states as (select e.Status as Status from #schema.first() e) select s.Status as Status from states s inner join #schema.second() p on s.Status = p.Status");
        yield return Candidate("REC-104-C07", REC104CandidateKind.Compatible, "with states as (select e.Status as Status, e.Number as Number from #schema.first() e) select Status as Status from states group by Status");
        yield return Candidate("REC-104-C08", REC104CandidateKind.Compatible, "with states as (select e.Status as Status from #schema.first() e union all (Status) select e.Status as Status from #schema.second() e) select Status as Status from states");

        yield return Candidate("REC-104-D01", REC104CandidateKind.Compatible, "select d.Status as Status from (select e.Status as Status from #schema.first() e) d");
        yield return Candidate("REC-104-D02", REC104CandidateKind.Compatible, "select d.CurrentStatus as Status from (select e.Status as CurrentStatus from #schema.first() e) d");
        yield return Candidate("REC-104-D03", REC104CandidateKind.Compatible, "select d.CurrentStatus as Status from (select x.Status as CurrentStatus from (select e.Status as Status from #schema.first() e) x) d");
        yield return Candidate("REC-104-D04", REC104CandidateKind.Compatible, "select d.Status as Status from (select e.OptionalStatus as Status from #schema.first() e) d");
        yield return Candidate("REC-104-D05", REC104CandidateKind.Compatible, "select d.Status as Status from (select e.Status as Status from #schema.first() e where e.Status = 'Running') d");
        yield return Candidate("REC-104-D06", REC104CandidateKind.Compatible, "select d.Status as Status from (select e.Status as Status from #schema.first() e) d inner join #schema.second() p on d.Status = p.Status");
        yield return Candidate("REC-104-D07", REC104CandidateKind.Compatible, "select d.Status as Status from (select e.Status as Status from #schema.first() e) d group by d.Status");
        yield return Candidate("REC-104-D08", REC104CandidateKind.Compatible, "select d.Status as Status from (select e.Status as Status from #schema.first() e union all select e.Status as Status from #schema.second() e) d");

        yield return Candidate("REC-104-J01", REC104CandidateKind.Compatible, "select a.Status as Status from #schema.first() a inner join #schema.second() b on a.Number = b.Number");
        yield return Candidate("REC-104-J02", REC104CandidateKind.Compatible, "select b.Status as Status from #schema.first() a inner join #schema.second() b on a.Number = b.Number");
        yield return Candidate("REC-104-J03", REC104CandidateKind.Compatible, "select a.Status as Status, b.Status as PeerStatus from #schema.first() a inner join #schema.second() b on a.Status = b.Status");
        yield return Candidate("REC-104-J04", REC104CandidateKind.Compatible, "select a.OptionalStatus as Status from #schema.first() a left join #schema.second() b on a.Number = b.Number");
        yield return Candidate("REC-104-J05", REC104CandidateKind.Compatible, "select a.Status as Status from #schema.first() a group by a.Status");
        yield return Candidate("REC-104-J06", REC104CandidateKind.Compatible, "select a.Status as Status, Count(a.Status) as StatusCount from #schema.first() a group by a.Status");
        yield return Candidate("REC-104-J07", REC104CandidateKind.Compatible, "select a.Status as Status from #schema.first() a inner join #schema.second() b on a.Number = b.Number group by a.Status");
        yield return Candidate("REC-104-J08", REC104CandidateKind.Compatible, "select a.Status as Status from #schema.first() a inner join #schema.second() b on a.Status = b.Status group by a.Status");

        yield return Candidate("REC-104-S01", REC104CandidateKind.Compatible, "select e.Status as Status from #schema.first() e union select e.Status as Status from #schema.second() e");
        yield return Candidate("REC-104-S02", REC104CandidateKind.Compatible, "select e.Status as Status from #schema.first() e union all select e.Status as Status from #schema.second() e");
        yield return Candidate("REC-104-S03", REC104CandidateKind.Compatible, "select e.Status as Status from #schema.first() e union all select null as Status from #schema.second() e");
        yield return Candidate("REC-104-S04", REC104CandidateKind.Compatible, "select null as Status from #schema.first() e union all select e.Status as Status from #schema.second() e");
        yield return Candidate("REC-104-S05", REC104CandidateKind.Compatible, "select e.Status as Status from #schema.first() e intersect select e.Status as Status from #schema.second() e");
        yield return Candidate("REC-104-S06", REC104CandidateKind.Compatible, "select e.Status as Status from #schema.first() e except select e.Status as Status from #schema.second() e");
        yield return Candidate("REC-104-S07", REC104CandidateKind.Compatible, "select e.Status as Status from #schema.first() e union all select e.Status as Status from #schema.second() e union all select e.Status as Status from #schema.first() e");
        yield return Candidate("REC-104-S08", REC104CandidateKind.Compatible, "select e.Status as Status from #schema.first() e union all select e.OptionalStatus as Status from #schema.second() e");

        yield return Candidate("REC-104-I01", REC104CandidateKind.IdentityConflict, "select e.Status as Status from #schema.first() e union all select e.Other as Status from #schema.second() e", DiagnosticCode.MQ3109_EnumIdentityMismatch, "Other");
        yield return Candidate("REC-104-I02", REC104CandidateKind.IdentityConflict, "with lefts as (select e.Status as Status from #schema.first() e), rights as (select e.Other as Status from #schema.second() e) select l.Status as Status from lefts l inner join rights r on l.Status = r.Status", DiagnosticCode.MQ3109_EnumIdentityMismatch, "Status = r.Status");
        yield return Candidate("REC-104-I03", REC104CandidateKind.IdentityConflict, "select d.Status as Status from (select e.Status as Status from #schema.first() e) d inner join #schema.second() p on d.Status = p.Other", DiagnosticCode.MQ3109_EnumIdentityMismatch, "Status = p.Other");
        yield return Candidate("REC-104-I04", REC104CandidateKind.IdentityConflict, "select a.Status as Status from #schema.first() a inner join #schema.second() b on a.Status = b.Other", DiagnosticCode.MQ3109_EnumIdentityMismatch, "Status = b.Other");
        yield return Candidate("REC-104-I05", REC104CandidateKind.IdentityConflict, "select case when e.Number = 1 then e.Status else e.Other end as Status from #schema.first() e", DiagnosticCode.MQ3109_EnumIdentityMismatch, "Other");
        yield return Candidate("REC-104-I06", REC104CandidateKind.CarrierConflict, "select e.Status as Status from #schema.first() e union all select e.Number as Status from #schema.second() e", DiagnosticCode.MQ3110_UnsupportedEnumOperator, "Number");
        yield return Candidate("REC-104-I07", REC104CandidateKind.IdentityConflict, "select e.Status as Status from #schema.first() e where e.Status in (select p.Other from #schema.second() p)", DiagnosticCode.MQ3109_EnumIdentityMismatch, "Other");
        yield return Candidate("REC-104-I08", REC104CandidateKind.IdentityConflict, "select e.Status as Status from #schema.first() e where e.OptionalStatus = e.Other", DiagnosticCode.MQ3109_EnumIdentityMismatch, "OptionalStatus = e.Other");

        yield return Candidate("REC-104-W01", REC104CandidateKind.WarmReplay, "select e.Status as Status from #schema.first() e");
        yield return Candidate("REC-104-W02", REC104CandidateKind.WarmReplay, "with states as (select e.Status as Status from #schema.first() e) select Status as Status from states");
        yield return Candidate("REC-104-W03", REC104CandidateKind.WarmReplay, "select d.Status as Status from (select e.Status as Status from #schema.first() e) d");
        yield return Candidate("REC-104-W04", REC104CandidateKind.WarmReplay, "select e.Status as Status, Count(e.Status) as StatusCount from #schema.first() e group by e.Status");
        yield return Candidate("REC-104-W05", REC104CandidateKind.WarmReplay, "select e.Status as Status from #schema.first() e union all select e.Status as Status from #schema.second() e");
        yield return Candidate("REC-104-W06", REC104CandidateKind.WarmReplay, "select e.OptionalStatus as Status from #schema.first() e");
        yield return Candidate("REC-104-W07", REC104CandidateKind.WarmReplay, "select e.Status as Status from #schema.first() e where e.Status = 'Running'");
        yield return Candidate("REC-104-W08", REC104CandidateKind.WarmReplay, "with states as (select e.Status as Status from #schema.first() e) select s.Status as Status from states s inner join #schema.second() p on s.Status = p.Status");
    }

    private static REC104Candidate Candidate(
        string caseId,
        REC104CandidateKind kind,
        string query,
        DiagnosticCode? expectedCode = null,
        string? expectedSpan = null)
    {
        return new REC104Candidate(caseId, kind, query + $" /* {caseId} */", expectedCode, expectedSpan);
    }
}

public sealed record REC104Candidate(
    string CaseId,
    REC104CandidateKind Kind,
    string Query,
    DiagnosticCode? ExpectedCode = null,
    string? ExpectedSpan = null);

public enum REC104CandidateKind
{
    Compatible,
    IdentityConflict,
    CarrierConflict,
    WarmReplay
}

public sealed record REC104Entity(
    REC104PrimaryStatus Status,
    REC104SecondaryStatus Other,
    REC104PrimaryStatus? OptionalStatus,
    int Number,
    string Label);

public enum REC104PrimaryStatus : short
{
    Queued = 10,
    Running = 20,
    Finished = 30
}

public enum REC104SecondaryStatus : short
{
    Queued = 10,
    Running = 20,
    Finished = 30
}
