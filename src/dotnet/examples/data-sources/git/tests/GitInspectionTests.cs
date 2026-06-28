using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Diagnostics;

namespace Musoq.Examples.DataSources.Git.Tests;

[TestClass]
public sealed class GitInspectionTests : GitExampleTestBase
{
    [TestMethod]
    public void Inspect_WhenGitPlannerLeavesResidualStatsWork_ShouldIncludePlanningDiagnostics()
    {
        const string query =
            "select g.Subject from #git.commits() g " +
            "where g.AuthorName = 'Bob Evaluator' and g.Additions > 100 " +
            "order by g.AuthoredAt asc take 1";

        var inspection = Inspect(query);

        StringAssert.Contains(inspection.PlanningText, "source plan accepted:");
        StringAssert.Contains(inspection.PlanningText, "source plan residual:");
        StringAssert.Contains(inspection.PlanningText, "source capability predicate:");
        StringAssert.Contains(inspection.PlanningText, "-> Partial");
        StringAssert.Contains(inspection.PlanningText, "source capability slicing:");
        StringAssert.Contains(inspection.PlanningText, "-> Rejected");
        StringAssert.Contains(inspection.PlanningText, "source plan diagnostic [TryPlanSource]: Warning");
        StringAssert.Contains(inspection.PlanningText, "Git stats columns are loaded lazily");
        Assert.IsTrue(inspection.Warnings.Any(warning =>
            warning.Code == DiagnosticCode.MQ5013_SourceContractWarning &&
            warning.Message.Contains("GitPredicatePushdown", StringComparison.Ordinal)));
        Assert.IsTrue(inspection.Warnings.Any(warning =>
            warning.Code == DiagnosticCode.MQ5013_SourceContractWarning &&
            warning.Message.Contains("GitSlicePushdown", StringComparison.Ordinal)));
        Assert.IsFalse(inspection.Warnings.Any(warning =>
            warning.Code == DiagnosticCode.MQ5012_OptimizationFallback));
    }

    [TestMethod]
    public void Inspect_WhenGitQueryIsGenerated_ShouldUseTypedGitCommitRowAccess()
    {
        const string query =
            "select g.ShortSha, g.Subject from #git.commits('musoq') g " +
            "where g.AuthorName = 'Bob Evaluator' order by g.AuthoredAt desc take 1";

        var inspection = Inspect(query);

        StringAssert.Contains(inspection.GeneratedCSharpCode, "GitCommitRow");
        StringAssert.Contains(
            inspection.GeneratedCSharpCode,
            "GetRowSource<Musoq.Examples.DataSources.Git.GitCommitRow>");
        StringAssert.Contains(inspection.GeneratedCSharpCode, ".ShortSha");
        StringAssert.Contains(inspection.GeneratedCSharpCode, ".Subject");
        Assert.IsFalse(inspection.GeneratedCSharpCode.Contains("GetColumnValue", StringComparison.Ordinal));
        Assert.IsFalse(inspection.GeneratedCSharpCode.Contains("GetProperty", StringComparison.Ordinal));
        Assert.IsFalse(inspection.GeneratedCSharpCode.Contains("[\"ShortSha\"]", StringComparison.Ordinal));
        Assert.IsFalse(inspection.GeneratedCSharpCode.Contains("[\"Subject\"]", StringComparison.Ordinal));
    }
}
