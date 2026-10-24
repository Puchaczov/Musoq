using System.Linq;
using Musoq.Parser.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Schema;
using Musoq.Tests.Common;

namespace Musoq.Converter.Tests;

public partial class QueryInspectionTests
{
    [TestMethod]
    [DataRow("skip $offset take $limit")]
    [DataRow("skip $offset take 2")]
    public void RuntimePaging_ShouldRemainResidualWhenProviderAcceptsEveryRequest(string paging)
    {
        var provider = new PlanningSchemaProvider(static request => SourcePlanResult.AcceptAll(request), CreatePlanningRows());
        var sql = $"params(offset: int = 1, limit: int = 2) select p.Name from #planning.items() p order by p.Name {paging}";
        using var query = CompileForExecution(sql, provider);
        var table = query.Run();
        CollectionAssert.AreEqual(new[] { "bravo", "charlie" }, table.Select(static row => (string)row.ReadValue(0)!).ToArray());
        Assert.IsTrue(provider.Requests.All(static request => request.Skip == null && request.Take == null));
        NativeParameterTestInputs.Set(query.TypedParameters, query.ParameterDefinitions, "offset", 2);
        CollectionAssert.AreEqual(new[] { "charlie", "delta" }, query.Run().Select(static row => (string)row.ReadValue(0)!).ToArray());
    }

    [TestMethod]
    [DataRow("take $limit", "PhysicalTopN")]
    [DataRow("skip $offset take $limit", "PhysicalTopOffset")]
    public void RuntimePaging_WithRejectedSourcePlan_ShouldRetainBoundedOrdering(string paging, string strategy)
    {
        var provider = new PlanningSchemaProvider(static request => SourcePlanResult.RejectAll(request));
        var inspection = Inspect($"params(offset: int = 1, limit: int = 2) select p.Name from #planning.items() p order by p.Name {paging}", provider);
        StringAssert.Contains(inspection.PhysicalPlanText, strategy);
        StringAssert.Contains(inspection.GeneratedCSharpCode, "BoundedTopRecordList");
        StringAssert.Contains(inspection.ExecutionPlanText, "$limit");
    }
    [TestMethod]
    [DataRow("select p.Name from #planning.items() p skip $offset")]
    [DataRow("select p.Name from #planning.items() p union all select q.Name from #planning.items() q skip $offset")]
    public void UnorderedRuntimeSkip_DefaultZero_ShouldWarnBecauseValueCanChange(string body)
    {
        var provider = new PlanningSchemaProvider(static request => SourcePlanResult.RejectAll(request));
        var inspection = Inspect($"param(offset: int = 0) {body}", provider);
        Assert.IsTrue(inspection.Warnings.Any(static warning => warning.Code == DiagnosticCode.MQ5021_UnorderedSkip));
    }

    [TestMethod]
    public void UnorderedLetSkip_KnownZero_ShouldRemainQuiet()
    {
        var provider = new PlanningSchemaProvider(static request => SourcePlanResult.RejectAll(request));
        var inspection = Inspect("let offset: long = 0L select p.Name from #planning.items() p skip $offset", provider);
        Assert.IsFalse(inspection.Warnings.Any(static warning => warning.Code == DiagnosticCode.MQ5021_UnorderedSkip));
    }
}
