using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Benchmarks.Scenarios;
using Musoq.Converter;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;
using Musoq.Schema.Optimization;
using Musoq.Schema;

namespace Musoq.Benchmarks.Tests;

[TestClass]
public sealed class CostStrategyTests
{
    [TestMethod]
    [DataRow("B01_Scan_Unordered")]
    [DataRow("B01_Wide_Unordered")]
    public void UnorderedProjection_HasNoPhysicalSort(string id)
        => Assert.IsFalse(PhysicalNodes(Inspect(id).PhysicalPlan).Any(node => node is PhysicalSortNode));

    [TestMethod]
    public void StringKeyJoin_SelectsAStringHashKey()
    {
        var join = PhysicalNodes(Inspect("B07_StringKeyJoin").PhysicalPlan).OfType<PhysicalHashJoinNode>().Single();
        Assert.HasCount(1, join.BuildKeys);
        Assert.AreEqual(typeof(string), join.BuildKeys[0].ReturnType);
    }

    [TestMethod]
    public void RangeExistence_SelectsAnEqualityPartitionedRangeStrategy()
    {
        var join = PhysicalNodes(Inspect("B10_PartitionedRangeExists").PhysicalPlan).OfType<PhysicalSortMergeJoinNode>().Single();
        Assert.HasCount(1, join.LeftPartitionKeys);
        Assert.HasCount(1, join.RightPartitionKeys);
    }

    [TestMethod]
    public void StableAndVolatileControls_UseDifferentScalarReusePaths()
    {
        AssertCallsPerRowLoop(Inspect("B03_StableScalarReuse"), "StableWork", 1);
        AssertCallsPerRowLoop(Inspect("B03_VolatileScalarControl"), "VolatileWork", 2);
    }

    [TestMethod]
    [DataRow("B26_ComputedProjection_Accepted", 1)]
    [DataRow("B26_ComputedProjection_Residual", 0)]
    public void ComputedProjectionPair_UsesTheNegotiatedTypedFields(string id, int accepted)
    {
        var benchmark = Inspect(id);
        var originalScan = PhysicalNodes(benchmark.PhysicalPlan).OfType<PhysicalSchemaScanNode>().Single();
        Assert.AreEqual("Id", originalScan.SourceTransferStrategy!.Shape!.Fields.Single().Name);
        StringAssert.Contains(benchmark.GeneratedCSharpCode, "StrictCastRuntime.ToInt64");

        var source = accepted > 0 ? "accepted" : "residual";
        var inspection = Inspect(id, $"select r.Id::long * 2 as Value from computed.{source}() r");
        var scan = PhysicalNodes(inspection.PhysicalPlan).OfType<PhysicalSchemaScanNode>().Single();
        var shape = scan.SourceTransferStrategy!.Shape!;
        Assert.AreEqual(accepted, shape.Fields.Count(field => field.Origin == QueryRowFieldOrigin.ComputedProjection));
        if (accepted > 0)
        {
            Assert.IsFalse(inspection.GeneratedCSharpCode.Contains("StrictCastRuntime.ToInt64", StringComparison.Ordinal));
            Assert.AreEqual(QueryRowFieldOrigin.ComputedProjection, shape.Fields.Single().Origin);
        }
        else
            StringAssert.Contains(inspection.GeneratedCSharpCode, "StrictCastRuntime.ToInt64");
    }

    [TestMethod]
    public void ApplyInvariant_ComputesOncePerOuterRowBeforeItemIteration()
    {
        var root = CSharpSyntaxTree.ParseText(Inspect("B13_ApplyInvariant").GeneratedCSharpCode).GetRoot();
        var calls = root.DescendantNodes().OfType<InvocationExpressionSyntax>()
            .Where(call => call.Expression is MemberAccessExpressionSyntax member && member.Name.Identifier.ValueText == "StableWork").ToArray();
        Assert.IsNotEmpty(calls);
        foreach (var call in calls)
        {
            var outerLoop = call.Ancestors().OfType<ForStatementSyntax>().Single();
            var declaration = call.Ancestors().OfType<LocalDeclarationStatementSyntax>().First();
            Assert.IsTrue(outerLoop.DescendantNodes().OfType<ForStatementSyntax>()
                .Any(inner => inner.SpanStart > declaration.Span.End));
        }
    }

    [TestMethod]
    public void ScalarFixtures_DenyConcurrentRowProcessingInBothControls()
    {
        var scenario = ScenarioCatalog.Get("B03_StableScalarReuse");
        var schema = FixtureCatalog.Prepare(scenario, 4).CreateSchemaProvider().GetSchema("perf");
        using var cancellation = new CancellationTokenSource();
        var metadata = new SourceMetadataContext("scalar", cancellation.Token, [],
            new Dictionary<string, string>(), Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance);
        var descriptor = schema.DescribeSource("rows", new SourceDescribeContext(SourceIdentity.Empty, metadata));
        Assert.AreEqual(SourceExecutionPermissions.None, descriptor.ExecutionPermissions);
    }

    private static QueryInspectionResult Inspect(string id, string? sql = null)
    {
        var scenario = ScenarioCatalog.Get(id);
        return InstanceCreator.CompileForInspection(sql ?? scenario.Sql, "cost-inspection",
            FixtureCatalog.Prepare(scenario, 1000).CreateSchemaProvider(), BenchmarkLogger.Instance, ExecutionSettings.Create(scenario, 1000));
    }

    private static void AssertCallsPerRowLoop(QueryInspectionResult inspection, string name, int expected)
    {
        var methods = CSharpSyntaxTree.ParseText(inspection.GeneratedCSharpCode).GetRoot().DescendantNodes()
            .OfType<MethodDeclarationSyntax>().Where(method => method.Identifier.ValueText.StartsWith("ComputeShapeRows_", StringComparison.Ordinal));
        var loops = methods.SelectMany(method => method.DescendantNodes().OfType<ForStatementSyntax>())
            .Where(loop => loop.DescendantNodes().OfType<YieldStatementSyntax>()
                .Any(statement => ReferenceEquals(statement.Ancestors().OfType<ForStatementSyntax>().First(), loop))).ToArray();
        Assert.IsNotEmpty(loops);
        foreach (var loop in loops)
            Assert.AreEqual(expected, loop.DescendantNodes().OfType<InvocationExpressionSyntax>()
                .Count(call => call.Expression is MemberAccessExpressionSyntax member && member.Name.Identifier.ValueText == name));
    }

    private static IEnumerable<PhysicalNode> PhysicalNodes(PhysicalNode node)
    {
        yield return node;
        foreach (var child in node.Children)
            foreach (var descendant in PhysicalNodes(child))
                yield return descendant;
    }
}
