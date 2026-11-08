using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Parser.Diagnostics;
using static Musoq.Evaluator.Tests.MusoqExceptionAssertions;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class DiagnosticREC110IncrementalRenameTests : BasicEntityTestBase
{
    [TestMethod]
    public void IncrementalRenameCandidates_ShouldHonorFrozenContract()
    {
        foreach (var candidate in CandidateCases())
            ValidateCandidate(candidate);
    }

    [TestMethod]
    public void CandidateMatrix_ShouldContainFortyEightCasesAcrossEightFamilies()
    {
        var candidates = CandidateCases();
        Assert.HasCount(48, candidates);
        Assert.HasCount(39, candidates.Where(static candidate => candidate.ExpectedCode is not null));
        Assert.HasCount(9, candidates.Where(static candidate => candidate.ExpectedCode is null));
        CollectionAssert.AllItemsAreUnique(candidates.Select(static candidate => candidate.CaseId).ToArray());
        foreach (var family in Families)
            Assert.HasCount(6, candidates.Where(candidate => candidate.Family == family), $"{family} must contain six registered cases.");
    }

    [TestMethod]
    public void CandidateMetadata_ShouldDeclareScopeAndClassificationForEveryCase()
    {
        foreach (var candidate in CandidateCases())
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(candidate.RootCause), candidate.CaseId);
            Assert.IsFalse(string.IsNullOrWhiteSpace(candidate.InsertionPoint), candidate.CaseId);
            Assert.IsFalse(string.IsNullOrWhiteSpace(candidate.ExpectedBoundaryToken), candidate.CaseId);
            Assert.IsTrue(candidate.SeedQuery.Contains($"/* {candidate.CaseId} */", StringComparison.Ordinal), candidate.CaseId);
            if (candidate.ExpectedCode is null)
            {
                Assert.AreEqual("valid_ordinary", candidate.ExpectedClassification, candidate.CaseId);
                Assert.AreEqual("no_change", candidate.RepairPolicy, candidate.CaseId);
            }
            else
            {
                Assert.AreEqual("invalid", candidate.ExpectedClassification, candidate.CaseId);
                Assert.AreEqual("request_observation", candidate.RepairPolicy, candidate.CaseId);
                Assert.IsFalse(string.IsNullOrWhiteSpace(candidate.ExpectedToken), candidate.CaseId);
            }
        }
    }

    [TestMethod]
    public void ValidControls_ShouldExecuteWithoutChangingScopeResolution()
    {
        foreach (var candidate in CandidateCases().Where(static candidate => candidate.ExpectedCode is null))
        {
            CreateAndRunVirtualMachine(candidate.SeedQuery, CreateSources()).Run(TestContext.CancellationToken);
            CreateAndRunVirtualMachine(candidate.ResultQuery, CreateSources()).Run(TestContext.CancellationToken);
        }
    }

    [TestMethod]
    public void GroupedUnknownConsumer_ShouldRetainIndependentUnknownColumnRoots()
    {
        var exception = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachine(
                "select Unknown, a.Country as RegionOut from #A.entities() a group by CountryOut",
                CreateSources()));

        Assert.HasCount(2, exception.Envelopes);
        CollectionAssert.AreEqual(
            new[] { DiagnosticCode.MQ3001_UnknownColumn, DiagnosticCode.MQ3001_UnknownColumn },
            exception.Envelopes.Select(static envelope => envelope.Code).ToArray());
        StringAssert.Contains(exception.Envelopes[0].Message, "Unknown");
        StringAssert.Contains(exception.Envelopes[1].Message, "CountryOut");
    }

    [TestMethod]
    public void GroupedQualifiedUnknownAlias_ShouldSuppressDependentProjectionCascade()
    {
        var exception = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachine(
                "select a.Country from #A.entities() a group by missing.Country",
                CreateSources()));

        Assert.HasCount(1, exception.Envelopes);
        Assert.AreEqual(DiagnosticCode.MQ3015_UnknownAlias, exception.PrimaryEnvelope.Code);
        StringAssert.Contains(exception.PrimaryEnvelope.Message, "missing");
    }

    [TestMethod]
    public void GroupedInvalidKey_ShouldRetainIndependentNonAggregateRoot()
    {
        var exception = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachine(
                "select a.Country, a.City, Count(*) from #A.entities() a group by a.Country, Missing",
                CreateSources()));

        Assert.HasCount(2, exception.Envelopes);
        Assert.AreEqual(1, exception.Envelopes.Count(static envelope =>
            envelope.Code == DiagnosticCode.MQ3001_UnknownColumn));
        Assert.AreEqual(1, exception.Envelopes.Count(static envelope =>
            envelope.Code == DiagnosticCode.MQ3012_NonAggregateInSelect));
        StringAssert.Contains(
            exception.Envelopes.Single(static envelope => envelope.Code == DiagnosticCode.MQ3001_UnknownColumn).Message,
            "Missing");
        StringAssert.Contains(
            exception.Envelopes.Single(static envelope => envelope.Code == DiagnosticCode.MQ3012_NonAggregateInSelect).Message,
            "City");
    }

    [TestMethod]
    public void GroupedKnownAliasUnknownColumn_ShouldRetainIndependentProjectionRoot()
    {
        var exception = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachine(
                "select a.Country from #A.entities() a group by a.Missing",
                CreateSources()));

        Assert.HasCount(2, exception.Envelopes);
        Assert.AreEqual(1, exception.Envelopes.Count(static envelope =>
            envelope.Code == DiagnosticCode.MQ3001_UnknownColumn));
        Assert.AreEqual(1, exception.Envelopes.Count(static envelope =>
            envelope.Code == DiagnosticCode.MQ3012_NonAggregateInSelect));
        StringAssert.Contains(
            exception.Envelopes.Single(static envelope => envelope.Code == DiagnosticCode.MQ3001_UnknownColumn).Message,
            "Missing");
        StringAssert.Contains(
            exception.Envelopes.Single(static envelope => envelope.Code == DiagnosticCode.MQ3012_NonAggregateInSelect).Message,
            "Country");
    }

    [TestMethod]
    public void GroupedQualifiedUnknownColumn_ShouldRetainIndependentCrossSourceProjectionRoot()
    {
        var exception = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachine(
                "select a.Country, Count(*) from #A.entities() a inner join #B.entities() b on a.Id = b.Id group by b.Countr",
                CreateSources()));

        Assert.HasCount(2, exception.Envelopes);
        Assert.AreEqual(1, exception.Envelopes.Count(static envelope =>
            envelope.Code == DiagnosticCode.MQ3001_UnknownColumn));
        Assert.AreEqual(1, exception.Envelopes.Count(static envelope =>
            envelope.Code == DiagnosticCode.MQ3012_NonAggregateInSelect));
        StringAssert.Contains(
            exception.Envelopes.Single(static envelope => envelope.Code == DiagnosticCode.MQ3001_UnknownColumn).Message,
            "Countr");
        StringAssert.Contains(
            exception.Envelopes.Single(static envelope => envelope.Code == DiagnosticCode.MQ3012_NonAggregateInSelect).Message,
            "Country");
    }

    private void ValidateCandidate(RecoveryCase candidate)
    {
        CreateAndRunVirtualMachine(candidate.SeedQuery, CreateSources()).Run(TestContext.CancellationToken);
        if (candidate.ExpectedCode is null)
        {
            CreateAndRunVirtualMachine(candidate.ResultQuery, CreateSources()).Run(TestContext.CancellationToken);
            return;
        }
        var exception = Assert.Throws<MusoqQueryException>(() => CreateAndRunVirtualMachine(candidate.ResultQuery, CreateSources()));
        AssertSingleError(exception, candidate.ExpectedCode.Value, DiagnosticPhase.Bind, candidate.ExpectedToken!);
        AssertHasGuidance(exception);
        var envelope = exception.PrimaryEnvelope;
        var text = string.Join(" ", new[] { envelope.Message, envelope.Explanation }.Concat(envelope.SuggestedFixes));
        StringAssert.Contains(text, candidate.Guidance, candidate.CaseId);
    }

    private static readonly string[] Families =
    [
        "SOURCE_ALIAS_CONSUMERS", "PROJECTION_ALIAS_CONSUMERS", "CTE_OUTPUT_CONSUMERS", "CTE_REFERENCE_ALIASES",
        "DERIVED_OUTPUT_CONSUMERS", "NESTED_SHADOWING", "EXPORT_BOUNDARIES", "BLOCK_TARGET"
    ];

    private static IReadOnlyList<RecoveryCase> CandidateCases() =>
    [
        Invalid("A01", "SOURCE_ALIAS_CONSUMERS", "select a.City from #A.entities() a", "from #A.entities() a", "from #A.entities() renamed", DiagnosticCode.MQ3015_UnknownAlias, "a", "alias", "source-alias-select", "SELECT source projection", "SELECT"),
        Invalid("A02", "SOURCE_ALIAS_CONSUMERS", "select a.City from #A.entities() a where a.Population > 0", "select a.City from #A.entities() a", "select renamed.City from #A.entities() renamed", DiagnosticCode.MQ3015_UnknownAlias, "a", "alias", "source-alias-where", "WHERE source predicate", "WHERE"),
        Invalid("A03-v2", "SOURCE_ALIAS_CONSUMERS", "select Count(*) from #A.entities() a group by a.Country", "select Count(*) from #A.entities() a", "select Count(*) from #A.entities() renamed", DiagnosticCode.MQ3015_UnknownAlias, "a", "alias", "source-alias-group", "GROUP BY source expression", "GROUP BY"),
        Invalid("A04", "SOURCE_ALIAS_CONSUMERS", "select a.City from #A.entities() a order by a.City", "select a.City from #A.entities() a", "select renamed.City from #A.entities() renamed", DiagnosticCode.MQ3015_UnknownAlias, "a", "alias", "source-alias-order", "ORDER BY source expression", "ORDER BY"),
        Invalid("A05", "SOURCE_ALIAS_CONSUMERS", "select a.City from #A.entities() a inner join #B.entities() b on a.Id = b.Id", "select a.City from #A.entities() a", "select renamed.City from #A.entities() renamed", DiagnosticCode.MQ3015_UnknownAlias, "a", "alias", "source-alias-join", "JOIN ON source expression", "JOIN ON"),
        Invalid("A06", "SOURCE_ALIAS_CONSUMERS", "select a.Name from #A.entities() a cross apply a.Children child where a.Id > 0", "select a.Name from #A.entities() a cross apply a.Children child", "select renamed.Name from #A.entities() renamed cross apply renamed.Children child", DiagnosticCode.MQ3015_UnknownAlias, "a", "alias", "source-alias-apply", "CROSS APPLY source and stale WHERE", "CROSS APPLY"),
        Invalid("B01", "PROJECTION_ALIAS_CONSUMERS", "select a.City as CityOut from #A.entities() a where CityOut = 'WARSAW'", "select a.City as CityOut", "select a.City as TownOut", DiagnosticCode.MQ3001_UnknownColumn, "CityOut", "column", "projection-alias-where", "WHERE SELECT alias", "WHERE"),
        Invalid("B02-v2", "PROJECTION_ALIAS_CONSUMERS", "select a.Country as CountryOut from #A.entities() a group by CountryOut", "select a.Country as CountryOut", "select a.Country as RegionOut", DiagnosticCode.MQ3001_UnknownColumn, "CountryOut", "column", "projection-alias-group", "GROUP BY SELECT alias", "GROUP BY"),
        Invalid("B03", "PROJECTION_ALIAS_CONSUMERS", "select Count(*) as CountOut from #A.entities() a group by a.Country having CountOut > 0", "select Count(*) as CountOut", "select Count(*) as TotalOut", DiagnosticCode.MQ3001_UnknownColumn, "CountOut", "column", "projection-alias-having", "HAVING aggregate alias", "HAVING"),
        Invalid("B04", "PROJECTION_ALIAS_CONSUMERS", "select a.City as CityOut from #A.entities() a order by CityOut", "select a.City as CityOut", "select a.City as TownOut", DiagnosticCode.MQ3001_UnknownColumn, "CityOut", "column", "projection-alias-order", "ORDER BY SELECT alias", "ORDER BY"),
        Invalid("B05", "PROJECTION_ALIAS_CONSUMERS", "select Count(*) as CountOut from #A.entities() a group by a.Country order by CountOut", "select Count(*) as CountOut", "select Count(*) as TotalOut", DiagnosticCode.MQ3001_UnknownColumn, "CountOut", "column", "projection-alias-aggregate-order", "ORDER BY aggregate alias", "ORDER BY"),
        Invalid("B06", "PROJECTION_ALIAS_CONSUMERS", "select a.City as CityOut from #A.entities() a where CityOut like 'W%'", "select a.City as CityOut", "select a.City as TownOut", DiagnosticCode.MQ3001_UnknownColumn, "CityOut", "column", "projection-alias-like-where", "LIKE predicate SELECT alias", "WHERE"),
        Invalid("C01", "CTE_OUTPUT_CONSUMERS", "with p as (select a.City as CityOut from #A.entities() a) select CityOut from p", "select a.City as CityOut", "select a.City as TownOut", DiagnosticCode.MQ3001_UnknownColumn, "CityOut", "column", "cte-output-select", "CTE output SELECT consumer", "CTE output"),
        Invalid("C02", "CTE_OUTPUT_CONSUMERS", "with p as (select a.City as CityOut from #A.entities() a) select CityOut from p where CityOut = 'WARSAW'", "select a.City as CityOut from #A.entities() a) select CityOut from p", "select a.City as TownOut from #A.entities() a) select TownOut from p", DiagnosticCode.MQ3001_UnknownColumn, "CityOut", "column", "cte-output-where", "CTE output WHERE consumer", "WHERE"),
        Invalid("C03", "CTE_OUTPUT_CONSUMERS", "with p as (select a.City as CityOut from #A.entities() a) select CityOut from p order by CityOut", "select a.City as CityOut from #A.entities() a) select CityOut from p", "select a.City as TownOut from #A.entities() a) select TownOut from p", DiagnosticCode.MQ3001_UnknownColumn, "CityOut", "column", "cte-output-order", "CTE output ORDER BY consumer", "ORDER BY"),
        Invalid("C04-v2", "CTE_OUTPUT_CONSUMERS", "with p as (select a.Country as CountryOut from #A.entities() a) select Count(*) from p group by CountryOut", "select a.Country as CountryOut from #A.entities() a) select Count(*)", "select a.Country as RegionOut from #A.entities() a) select Count(*)", DiagnosticCode.MQ3001_UnknownColumn, "CountryOut", "column", "cte-output-group", "CTE output GROUP BY consumer", "GROUP BY"),
        Invalid("C05", "CTE_OUTPUT_CONSUMERS", "with p as (select a.City as CityOut from #A.entities() a) select p.CityOut from p inner join #B.entities() b on p.CityOut = b.City", "select a.City as CityOut from #A.entities() a) select p.CityOut", "select a.City as TownOut from #A.entities() a) select p.TownOut", DiagnosticCode.MQ3001_UnknownColumn, "CityOut", "column", "cte-output-join", "CTE output JOIN consumer", "JOIN ON"),
        Invalid("C06", "CTE_OUTPUT_CONSUMERS", "with p as (select a.City as CityOut from #A.entities() a), q as (select CityOut from p) select q.CityOut from q", "select a.City as CityOut from #A.entities() a), q as (select CityOut from p) select q.CityOut", "select a.City as TownOut from #A.entities() a), q as (select TownOut from p) select q.CityOut", DiagnosticCode.MQ3001_UnknownColumn, "CityOut", "column", "cte-output-nested", "nested CTE output consumer", "outer CTE output"),
        Invalid("D01", "CTE_REFERENCE_ALIASES", "with p as (select a.City from #A.entities() a) select c.City from p c", "from p c", "from p renamed", DiagnosticCode.MQ3015_UnknownAlias, "c", "alias", "cte-reference-select", "CTE reference alias in SELECT", "SELECT"),
        Invalid("D02", "CTE_REFERENCE_ALIASES", "with p as (select a.City from #A.entities() a) select c.City from p c where c.City = 'WARSAW'", "select c.City from p c", "select renamed.City from p renamed", DiagnosticCode.MQ3015_UnknownAlias, "c", "alias", "cte-reference-where", "CTE reference alias in WHERE", "WHERE"),
        Invalid("D03", "CTE_REFERENCE_ALIASES", "with p as (select a.City from #A.entities() a) select c.City from p c order by c.City", "select c.City from p c", "select renamed.City from p renamed", DiagnosticCode.MQ3015_UnknownAlias, "c", "alias", "cte-reference-order", "CTE reference alias in ORDER BY", "ORDER BY"),
        Invalid("D04-v2", "CTE_REFERENCE_ALIASES", "with p as (select a.Country from #A.entities() a) select Count(*) from p c group by c.Country", "select Count(*) from p c", "select Count(*) from p renamed", DiagnosticCode.MQ3015_UnknownAlias, "c", "alias", "cte-reference-group", "CTE reference alias in GROUP BY", "GROUP BY"),
        Invalid("D05", "CTE_REFERENCE_ALIASES", "with p as (select a.City from #A.entities() a) select c.City from p c inner join #B.entities() b on c.City = b.City", "select c.City from p c", "select renamed.City from p renamed", DiagnosticCode.MQ3015_UnknownAlias, "c", "alias", "cte-reference-join", "CTE reference alias in JOIN ON", "JOIN ON"),
        Invalid("D06", "CTE_REFERENCE_ALIASES", "with p as (select a.City from #A.entities() a), q as (select c.City from p c) select q.City from q", "from p c", "from p renamed", DiagnosticCode.MQ3015_UnknownAlias, "c", "alias", "cte-reference-nested", "nested CTE reference alias", "nested CTE"),
        Invalid("E01", "DERIVED_OUTPUT_CONSUMERS", "select d.CityOut from (select a.City as CityOut from #A.entities() a) d", "as CityOut", "as TownOut", DiagnosticCode.MQ3001_UnknownColumn, "CityOut", "column", "derived-output-select", "derived output SELECT consumer", "SELECT"),
        Invalid("E02", "DERIVED_OUTPUT_CONSUMERS", "select d.CityOut from (select a.City as CityOut from #A.entities() a) d where d.CityOut = 'WARSAW'", "select d.CityOut from (select a.City as CityOut", "select d.TownOut from (select a.City as TownOut", DiagnosticCode.MQ3001_UnknownColumn, "CityOut", "column", "derived-output-where", "derived output WHERE consumer", "WHERE"),
        Invalid("E03", "DERIVED_OUTPUT_CONSUMERS", "select d.CityOut from (select a.City as CityOut from #A.entities() a) d order by d.CityOut", "select d.CityOut from (select a.City as CityOut", "select d.TownOut from (select a.City as TownOut", DiagnosticCode.MQ3001_UnknownColumn, "CityOut", "column", "derived-output-order", "ORDER BY consumer", "ORDER BY"),
        Invalid("E04-v2", "DERIVED_OUTPUT_CONSUMERS", "select Count(*) from (select a.Country as CountryOut from #A.entities() a) d group by d.CountryOut", "select a.Country as CountryOut", "select a.Country as RegionOut", DiagnosticCode.MQ3001_UnknownColumn, "CountryOut", "column", "derived-output-group", "derived output GROUP BY consumer", "GROUP BY"),
        Invalid("E05", "DERIVED_OUTPUT_CONSUMERS", "select d.CityOut from (select a.City as CityOut from #A.entities() a) d inner join #B.entities() b on d.CityOut = b.City", "select d.CityOut from (select a.City as CityOut", "select d.TownOut from (select a.City as TownOut", DiagnosticCode.MQ3001_UnknownColumn, "CityOut", "column", "derived-output-join", "derived output JOIN consumer", "JOIN ON"),
        Invalid("E06", "DERIVED_OUTPUT_CONSUMERS", "select x.CityOut from (select d.CityOut from (select a.City as CityOut from #A.entities() a) d) x", "select d.CityOut from (select a.City as CityOut", "select d.TownOut from (select a.City as TownOut", DiagnosticCode.MQ3001_UnknownColumn, "CityOut", "column", "derived-output-nested", "nested derived output consumer", "outer derived output"),
        Invalid("F01-v3", "NESTED_SHADOWING", "select a.City from #A.entities() a where exists (select 1 from #B.entities() b where b.Country = a.Country)", "select 1 from #B.entities() b where b.Country", "select 1 from #B.entities() renamedB where b.Country", DiagnosticCode.MQ3015_UnknownAlias, "b", "alias", "nested-shadow-inner", "inner stale alias with outer alias visible", "inner WHERE"),
        Invalid("F02-v3", "NESTED_SHADOWING", "select a.City from #A.entities() a where exists (select 1 from #B.entities() b where b.Country = a.Country)", "select a.City from #A.entities() a where exists", "select renamedA.City from #A.entities() renamedA where exists", DiagnosticCode.MQ3015_UnknownAlias, "a", "alias", "nested-shadow-outer", "outer stale alias in correlated inner block", "correlation"),
        Valid("F03", "NESTED_SHADOWING", "select a.City from #A.entities() a where exists (select a.City from #B.entities() a where a.Country = a.Country)", "select a.City from #B.entities() a", "select a.City from #B.entities() a", "alias", "nested-shadow-same-spelling", "same-spelled inner alias shadows outer alias"),
        Valid("F04", "NESTED_SHADOWING", "select a.City from #A.entities() a where exists (select b.City from #B.entities() b where b.Country = a.Country)", "where b.Country = a.Country", "where b.Country = a.Country", "alias", "nested-shadow-correlation", "outer alias remains visible to inner correlation"),
        Invalid("F05", "NESTED_SHADOWING", "with p as (select a.City as CityOut from #A.entities() a) select p.CityOut from p where exists (select b.City from #B.entities() b where b.Country = p.CityOut)", "b.Country = p.CityOut", "b.Country = c.CityOut", DiagnosticCode.MQ3015_UnknownAlias, "c", "alias", "nested-shadow-cte", "stale CTE alias in nested block", "nested WHERE"),
        Valid("F06", "NESTED_SHADOWING", "select a.City from #A.entities() a where exists (select d.City from (select b.City from #B.entities() b) d where d.City = a.City)", "where d.City = a.City", "where d.City = a.City", "alias", "nested-shadow-derived", "derived alias and outer correlation remain visible"),
        Invalid("G01-v2", "EXPORT_BOUNDARIES", "with p as (select a.City from #A.entities() a) select City from p", "select City from p", "select [a.City] from p", DiagnosticCode.MQ3001_UnknownColumn, "a.City", "column", "export-source-qualified-leak", "source qualifier is not exported by CTE", "CTE output"),
        Valid("G02", "EXPORT_BOUNDARIES", "with p as (select a.City from #A.entities() a) select City from p", "select City from p", "select [City] from p", "column", "export-unqualified-column", "CTE exports the simple projection name"),
        Valid("G03", "EXPORT_BOUNDARIES", "with p as (select a.City as [a.City] from #A.entities() a) select [a.City] from p", "select [a.City] from p", "select p.[a.City] from p", "column", "export-explicit-dotted", "explicit dotted CTE output is exported"),
        Invalid("G04", "EXPORT_BOUNDARIES", "select d.City from (select a.City from #A.entities() a) d", "select d.City", "select d.[a.City]", DiagnosticCode.MQ3001_UnknownColumn, "a.City", "column", "export-derived-source-qualified-leak", "source qualifier is not exported by derived table", "derived output"),
        Valid("G05", "EXPORT_BOUNDARIES", "select d.City from (select a.City from #A.entities() a) d", "select d.City", "select d.[City]", "column", "export-derived-simple", "derived table exports simple projection name"),
        Valid("G06", "EXPORT_BOUNDARIES", "select d.[a.City] from (select a.City as [a.City] from #A.entities() a) d", "select d.[a.City]", "select d.[a.City]", "column", "export-derived-explicit-dotted", "derived table exports explicit dotted name"),
        Invalid("H01", "BLOCK_TARGET", "with leftq as (select a.City as CityOut from #A.entities() a), rightq as (select b.City as CityOut from #B.entities() b) select r.CityOut from rightq r", "rightq as (select b.City as CityOut from #B.entities() b) select r.CityOut", "rightq as (select b.City as TownOut from #B.entities() b) select r.CityOut", DiagnosticCode.MQ3001_UnknownColumn, "CityOut", "column", "block-target-right-output", "stale consumer remains in right query block", "right CTE"),
        Invalid("H02", "BLOCK_TARGET", "with leftq as (select a.City as CityOut from #A.entities() a), rightq as (select b.City as CityOut from #B.entities() b) select l.CityOut from leftq l inner join rightq r on l.CityOut = r.CityOut", "rightq as (select b.City as CityOut from #B.entities() b)", "rightq as (select b.City as TownOut from #B.entities() b)", DiagnosticCode.MQ3001_UnknownColumn, "CityOut", "column", "block-target-join-output", "stale consumer belongs to right JOIN input", "right JOIN input"),
        Valid("H03", "BLOCK_TARGET", "with leftq as (select a.City as CityOut from #A.entities() a), rightq as (select b.City as CityOut from #B.entities() b) select l.CityOut from leftq l inner join rightq r on l.CityOut = r.CityOut", "select l.CityOut from leftq l", "select l.CityOut from leftq l", "column", "block-target-valid-left", "left block remains valid beside same-named right block"),
        Invalid("H04", "BLOCK_TARGET", "with p as (select a.City as CityOut from #A.entities() a), q as (select CityOut from p) select q.CityOut from q", "q as (select CityOut from p) select q.CityOut", "q as (select CityOut as TownOut from p) select q.CityOut", DiagnosticCode.MQ3001_UnknownColumn, "CityOut", "column", "block-target-nested-output", "outer consumer targets renamed nested output", "outer CTE"),
        Valid("H05", "BLOCK_TARGET", "with p as (select a.City as CityOut from #A.entities() a) select p.CityOut from p", "select p.CityOut from p", "select p.CityOut from p", "column", "block-target-valid-cte", "CTE output remains visible in its consumer block"),
        Invalid("H06", "BLOCK_TARGET", "select l.CityOut from (select a.City as CityOut from #A.entities() a) l inner join (select b.City as CityOut from #B.entities() b) r on l.CityOut = r.CityOut", "select b.City as CityOut from #B.entities() b", "select b.City as TownOut from #B.entities() b", DiagnosticCode.MQ3001_UnknownColumn, "CityOut", "column", "block-target-derived-join", "stale consumer targets the renamed right derived output", "right derived input"),
    ];

    private static RecoveryCase Invalid(string id,string family,string seed,string before,string after,DiagnosticCode code,string token,string guidance,string root,string insert,string boundary) =>
        Create(id,family,seed,before,after,"invalid",code,token,guidance,"request_observation",root,insert,boundary);

    private static RecoveryCase Valid(string id,string family,string seed,string before,string after,string root,string insert,string boundary) =>
        Create(id,family,seed,before,after,"valid_ordinary",null,null,"valid","no_change",root,insert,boundary);

    private static RecoveryCase Create(string id,string family,string seed,string before,string after,string classification,DiagnosticCode? code,string? token,string guidance,string repair,string root,string insert,string boundary)
    {
        var marker = $" /* {id} */";
        return new RecoveryCase(id,family,seed+marker,ReplaceOnce(seed,before,after)+marker,classification,code,token,guidance,repair,root,insert,boundary);
    }

    private static string ReplaceOnce(string source,string before,string after)
    {
        var start = source.IndexOf(before,StringComparison.Ordinal);
        if(start<0 || source.IndexOf(before,start+before.Length,StringComparison.Ordinal)>=0)
            throw new InvalidOperationException($"Expected exactly one '{before}' occurrence in '{source}'.");
        return source[..start]+after+source[(start+before.Length)..];
    }

    private static IDictionary<string,IEnumerable<BasicEntity>> CreateSources() =>
        new Dictionary<string,IEnumerable<BasicEntity>>
        {
            ["#A"] = [new BasicEntity { Name="A1", City="WARSAW", Country="POLAND", Population=500, Id=1 }, new BasicEntity { Name="A2", City="BERLIN", Country="GERMANY", Population=250, Id=2 }],
            ["#B"] = [new BasicEntity { Name="B1", City="WARSAW", Country="POLAND", Population=450, Id=1 }, new BasicEntity { Name="B2", City="KRAKOW", Country="POLAND", Population=300, Id=3 }]
        };

    private sealed record RecoveryCase(string CaseId,string Family,string SeedQuery,string ResultQuery,string ExpectedClassification,DiagnosticCode? ExpectedCode,string? ExpectedToken,string Guidance,string RepairPolicy,string RootCause,string InsertionPoint,string ExpectedBoundaryToken);
}
