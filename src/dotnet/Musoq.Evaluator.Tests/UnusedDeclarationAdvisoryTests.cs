using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class UnusedDeclarationAdvisoryTests
{
    [TestMethod]
    public void UnreachableCteAndDeadDependencyChain_ReportEachDefinition()
    {
        var result = Analyze(@"
            with dead as (select Name from #A.Entities()),
                 dead_dependency as (select Name from dead),
                 live as (select Name from #A.Entities())
            select Name from live");

        Assert.IsFalse(result.HasErrors, string.Join(" | ", result.Diagnostics));
        Assert.AreEqual(2, result.Warnings.Count(static warning => warning.Code == DiagnosticCode.MQ5022_UnusedCte));
        Assert.IsFalse(result.Warnings.Any(static warning => warning.Code == DiagnosticCode.MQ5023_UnusedScriptVariable));
    }

    [TestMethod]
    public void TransitiveLiveCtes_RemainQuiet()
    {
        var result = Analyze(@"
            with base as (select Name from #A.Entities()),
                 live as (select Name from base)
            select Name from live");

        Assert.IsFalse(result.HasErrors, string.Join(" | ", result.Diagnostics));
        Assert.IsFalse(result.Warnings.Any(static warning => warning.Code == DiagnosticCode.MQ5022_UnusedCte));
    }

    [TestMethod]
    public void UnusedVariableChain_ReportsEveryDeadDeclaration()
    {
        var result = Analyze("let dead: int = 1; let alsoDead: int = $dead + 1; select Name from #A.Entities()");

        Assert.IsFalse(result.HasErrors, string.Join(" | ", result.Diagnostics));
        Assert.AreEqual(2, result.Warnings.Count(static warning => warning.Code == DiagnosticCode.MQ5023_UnusedScriptVariable));
    }

    [TestMethod]
    public void TransitiveLiveVariableChain_RemainsQuiet()
    {
        var result = Analyze("let root: int = 1; let live: int = $root + 1; select $live from #A.Entities()");

        Assert.IsFalse(result.HasErrors, string.Join(" | ", result.Diagnostics));
        Assert.IsFalse(result.Warnings.Any(static warning => warning.Code == DiagnosticCode.MQ5023_UnusedScriptVariable));
    }

    [TestMethod]
    public void VariableUsedByLiveCte_RemainsQuiet()
    {
        var result = Analyze(@"
            let label: string = 'live';
            with live as (select $label as Name from #A.Entities())
            select Name from live");

        Assert.IsFalse(result.HasErrors, string.Join(" | ", result.Diagnostics));
        Assert.IsFalse(result.Warnings.Any(static warning => warning.Code == DiagnosticCode.MQ5023_UnusedScriptVariable));
        Assert.IsFalse(result.Warnings.Any(static warning => warning.Code == DiagnosticCode.MQ5022_UnusedCte));
    }

    [TestMethod]
    public void VariableUsedOnlyByDeadCte_RemainsUnused()
    {
        var result = Analyze(@"
            let label: string = 'dead';
            with dead as (select $label as Name from #A.Entities())
            select Name from #A.Entities()");

        Assert.IsFalse(result.HasErrors, string.Join(" | ", result.Diagnostics));
        Assert.AreEqual(1, result.Warnings.Count(static warning => warning.Code == DiagnosticCode.MQ5022_UnusedCte));
        Assert.AreEqual(1, result.Warnings.Count(static warning => warning.Code == DiagnosticCode.MQ5023_UnusedScriptVariable));
    }

    [TestMethod]
    public void ScriptParameters_AreNotReportedAsUnusedLetVariables()
    {
        var result = Analyze("param(name: string); select Name from #A.Entities()");

        Assert.IsFalse(result.HasErrors, string.Join(" | ", result.Diagnostics));
        Assert.IsFalse(result.Warnings.Any(static warning => warning.Code == DiagnosticCode.MQ5023_UnusedScriptVariable));
    }

    [TestMethod]
    public void UnusedRecursiveCte_IsReportedWithoutTreatingSelfReferenceAsLive()
    {
        var result = Analyze(@"
            with recursive counter (Value) as (
                select Value from values {{ Value: 1 }} seed
                union all
                select c.Value + 1 from counter c where c.Value < 2)
            select Name from #A.Entities()");

        Assert.IsFalse(result.HasErrors, string.Join(" | ", result.Diagnostics));
        Assert.AreEqual(1, result.Warnings.Count(static warning => warning.Code == DiagnosticCode.MQ5022_UnusedCte));
    }

    [TestMethod]
    public void MultipleCteScopes_AreAnalyzedIndependently()
    {
        var result = Analyze(@"
            with first_dead as (select Name from #A.Entities())
            select Name from #A.Entities();
            with second_dead as (select Name from #A.Entities())
            select Name from #A.Entities()");

        Assert.IsFalse(result.HasErrors, string.Join(" | ", result.Diagnostics));
        Assert.AreEqual(2, result.Warnings.Count(static warning => warning.Code == DiagnosticCode.MQ5022_UnusedCte));
    }

    [TestMethod]
    public void DeclarationWarnings_UseDeclarationAndCteSourceSpans()
    {
        const string variableQuery = "let dead: int = 1; select Name from #A.Entities()";
        const string cteQuery = "with dead as (select Name from #A.Entities()) select Name from #A.Entities()";

        var variableResult = Analyze(variableQuery);
        var variableWarning = variableResult.Warnings.Single(warning => warning.Code == DiagnosticCode.MQ5023_UnusedScriptVariable);
        Assert.AreEqual(variableQuery.IndexOf("dead", System.StringComparison.Ordinal), variableWarning.Span.Start);
        Assert.AreEqual(4, variableWarning.Span.Length);

        var cteResult = Analyze(cteQuery);
        var cteWarning = cteResult.Warnings.Single(warning => warning.Code == DiagnosticCode.MQ5022_UnusedCte);
        Assert.AreEqual(cteQuery.IndexOf("dead", System.StringComparison.Ordinal), cteWarning.Span.Start);
        Assert.AreEqual(DiagnosticPhase.Bind, cteWarning.Phase);
    }

    private static QueryAnalysisResult Analyze(string query)
    {
        var provider = new BasicSchemaProvider<BasicEntity>(
            new Dictionary<string, IEnumerable<BasicEntity>>
            {
                ["#A"] = []
            });
        return new QueryAnalyzer(provider).Analyze(query);
    }
}
