using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class SubqueryTests
{
    private const string CorrelatedInlineOuterValues =
        "from values { (Label: 'high', Key: 1, Value: 10), (Label: 'medium', Key: 2, Value: 5), (Label: 'low', Key: 3, Value: 3) } p";

    private const string CorrelatedInlineInnerQuery =
        "select q.Number from values { (Key: 1, Number: 8), (Key: 1, Number: 11), (Key: 2, Number: 4), (Key: 3, Number: 5) } q where q.Key = p.Key";

    private const string CorrelatedInlineCaseQuery =
        "select case when p.Value > ANY (" + CorrelatedInlineInnerQuery + ") then 'match' else 'miss' end as Outcome " + CorrelatedInlineOuterValues;

    [TestMethod]
    public void InlineCorrelatedAnyFilter_ShouldRetainOriginalMatchingRows()
    {
        const string query = "select p.Label as Label " + CorrelatedInlineOuterValues + " where p.Value > ANY (" + CorrelatedInlineInnerQuery + ")";
        AssertCorrelatedInlineRows(query, "Label", ["high"], ["medium"]);
    }

    [TestMethod]
    public void InlineCorrelatedAllFilter_ShouldRetainOriginalMatchingRow()
    {
        const string query = "select p.Label as Label " + CorrelatedInlineOuterValues + " where p.Value > ALL (" + CorrelatedInlineInnerQuery + ")";
        AssertCorrelatedInlineRows(query, "Label", ["medium"]);
    }

    [TestMethod]
    [TestProperty("CandidateId", "SPECSTRESS-2026-09-29-P1-0288")]
    public void InlineCorrelatedCaseQuantifier_ShouldReturnMatchingOutcomes()
    {
        AssertCorrelatedInlineRows(CorrelatedInlineCaseQuery, "Outcome", ["match"], ["match"], ["miss"]);
    }

    [TestMethod]
    public void InlineCorrelatedCaseQuantifier_ShouldRetainTypedCapturesUnderCaseGuard()
    {
        AssertCorrelatedInlineRows(CorrelatedInlineCaseQuery, "Outcome", ["match"], ["match"], ["miss"]);
        var inspection = InstanceCreator.CompileForInspection(CorrelatedInlineCaseQuery, Guid.NewGuid().ToString(),
            new BasicSchemaProvider<BasicEntity>(new Dictionary<string, IEnumerable<BasicEntity>>()),
            LoggerResolver, TestCompilationOptions);
        Assert.Contains("-> Apply", inspection.PlanningText);
    }

    [TestMethod]
    public void InlineCorrelatedCaseWithWidenedPartition_ShouldPreserveMatches()
    {
        const string outer = "from values { (Label: 'high', Key: 1s, Value: 10), (Label: 'medium', Key: 2s, Value: 5), (Label: 'low', Key: 3s, Value: 3) } p";
        const string query = "select case when p.Value > ANY (" + CorrelatedInlineInnerQuery + ") then 'match' else 'miss' end as Outcome " + outer;
        AssertCorrelatedInlineRows(query, "Outcome", ["match"], ["match"], ["miss"]);
    }

    [TestMethod]
    public void InlineCorrelatedCaseWithCompositePartition_ShouldPreservePartitionMatches()
    {
        const string query = "select case when p.Value > ANY (select q.Number from values { (Key: 1, SecondKey: 10, Number: 8), (Key: 1, SecondKey: 20, Number: 11), (Key: 3, SecondKey: 30, Number: 5) } q where q.Key = p.Key and q.SecondKey = p.SecondKey) then 'match' else 'miss' end as Outcome from values { (Key: 1, SecondKey: 10, Value: 10), (Key: 1, SecondKey: 20, Value: 10), (Key: 3, SecondKey: 30, Value: 3) } p";
        AssertCorrelatedInlineRows(query, "Outcome", ["match"], ["miss"], ["miss"]);
    }

    private CompiledQuery CompileCorrelatedInline(string query)
    {
        return CreateAndRunVirtualMachine(query, new Dictionary<string, IEnumerable<BasicEntity>>());
    }

    private void AssertCorrelatedInlineRows(string query, string outputName, params object?[][] rows)
    {
        using var compiled = CompileCorrelatedInline(query);
        using var result = compiled.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertColumns(result, (outputName, typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(result, rows);
    }
}
