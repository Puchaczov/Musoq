using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Components;
using Musoq.Evaluator.Tests.Schema.RecursiveCte;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class RecursiveGraphFixtureTests
{
    private readonly TestsLoggerResolver _loggerResolver = new();

    [TestMethod]
    public void GraphSources_WhenQueriedDirectly_ShouldReturnRowsAndRecordLifecycle()
    {
        var provider = new RecursiveGraphSchemaProvider(new RecursiveGraphData
        {
            Roots = [new RecursiveGraphRoot(1)],
            Edges =
            [
                new RecursiveGraphEdge(1, 2, 1m, "one-two"),
                new RecursiveGraphEdge(2, 3, 2m, "two-three")
            ]
        });

        var roots = Execute("select RootId from #graph.roots()", provider);
        var edges = Execute("select SourceId, TargetId from #graph.edges() order by SourceId", provider);
        var neighbors = Execute("select TargetId from #graph.neighbors(1)", provider);

        TableMaterializationTestHelper.AssertRowsInOrder(roots, [1]);
        TableMaterializationTestHelper.AssertRowsInOrder(edges, [1, 2], [2, 3]);
        TableMaterializationTestHelper.AssertRowsInOrder(neighbors, [2]);
        Assert.AreEqual(1, provider.Recorder.Created("roots"));
        Assert.AreEqual(1, provider.Recorder.Enumerated("roots"));
        Assert.AreEqual(1, provider.Recorder.Disposed("roots"));
        Assert.AreEqual(2, provider.Recorder.RowsYielded("edges"));
        Assert.AreEqual(1, provider.Recorder.NeighborInvocations);
    }

    private Tables.Table Execute(string query, RecursiveGraphSchemaProvider provider)
    {
        using var queryInstance = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            provider,
            _loggerResolver,
            new CompilationOptions(usePrimitiveTypeValidation: false));

        return TableMaterializationTestHelper.Materialize(queryInstance.Run());
    }
}
