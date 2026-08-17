using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class UnreachableBranchAdvisoryTests : BasicEntityTestBase
{
    [TestMethod]
    public void CaseFalseAndNullConditions_ReportUnreachableBranches()
    {
        var result = Analyze(
            "select case when false then 'false' when null then 'null' else 'live' end from #A.Entities()");

        Assert.IsFalse(result.HasErrors, string.Join(" | ", result.Diagnostics));
        Assert.AreEqual(
            2,
            result.Warnings.Count(static warning => warning.Code == DiagnosticCode.MQ5008_UnreachableCode),
            string.Join(" | ", result.Warnings.Select(static warning => warning.ToDetailedString())));
        Assert.IsTrue(result.Warnings.All(static warning => warning.Phase == DiagnosticPhase.Bind));
    }

    [TestMethod]
    public void CaseAlwaysTrueCondition_ReportsOneUnreachableTail()
    {
        var result = Analyze(
            "select case when true then 'first' when Population > 1 then 'second' when false then 'third' else 'fallback' end from #A.Entities()");

        Assert.IsFalse(result.HasErrors, string.Join(" | ", result.Diagnostics));
        Assert.AreEqual(1, result.Warnings.Count(static warning => warning.Code == DiagnosticCode.MQ5008_UnreachableCode));
    }

    [TestMethod]
    public void CaseDuplicateDeterministicCondition_ReportsLaterCondition()
    {
        var result = Analyze(
            "select case when Population = 1 then 'first' when Population = 1 then 'duplicate' else 'fallback' end from #A.Entities()");

        Assert.IsFalse(result.HasErrors, string.Join(" | ", result.Diagnostics));
        Assert.AreEqual(1, result.Warnings.Count(static warning => warning.Code == DiagnosticCode.MQ5008_UnreachableCode));
    }

    [TestMethod]
    public void ConstantScriptCondition_UsesTheSameCaseAnalysis()
    {
        var result = Analyze(
            "let enabled: bool = true; select case when $enabled then 'first' when Population > 1 then 'tail' else 'fallback' end from #A.Entities()");

        Assert.IsFalse(result.HasErrors, string.Join(" | ", result.Diagnostics));
        Assert.AreEqual(1, result.Warnings.Count(static warning => warning.Code == DiagnosticCode.MQ5008_UnreachableCode));
    }

    [TestMethod]
    public void NonNullLiteralAndConstantScriptCoalesce_ReportUnreachableFallbacks()
    {
        var literal = Analyze("select 1 ?? 'unused' from #A.Entities()");
        var script = Analyze("let value: int = 1; select $value ?? 2 from #A.Entities()");

        AssertCode(literal);
        AssertCode(script);
    }

    [TestMethod]
    public void NullableAndNullCoalesceInputs_RemainQuiet()
    {
        var nullable = Analyze("select NullableValue ?? 0 from #A.Entities()");
        var nullLiteral = Analyze("select null ?? Name from #A.Entities()");
        var reference = Analyze("select Name ?? 'fallback' from #A.Entities()");

        Assert.IsFalse(nullable.Warnings.Any(static warning => warning.Code == DiagnosticCode.MQ5008_UnreachableCode));
        Assert.IsFalse(nullLiteral.Warnings.Any(static warning => warning.Code == DiagnosticCode.MQ5008_UnreachableCode));
        Assert.IsFalse(reference.Warnings.Any(static warning => warning.Code == DiagnosticCode.MQ5008_UnreachableCode));
    }

    [TestMethod]
    public void WarningAnalysis_DoesNotChangeCoalesceRuntimeValue()
    {
        var table = CreateAndRunVirtualMachine(
            "select 1 ?? 'unused' as Value from #A.Entities()",
            new Dictionary<string, IEnumerable<BasicEntity>>
            {
                ["#A"] = [new BasicEntity()]
            }).Run();

        Assert.AreEqual(1, table[0][0]);
    }

    private static void AssertCode(QueryAnalysisResult result)
    {
        Assert.IsFalse(result.HasErrors, string.Join(" | ", result.Diagnostics));
        var warnings = result.Warnings.Where(static warning => warning.Code == DiagnosticCode.MQ5008_UnreachableCode).ToArray();
        Assert.AreEqual(1, warnings.Length, string.Join(" | ", result.Diagnostics));
        Assert.AreEqual(DiagnosticPhase.Bind, warnings[0].Phase);
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
