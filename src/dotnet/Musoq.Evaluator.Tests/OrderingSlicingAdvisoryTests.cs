using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class OrderingSlicingAdvisoryTests
{
    [TestMethod]
    [DataRow("union")]
    [DataRow("union all")]
    [DataRow("except")]
    [DataRow("intersect")]
    public void SetOperation_RightmostOrderBy_ReportsOneScopeWarning(string setOperator)
    {
        var query =
            $"select Name from #A.Entities() {setOperator} select Name from #A.Entities() order by Name";
        var result = Analyze(query);

        AssertNoErrors(result);
        var warnings = result.Warnings.Where(static warning =>
            warning.Code == DiagnosticCode.MQ5020_SetOperationOrderByScope).ToArray();
        Assert.AreEqual(1, warnings.Length, string.Join(" | ", result.Diagnostics));
        Assert.AreEqual(DiagnosticSeverity.Warning, warnings[0].Severity);
        Assert.AreEqual(DiagnosticPhase.Bind, warnings[0].Phase);
        Assert.IsTrue(warnings[0].Span.Start >= query.IndexOf("order by", System.StringComparison.Ordinal));
    }

    [TestMethod]
    public void SetOperation_Chain_ReportsOnlyTheRightmostScope()
    {
        var result = Analyze(
            "select Name from #A.Entities() union select Name from #A.Entities() union all select Name from #A.Entities() order by Name");

        AssertNoErrors(result);
        Assert.AreEqual(
            1,
            result.Warnings.Count(static warning => warning.Code == DiagnosticCode.MQ5020_SetOperationOrderByScope),
            string.Join(" | ", result.Diagnostics));
    }

    [TestMethod]
    public void SetOperation_BranchSlicing_MakesOrderMaterial()
    {
        var take = Analyze(
            "select Name from #A.Entities() union select Name from #A.Entities() order by Name take 1");
        var skip = Analyze(
            "select Name from #A.Entities() union select Name from #A.Entities() order by Name skip 1");
        var skipZero = Analyze(
            "select Name from #A.Entities() union select Name from #A.Entities() order by Name skip 0");
        var takeOnly = Analyze(
            "select Name from #A.Entities() order by Name take 1");

        AssertNoErrors(take);
        AssertNoErrors(skip);
        AssertNoErrors(skipZero);
        AssertNoErrors(takeOnly);
        Assert.IsFalse(take.Warnings.Any(static warning => warning.Code == DiagnosticCode.MQ5020_SetOperationOrderByScope));
        Assert.IsFalse(skip.Warnings.Any(static warning => warning.Code == DiagnosticCode.MQ5020_SetOperationOrderByScope));
        Assert.AreEqual(1, skipZero.Warnings.Count(static warning =>
            warning.Code == DiagnosticCode.MQ5020_SetOperationOrderByScope));
        Assert.IsFalse(takeOnly.Warnings.Any(static warning =>
            warning.Code == DiagnosticCode.MQ5020_SetOperationOrderByScope));
    }

    [TestMethod]
    public void SetOperation_OuterOrderedConsumer_SuppressesBranchScopeWarning()
    {
        var result = Analyze(
            """
            with combined as (
                select Name from #A.Entities()
                union
                select Name from #A.Entities() order by Name
            )
            select Name from combined order by Name
            """);

        AssertNoErrors(result);
        Assert.IsFalse(result.Warnings.Any(static warning =>
            warning.Code == DiagnosticCode.MQ5020_SetOperationOrderByScope),
            string.Join(" | ", result.Diagnostics));
    }

    [TestMethod]
    public void DerivedSetWrappedByOrderedOuterQuery_SuppressesBranchScopeWarning()
    {
        var result = Analyze(
            """
            select Name
            from (
                select Name from #A.Entities()
                union
                select Name from #A.Entities() order by Name
            ) combined
            order by Name
            """);

        AssertNoErrors(result);
        Assert.IsFalse(result.Warnings.Any(static warning =>
            warning.Code == DiagnosticCode.MQ5020_SetOperationOrderByScope),
            string.Join(" | ", result.Diagnostics));
    }

    [TestMethod]
    public void PositiveSkipWithoutOrderBy_ReportsOneWarningPerQuery()
    {
        var result = Analyze(
            "select Name from #A.Entities() skip 2 union select Name from #A.Entities() skip 3");

        AssertNoErrors(result);
        Assert.AreEqual(
            2,
            result.Warnings.Count(static warning => warning.Code == DiagnosticCode.MQ5021_UnorderedSkip),
            string.Join(" | ", result.Diagnostics));
    }

    [TestMethod]
    public void ZeroSkip_TakeOnly_AndOrderedSkip_RemainQuiet()
    {
        var zero = Analyze("select Name from #A.Entities() skip 0");
        var take = Analyze("select Name from #A.Entities() take 2");
        var ordered = Analyze("select Name from #A.Entities() order by Name skip 2");

        AssertNoErrors(zero);
        AssertNoErrors(take);
        AssertNoErrors(ordered);
        Assert.IsFalse(zero.Warnings.Any(static warning => warning.Code == DiagnosticCode.MQ5021_UnorderedSkip));
        Assert.IsFalse(take.Warnings.Any(static warning => warning.Code == DiagnosticCode.MQ5021_UnorderedSkip));
        Assert.IsFalse(ordered.Warnings.Any(static warning => warning.Code == DiagnosticCode.MQ5021_UnorderedSkip));
    }

    [TestMethod]
    public void NestedOrderedQuery_AndAliasOrdering_DoNotActivateDormantWarning()
    {
        var result = Analyze(
            "select Name as Label from #A.Entities() order by Label");

        AssertNoErrors(result);
        Assert.IsFalse(result.Warnings.Any(static warning =>
            warning.Code == DiagnosticCode.MQ5020_SetOperationOrderByScope));
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

    private static void AssertNoErrors(QueryAnalysisResult result)
    {
        Assert.IsFalse(result.HasErrors, string.Join(" | ", result.Diagnostics));
    }
}
