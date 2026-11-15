using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Wildcard;

namespace Musoq.Evaluator.Tests;

public sealed partial class ProjectionSensitiveWildcardTests
{
    [TestMethod]
    [TestCategory("QueryInventoryCompilerReproduction")]
    public void DistinctWildcard_ShouldPreserveProviderDiscoveryAndSourceColumnIndexes()
    {
        const string projection = "a.* exclude (Other) replace (a.Score + 1 as Score) rename (Name as Person)";
        const string source = "from #wildcard.rows() a where a.Other = 'source-column'";
        var controlRecorder = new ProjectionSensitiveWildcardRecorder();
        var distinctRecorder = new ProjectionSensitiveWildcardRecorder();
        using var control = Compile($"select {projection} {source}", controlRecorder);
        using var compiled = Compile($"select distinct {projection} {source}", distinctRecorder);
        using var controlResult = control.Run();
        using var result = compiled.Run();

        TableMaterializationTestHelper.AssertColumns(
            result,
            ("Id", typeof(int)),
            ("Person", typeof(string)),
            ("Score", typeof(int))
        );
        TableMaterializationTestHelper.AssertRowsInOrder(result, [1, "Ada", 11]);
        TableMaterializationTestHelper.AssertRowsInOrder(controlResult, [1, "Ada", 11]);
        Assert.AreEqual(
            controlRecorder.MetadataRequests.Count(static columns => columns.Length == 0),
            distinctRecorder.MetadataRequests.Count(static columns => columns.Length == 0),
            "Synthetic DISTINCT grouping must not add full-schema provider probes."
        );
        CollectionAssert.AreEquivalent(
            controlRecorder.QueryScopedShapes.SelectMany(static shape => shape).Distinct().ToArray(),
            distinctRecorder.QueryScopedShapes.SelectMany(static shape => shape).Distinct().ToArray()
        );
        Assert.IsEmpty(distinctRecorder.ExecutionContexts);
        Assert.IsNotEmpty(distinctRecorder.QueryScopedExecutionContexts);
    }
}
