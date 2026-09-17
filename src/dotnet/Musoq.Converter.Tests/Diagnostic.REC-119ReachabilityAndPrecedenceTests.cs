using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Tests.Components;
using Musoq.Converter.Tests.Schema;
using Musoq.Evaluator;
using Musoq.Parser.Diagnostics;

namespace Musoq.Converter.Tests;

[TestClass]
public sealed class DiagnosticREC119ReachabilityAndPrecedenceTests
{
    private const string SystemSeed = "select d.Dummy from #system.dual() d";
    private readonly TestsLoggerResolver loggerResolver = new();

    [TestMethod]
    public void AdvisoryMatrix_ShouldHonorFrozenContract()
    {
        Assert.HasCount(48, CandidateCases);
        Assert.HasCount(4, CandidateCases.GroupBy(static candidate => candidate.Family));
        Assert.IsTrue(CandidateCases.GroupBy(static candidate => candidate.Family).All(static family => family.Count() == 12));

        foreach (var candidate in CandidateCases)
        {
            var analysis = Analyze(candidate.Query);

            Assert.IsFalse(analysis.HasErrors, $"{candidate.Id}: {FormatDiagnostics(analysis.Diagnostics, candidate.Query)}");
            Assert.IsEmpty(analysis.Errors, $"{candidate.Id}: {FormatDiagnostics(analysis.Diagnostics, candidate.Query)}");
            Assert.AreEqual(
                candidate.ExpectedDiagnostics.Count,
                analysis.Warnings.Count(),
                $"{candidate.Id}: {FormatDiagnostics(analysis.Diagnostics, candidate.Query)}");
            CollectionAssert.AreEqual(
                candidate.ExpectedDiagnostics.ToArray(),
                analysis.Warnings.Select(static warning => warning.Code).ToArray(),
                candidate.Id);

            foreach (var warning in analysis.Warnings)
            {
                Assert.AreEqual(DiagnosticSeverity.Warning, warning.Severity, candidate.Id);
                Assert.AreEqual(DiagnosticPhase.Bind, warning.Phase, candidate.Id);
                Assert.AreEqual(DiagnosticSourceKind.Query, warning.SourceKind, candidate.Id);
                Assert.IsTrue(warning.Location.IsValid, candidate.Id);
                Assert.IsTrue(warning.Span.Start >= 0 && warning.Span.End <= candidate.Query.Length, candidate.Id);
            }

            var syntax = new QueryAnalyzer(new SystemSchemaProvider()).ValidateSyntax(candidate.Query);
            Assert.IsFalse(syntax.HasErrors, $"{candidate.Id}: {FormatDiagnostics(syntax.Diagnostics, candidate.Query)}");
            Assert.IsEmpty(syntax.Warnings, $"{candidate.Id}: {FormatDiagnostics(syntax.Diagnostics, candidate.Query)}");
        }
    }

    [TestMethod]
    public void WarningCompilation_ShouldRemainRunnableAndPreserveWarningContract()
    {
        foreach (var candidate in CandidateCases)
        {
            var build = InstanceCreator.CompileWithDiagnostics(
                candidate.Query,
                $"REC119_{candidate.Id}_{Guid.NewGuid():N}",
                new SystemSchemaProvider(),
                loggerResolver);

            Assert.IsTrue(build.Succeeded, $"{candidate.Id}: {FormatDiagnostics(build.Diagnostics, candidate.Query)}");
            Assert.IsEmpty(build.Errors, $"{candidate.Id}: {FormatDiagnostics(build.Diagnostics, candidate.Query)}");
            Assert.AreEqual(
                candidate.ExpectedDiagnostics.Count,
                build.Warnings.Count(),
                $"{candidate.Id}: {FormatDiagnostics(build.Diagnostics, candidate.Query)}");
            CollectionAssert.AreEqual(
                candidate.ExpectedDiagnostics.ToArray(),
                build.Warnings.Select(static warning => warning.Code).ToArray(),
                candidate.Id);

            using var table = build.CompiledQuery!.Run();
            Assert.IsNotNull(table, candidate.Id);
        }
    }

    [TestMethod]
    public void EquivalentReachabilityRewrites_ShouldKeepWarningCodesStable()
    {
        var deadCte = Analyze("with dead as (select d.Dummy from #system.dual() d) select d.Dummy from #system.dual() d");
        var deadCteWithAlias = Analyze("with dead as (select d.Dummy from #system.dual() d) select outerRow.Dummy from #system.dual() outerRow");
        var falseCase = Analyze("select case when false then 'dead' else 'live' end from #system.dual() d");
        var parenthesizedFalseCase = Analyze("select case when (false) then 'dead' else 'live' end from #system.dual() d");

        CollectionAssert.AreEqual(
            deadCte.Warnings.Select(static warning => warning.Code).ToArray(),
            deadCteWithAlias.Warnings.Select(static warning => warning.Code).ToArray());
        CollectionAssert.AreEqual(
            falseCase.Warnings.Select(static warning => warning.Code).ToArray(),
            parenthesizedFalseCase.Warnings.Select(static warning => warning.Code).ToArray());
    }

    [TestMethod]
    public void WarningOnlyCompilation_ShouldPreserveRuntimeValues()
    {
        const string query = "let dead: int = 1; select d.Dummy from #system.dual() d";
        var diagnosticBuild = InstanceCreator.CompileWithDiagnostics(
            query,
            $"REC119_runtime_diagnostic_{Guid.NewGuid():N}",
            new SystemSchemaProvider(),
            loggerResolver);

        Assert.IsTrue(diagnosticBuild.Succeeded, FormatDiagnostics(diagnosticBuild.Diagnostics, query));
        Assert.IsEmpty(diagnosticBuild.Errors, FormatDiagnostics(diagnosticBuild.Diagnostics, query));
        Assert.AreEqual(1, diagnosticBuild.Warnings.Count(static warning => warning.Code == DiagnosticCode.MQ5023_UnusedScriptVariable));
        using var diagnosticTable = diagnosticBuild.CompiledQuery!.Run();

        var convenienceQuery = InstanceCreator.CompileForExecution(
            query,
            $"REC119_runtime_convenience_{Guid.NewGuid():N}",
            new SystemSchemaProvider(),
            loggerResolver);
        using var convenienceTable = convenienceQuery.Run();

        Assert.AreEqual(convenienceTable.Count, diagnosticTable.Count);
        Assert.HasCount(convenienceTable.Columns.Count(), diagnosticTable.Columns);
        for (var rowIndex = 0; rowIndex < convenienceTable.Count; rowIndex++)
        {
            for (var columnIndex = 0; columnIndex < convenienceTable.Columns.Count(); columnIndex++)
                Assert.AreEqual(convenienceTable[rowIndex][columnIndex], diagnosticTable[rowIndex][columnIndex]);
        }
    }

    private static QueryAnalysisResult Analyze(string query)
    {
        return new QueryAnalyzer(new SystemSchemaProvider()).Analyze(query);
    }

    private static string FormatDiagnostics(IEnumerable<Diagnostic> diagnostics, string query)
    {
        return $"{query}{Environment.NewLine}{string.Join(Environment.NewLine, diagnostics.Select(static diagnostic => diagnostic.ToDetailedString()))}";
    }

    private static AdvisoryCase SystemCase(
        string id,
        string family,
        string query,
        IReadOnlyList<DiagnosticCode> expectedDiagnostics)
    {
        return new AdvisoryCase(id, family, query, expectedDiagnostics);
    }

    private static IReadOnlyList<AdvisoryCase> CandidateCases { get; } =
    [
        SystemCase("C01", "CTE_REACHABILITY", "with dead as (select d.Dummy from #system.dual() d) select d.Dummy from #system.dual() d", [DiagnosticCode.MQ5022_UnusedCte]),
        SystemCase("C02", "CTE_REACHABILITY", "with dead as (select d.Dummy from #system.dual() d), dead_child as (select c.Dummy from dead c) select d.Dummy from #system.dual() d", [DiagnosticCode.MQ5022_UnusedCte, DiagnosticCode.MQ5022_UnusedCte]),
        SystemCase("C03", "CTE_REACHABILITY", "with base as (select d.Dummy from #system.dual() d), live as (select b.Dummy from base b) select l.Dummy from live l", []),
        SystemCase("C04", "CTE_REACHABILITY", "with dead as (select d.Dummy from #system.dual() d), live as (select d.Dummy from #system.dual() d) select l.Dummy from live l", [DiagnosticCode.MQ5022_UnusedCte]),
        SystemCase("C05", "CTE_REACHABILITY", "with recursive counter (Value) as (select Value from values {( Value: 1 )} seed union all select c.Value + 1 from counter c where c.Value < 2) select d.Dummy from #system.dual() d", [DiagnosticCode.MQ5022_UnusedCte]),
        SystemCase("C06", "CTE_REACHABILITY", "with recursive counter (Value) as (select Value from values {( Value: 1 )} seed union all select c.Value + 1 from counter c where c.Value < 2) select c.Value from counter c", []),
        SystemCase("C07", "CTE_REACHABILITY", "with first_dead as (select d.Dummy from #system.dual() d), second_dead as (select d.Dummy from #system.dual() d) select d.Dummy from #system.dual() d", [DiagnosticCode.MQ5022_UnusedCte, DiagnosticCode.MQ5022_UnusedCte]),
        SystemCase("C08", "CTE_REACHABILITY", "with base as (select d.Dummy from #system.dual() d), live as (select b.Dummy from base b), dead as (select d.Dummy from #system.dual() d) select l.Dummy from live l", [DiagnosticCode.MQ5022_UnusedCte]),
        SystemCase("C09", "CTE_REACHABILITY", "with live as (select d.Dummy from #system.dual() d) select c.Dummy from live c", []),
        SystemCase("C10", "CTE_REACHABILITY", "with dead as (select d.Dummy from #system.dual() d where d.Dummy = 'x') select d.Dummy from #system.dual() d", [DiagnosticCode.MQ5022_UnusedCte]),
        SystemCase("C11", "CTE_REACHABILITY", "select (select e.Dummy from #system.dual() e) as Value from #system.dual() d", []),
        SystemCase("C12", "CTE_REACHABILITY", "select d.Dummy from #system.dual() d where d.Dummy in (select e.Dummy from #system.dual() e)", []),

        SystemCase("V01", "LET_REACHABILITY", "let dead: int = 1; select d.Dummy from #system.dual() d", [DiagnosticCode.MQ5023_UnusedScriptVariable]),
        SystemCase("V02", "LET_REACHABILITY", "let dead: int = 1; let alsoDead: int = $dead + 1; select d.Dummy from #system.dual() d", [DiagnosticCode.MQ5023_UnusedScriptVariable, DiagnosticCode.MQ5023_UnusedScriptVariable]),
        SystemCase("V03", "LET_REACHABILITY", "let root: int = 1; let live: int = $root + 1; select $live from #system.dual() d", []),
        SystemCase("V04", "LET_REACHABILITY", "let label: string = 'live'; with live as (select $label as Dummy from #system.dual() d) select l.Dummy from live l", []),
        SystemCase("V05", "LET_REACHABILITY", "let label: string = 'dead'; with dead as (select $label as Dummy from #system.dual() d) select d.Dummy from #system.dual() d", [DiagnosticCode.MQ5023_UnusedScriptVariable, DiagnosticCode.MQ5022_UnusedCte]),
        SystemCase("V06", "LET_REACHABILITY", "param (name: string = 'unused'); select d.Dummy from #system.dual() d", []),
        SystemCase("V07", "LET_REACHABILITY", "let value: string = 'live'; select $value from #system.dual() d", []),
        SystemCase("V08", "LET_REACHABILITY", "let root: int = 1; let live: int = $root + 1; with live_cte as (select $live as Dummy from #system.dual() d) select l.Dummy from live_cte l", []),
        SystemCase("V09", "LET_REACHABILITY", "let root: int = 1; let dead: int = $root + 1; select d.Dummy from #system.dual() d", [DiagnosticCode.MQ5023_UnusedScriptVariable, DiagnosticCode.MQ5023_UnusedScriptVariable]),
        SystemCase("V10", "LET_REACHABILITY", "let first: int = 1; let second: int = 2; select $first from #system.dual() d", [DiagnosticCode.MQ5023_UnusedScriptVariable]),
        SystemCase("V11", "LET_REACHABILITY", "let label: string = 'live'; select (select $label from #system.dual() e) as Value from #system.dual() d", []),
        SystemCase("V12", "LET_REACHABILITY", "let first: int = 1; let second: int = $first + 1; select $first from #system.dual() d", [DiagnosticCode.MQ5023_UnusedScriptVariable]),

        SystemCase("U01", "UNREACHABLE_BRANCH", "select case when false then 'dead' else 'live' end from #system.dual() d", [DiagnosticCode.MQ5008_UnreachableCode]),
        SystemCase("U02", "UNREACHABLE_BRANCH", "select case when null then 'null' else 'live' end from #system.dual() d", []),
        SystemCase("U03", "UNREACHABLE_BRANCH", "select case when true then 'first' when d.Dummy = 'x' then 'tail' else 'fallback' end from #system.dual() d", [DiagnosticCode.MQ5008_UnreachableCode]),
        SystemCase("U04", "UNREACHABLE_BRANCH", "select case when d.Dummy = 'x' then 'first' when d.Dummy = 'x' then 'duplicate' else 'fallback' end from #system.dual() d", [DiagnosticCode.MQ5008_UnreachableCode]),
        SystemCase("U05", "UNREACHABLE_BRANCH", "let enabled: bool = true; select case when $enabled then 'first' when d.Dummy = 'x' then 'tail' else 'fallback' end from #system.dual() d", [DiagnosticCode.MQ5008_UnreachableCode]),
        SystemCase("U06", "UNREACHABLE_BRANCH", "select 1 ?? 'unused' from #system.dual() d", [DiagnosticCode.MQ5008_UnreachableCode]),
        SystemCase("U07", "UNREACHABLE_BRANCH", "let value: int = 1; select $value ?? 2 from #system.dual() d", [DiagnosticCode.MQ5008_UnreachableCode]),
        SystemCase("U08", "UNREACHABLE_BRANCH", "param (value: string = 'value'); select $value ?? 'fallback' from #system.dual() d", []),
        SystemCase("U09", "UNREACHABLE_BRANCH", "select ToString(d.Dummy) ?? 'fallback' from #system.dual() d", []),
        SystemCase("U10", "UNREACHABLE_BRANCH", "select null ?? 'fallback' from #system.dual() d", []),
        SystemCase("U11", "UNREACHABLE_BRANCH", "param (value: bool = true); select case when $value then 'yes' else 'no' end from #system.dual() d", []),
        SystemCase("U12", "UNREACHABLE_BRANCH", "select case when d.Dummy = 'x' then 'first' when (d.Dummy = 'x') then 'duplicate' else 'fallback' end from #system.dual() d", [DiagnosticCode.MQ5008_UnreachableCode]),

        SystemCase("P01", "PRECEDENCE_INACTIVE", "select case when 1 = 2 then 'dead' else 'live' end from #system.dual() d", [DiagnosticCode.MQ5008_UnreachableCode]),
        SystemCase("P02", "PRECEDENCE_INACTIVE", "select case when 1 = 1 then 'live' else 'dead' end from #system.dual() d", [DiagnosticCode.MQ5008_UnreachableCode]),
        SystemCase("P03", "PRECEDENCE_INACTIVE", "select 1 ?? 'unused' from #system.dual() d", [DiagnosticCode.MQ5008_UnreachableCode]),
        SystemCase("P04", "PRECEDENCE_INACTIVE", SystemSeed + " where true", [DiagnosticCode.MQ5010_TautologicalCondition]),
        SystemCase("P05", "PRECEDENCE_INACTIVE", SystemSeed + " where false", [DiagnosticCode.MQ5011_ContradictoryCondition]),
        SystemCase("P06", "PRECEDENCE_INACTIVE", SystemSeed + " order by d.Dummy", []),
        SystemCase("P07", "PRECEDENCE_INACTIVE", "select outerRow.Dummy from #system.dual() outerRow order by outerRow.Dummy take 1", []),
        SystemCase("P08", "PRECEDENCE_INACTIVE", SystemSeed + " order by d.Dummy skip 0 take 1", []),
        SystemCase("P09", "PRECEDENCE_INACTIVE", "select 'b' as Dummy from #system.dual() d union all (Dummy) select 'a' as Dummy from #system.dual() e order by Dummy", []),
        SystemCase("P10", "PRECEDENCE_INACTIVE", "select 'b' as Dummy from #system.dual() d union all (Dummy) select 'a' as Dummy from #system.dual() e order by Dummy take 1", []),
        SystemCase("P11", "PRECEDENCE_INACTIVE", "select 'b' as Dummy from #system.dual() d union all (Dummy) select 'a' as Dummy from #system.dual() e order by Dummy skip 0 take 1", []),
        SystemCase("P12", "PRECEDENCE_INACTIVE", "select case when 1 = 2 then 'dead' else 'live' end from #system.dual() d where true", [DiagnosticCode.MQ5008_UnreachableCode, DiagnosticCode.MQ5010_TautologicalCondition])
    ];

    private sealed record AdvisoryCase(
        string Id,
        string Family,
        string Query,
        IReadOnlyList<DiagnosticCode> ExpectedDiagnostics);
}
