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
    public void SetOperation_ResultModifiers_DoNotReportRetiredScopeWarnings(string setOperator)
    {
        var query =
            $"select Name as Result from #A.Entities() {setOperator} select City as Other from #A.Entities() order by Result skip 0 take 1";
        var result = Analyze(query);

        AssertNoErrors(result);
        Assert.IsFalse(result.Warnings.Any(static warning =>
            warning.Code is DiagnosticCode.MQ5020_SetOperationOrderByScope or
                DiagnosticCode.MQ5026_SetOperationSliceScope),
            string.Join(" | ", result.Diagnostics));
    }

    [TestMethod]
    public void SetOperation_Chain_DoesNotReportRetiredScopeWarnings()
    {
        var result = Analyze(
            "select Name from #A.Entities() union select Name from #A.Entities() union all select Name from #A.Entities() order by Name skip 1 take 2");

        AssertNoErrors(result);
        Assert.IsFalse(result.Warnings.Any(static warning =>
                warning.Code is DiagnosticCode.MQ5020_SetOperationOrderByScope or
                    DiagnosticCode.MQ5026_SetOperationSliceScope),
            string.Join(" | ", result.Diagnostics));
    }

    [TestMethod]
    public void SetOperation_ResultOrderBy_ShouldRejectRightOperandAliasAndSourceQualifier()
    {
        var rightAlias = Analyze(
            "select Name as Result from #A.Entities() union select City as Other from #A.Entities() order by Other");
        var sourceQualifier = Analyze(
            "select a.Name as Result from #A.Entities() a union select b.City from #A.Entities() b order by b.City");

        Assert.IsTrue(rightAlias.HasErrors);
        Assert.IsTrue(rightAlias.Errors.Any(static error => error.Code == DiagnosticCode.MQ3001_UnknownColumn));
        Assert.IsTrue(sourceQualifier.HasErrors);
        Assert.IsTrue(sourceQualifier.Errors.Any(static error => error.Code == DiagnosticCode.MQ3015_UnknownAlias));
    }

    [TestMethod]
    public void ExplicitCteOperand_PreservesBranchLocalSliceWithoutMigrationWarning()
    {
        var result = Analyze(
            "with sliced as (select Name from #A.Entities() order by Name take 1) select Name from #A.Entities() union select Name from sliced");

        AssertNoErrors(result);
        Assert.IsFalse(result.Warnings.Any(static warning =>
            warning.Code is DiagnosticCode.MQ5020_SetOperationOrderByScope or
                DiagnosticCode.MQ5026_SetOperationSliceScope),
            string.Join(" | ", result.Diagnostics));
    }

    [TestMethod]
    public void PositiveSkipWithoutOrderBy_ReportsOneWarningPerQuery()
    {
        var result = Analyze(
            "with first_slice as (select Name from #A.Entities() skip 2), second_slice as (select Name from #A.Entities() skip 3) select Name from first_slice union select Name from second_slice");

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
