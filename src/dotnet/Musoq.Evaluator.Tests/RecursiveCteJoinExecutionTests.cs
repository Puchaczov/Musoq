using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Tests.Components;
using Musoq.Evaluator.Tests.Schema.RecursiveCte;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class RecursiveCteJoinExecutionTests
{
    private const string InnerJoinQuery =
        "with recursive reachable (Id, Depth) as (" +
        "select RootId, 0 from #graph.roots() " +
        "union (Id) " +
        "select e.TargetId, r.Depth + 1 from reachable r " +
        "inner join #graph.edges() e on e.SourceId = r.Id) " +
        "select Id, Depth from reachable order by Id";

    private const string CrossApplyQuery =
        "with recursive reachable (Id, Depth) as (" +
        "select RootId, 0 from #graph.roots() " +
        "union (Id) " +
        "select e.TargetId, r.Depth + 1 from reachable r " +
        "cross apply #graph.neighbors(r.Id) e) " +
        "select Id, Depth from reachable order by Id";

    private const string UncorrelatedCrossApplyQuery =
        "with recursive reachable (Id, Depth) as (" +
        "select RootId, 0 from #graph.roots() " +
        "union (Id) " +
        "select e.TargetId, r.Depth + 1 from reachable r " +
        "cross apply #graph.edges() e where e.SourceId = r.Id) " +
        "select Id, Depth from reachable order by Id";

    private readonly TestsLoggerResolver _loggerResolver = new();

    [TestMethod]
    public void RecursiveInnerJoinEdges_ShouldTraverseChain()
    {
        var provider = RecursiveGraphFixtures.CreateChainProvider();
        using var compiled = Compile(InnerJoinQuery, provider);

        var table = TableMaterializationTestHelper.Materialize(compiled.Run());

        TableMaterializationTestHelper.AssertRowsInOrder(table, [1, 0], [2, 1], [3, 2]);
        Assert.AreEqual(1, provider.Recorder.Enumerated("roots"));
        Assert.AreEqual(1, provider.Recorder.Enumerated("edges"));
        Assert.AreEqual(1, provider.Recorder.Disposed("edges"));
    }

    [TestMethod]
    public void RecursiveInvariantSource_WhenAnchorIsEmpty_ShouldNotBeOpened()
    {
        var provider = RecursiveGraphFixtures.CreateEmptyRootsProvider();
        using var compiled = Compile(InnerJoinQuery, provider);

        var table = TableMaterializationTestHelper.Materialize(compiled.Run());

        Assert.IsEmpty(table);
        Assert.AreEqual(1, provider.Recorder.Enumerated("roots"));
        Assert.AreEqual(0, provider.Recorder.Created("edges"));
        Assert.AreEqual(0, provider.Recorder.Enumerated("edges"));
        Assert.AreEqual(0, provider.Recorder.Disposed("edges"));
    }

    [TestMethod]
    public void RecursiveInvariantSource_WhenBackingDataChanges_ShouldUseCompletedSnapshot()
    {
        var factoryCalls = 0;
        var provider = new RecursiveGraphSchemaProvider(new RecursiveGraphData
        {
            Roots = [new RecursiveGraphRoot(1)],
            EdgesFactory = () => Interlocked.Increment(ref factoryCalls) == 1
                ?
                [
                    new RecursiveGraphEdge(1, 2, 1m, "one-two"),
                    new RecursiveGraphEdge(2, 3, 1m, "two-three")
                ]
                : [new RecursiveGraphEdge(1, 99, 1m, "changed")]
        });
        using var compiled = Compile(InnerJoinQuery, provider);

        var table = TableMaterializationTestHelper.Materialize(compiled.Run());

        TableMaterializationTestHelper.AssertRowsInOrder(table, [1, 0], [2, 1], [3, 2]);
        Assert.AreEqual(1, factoryCalls);
        Assert.AreEqual(1, provider.Recorder.Enumerated("edges"));
        Assert.AreEqual(1, provider.Recorder.Disposed("edges"));
    }

    [TestMethod]
    public void RecursiveInvariantSource_WhenEnumerationFails_ShouldDisposeOnce()
    {
        var provider = new RecursiveGraphSchemaProvider(new RecursiveGraphData
        {
            Roots = [new RecursiveGraphRoot(1)],
            EdgesFactory = static () => throw new InvalidOperationException("snapshot failed")
        });
        using var compiled = Compile(InnerJoinQuery, provider);

        var exception = Assert.ThrowsExactly<QueryExecutionException>(
            () => TableMaterializationTestHelper.Materialize(compiled.Run()));

        var envelope = exception.Envelope ?? throw new AssertFailedException("Expected a datasource envelope.");
        Assert.AreEqual(DiagnosticCode.MQ7011_DataSourceReadFailed, envelope.Code);
        var preserved = exception.InnerException?.InnerException ??
                        throw new AssertFailedException("The original datasource exception was not preserved.");
        Assert.AreEqual("snapshot failed", preserved.Message);
        Assert.AreEqual(1, provider.Recorder.Enumerated("edges"));
        Assert.AreEqual(1, provider.Recorder.Disposed("edges"));
    }

    [TestMethod]
    public void RecursiveInvariantSource_WhenPartialEnumerationFails_ShouldDisposeOnce()
    {
        var provider = new RecursiveGraphSchemaProvider(new RecursiveGraphData
        {
            Roots = [new RecursiveGraphRoot(1)],
            EdgeChunksFactory = static () => FailingEdgeChunks()
        });
        using var compiled = Compile(InnerJoinQuery, provider);

        var exception = Assert.ThrowsExactly<QueryExecutionException>(
            () => TableMaterializationTestHelper.Materialize(compiled.Run()));

        var envelope = exception.Envelope ?? throw new AssertFailedException("Expected a datasource envelope.");
        Assert.AreEqual(DiagnosticCode.MQ7011_DataSourceReadFailed, envelope.Code);
        var preserved = exception.InnerException?.InnerException ??
                        throw new AssertFailedException("The original datasource exception was not preserved.");
        Assert.AreEqual("snapshot failed after first chunk", preserved.Message);
        Assert.AreEqual(1, provider.Recorder.Enumerated("edges"));
        Assert.AreEqual(1, provider.Recorder.RowsYielded("edges"));
        Assert.AreEqual(1, provider.Recorder.Disposed("edges"));
    }

    [TestMethod]
    public void RecursiveInvariantSource_WhenSnapshotLimitIsReached_ShouldReportMq7009AndDispose()
    {
        var provider = RecursiveGraphFixtures.CreateChainProvider();
        var options = new CompilationOptions(usePrimitiveTypeValidation: false)
            .WithRecursiveCteLimits(new(100, 100, 1));
        using var compiled = InstanceCreator.CompileForExecution(
            InnerJoinQuery,
            Guid.NewGuid().ToString(),
            provider,
            _loggerResolver,
            options);

        var exception = Assert.ThrowsExactly<Musoq.Evaluator.Exceptions.RecursiveCteLimitExceededException>(
            () => TableMaterializationTestHelper.Materialize(compiled.Run()));

        Assert.AreEqual(Musoq.Parser.Diagnostics.DiagnosticCode.MQ7009_RecursiveCteSnapshotLimitExceeded, exception.Code);
        Assert.AreEqual(1, exception.ConfiguredLimit);
        Assert.AreEqual("reachable", exception.CteName);
        Assert.AreEqual(1, provider.Recorder.Enumerated("edges"));
        Assert.AreEqual(2, provider.Recorder.RowsYielded("edges"));
        Assert.AreEqual(1, provider.Recorder.Disposed("edges"));
    }

    [TestMethod]
    public void RecursiveInvariantSource_WhenCancelledDuringSnapshot_ShouldDisposeOnce()
    {
        using var cancellation = new CancellationTokenSource();
        var provider = new RecursiveGraphSchemaProvider(new RecursiveGraphData
        {
            Roots = [new RecursiveGraphRoot(1)],
            EdgeChunksFactory = () => CancellingEdgeChunks(cancellation)
        });
        using var compiled = Compile(InnerJoinQuery, provider);

        Assert.ThrowsExactly<OperationCanceledException>(
            () => TableMaterializationTestHelper.Materialize(compiled.Run(cancellation.Token)));

        Assert.AreEqual(1, provider.Recorder.Enumerated("edges"));
        Assert.AreEqual(2, provider.Recorder.RowsYielded("edges"));
        Assert.AreEqual(1, provider.Recorder.Disposed("edges"));
    }

    [TestMethod]
    public void RecursiveInvariantSource_WhenRowsMutateAfterEnumeration_ShouldUseCopiedScalarValues()
    {
        var first = new RecursiveGraphEdge(1, 2, 1m, "one-two");
        var second = new RecursiveGraphEdge(2, 3, 1m, "two-three");
        var provider = new RecursiveGraphSchemaProvider(new RecursiveGraphData
        {
            Roots = [new RecursiveGraphRoot(1)],
            Edges = [first, second],
            EdgesEnumerationCompleted = () =>
            {
                first.TargetId = 99;
                second.SourceId = 99;
                second.TargetId = 100;
            }
        });
        using var compiled = Compile(InnerJoinQuery, provider);

        var table = TableMaterializationTestHelper.Materialize(compiled.Run());

        TableMaterializationTestHelper.AssertRowsInOrder(table, [1, 0], [2, 1], [3, 2]);
        Assert.AreEqual(1, provider.Recorder.Enumerated("edges"));
        Assert.AreEqual(1, provider.Recorder.Disposed("edges"));
    }

    [TestMethod]
    public void RecursiveUncorrelatedCrossApply_ShouldSnapshotSourceOnce()
    {
        var provider = RecursiveGraphFixtures.CreateChainProvider();
        using var compiled = Compile(UncorrelatedCrossApplyQuery, provider);

        var table = TableMaterializationTestHelper.Materialize(compiled.Run());

        TableMaterializationTestHelper.AssertRowsInOrder(table, [1, 0], [2, 1], [3, 2]);
        Assert.AreEqual(1, provider.Recorder.Created("edges"));
        Assert.AreEqual(1, provider.Recorder.Enumerated("edges"));
        Assert.AreEqual(1, provider.Recorder.Disposed("edges"));
    }

    [TestMethod]
    public void RecursiveUncorrelatedOuterApply_WhenSourceIsEmpty_ShouldSnapshotSourceOnce()
    {
        const string query =
            "with recursive reachable (Id, Depth) as (" +
            "select RootId, 0 from #graph.roots() union (Id) " +
            "select r.Id, r.Depth + 1 from reachable r " +
            "outer apply #graph.edges() e where r.Depth < 1) " +
            "select Id, Depth from reachable order by Id";
        var provider = RecursiveGraphFixtures.CreateEmptyEdgesProvider();
        using var compiled = Compile(query, provider);

        var table = TableMaterializationTestHelper.Materialize(compiled.Run());

        TableMaterializationTestHelper.AssertRowsInOrder(table, [1, 0]);
        Assert.AreEqual(1, provider.Recorder.Created("edges"));
        Assert.AreEqual(1, provider.Recorder.Enumerated("edges"));
        Assert.AreEqual(1, provider.Recorder.Disposed("edges"));
    }

    [TestMethod]
    public void RecursiveCompositeInvariantSubplan_ShouldSnapshotBothSourcesOnce()
    {
        const string query =
            "with recursive reachable (Id, Depth) as (" +
            "select RootId, 0 from #graph.roots() union (Id) " +
            "select e.TargetId, r.Depth + 1 " +
            "from #graph.edges() e " +
            "inner join values {{ Label: 'one-two' }, { Label: 'two-three' }} expected " +
            "on e.Label = expected.Label " +
            "inner join reachable r on e.SourceId = r.Id) " +
            "select Id, Depth from reachable order by Id";
        var provider = RecursiveGraphFixtures.CreateChainProvider();
        var inspection = InstanceCreator.CompileForInspection(
            query,
            Guid.NewGuid().ToString(),
            provider,
            _loggerResolver,
            new CompilationOptions(usePrimitiveTypeValidation: false));
        Assert.AreEqual(1, CountOccurrences(inspection.PhysicalPlanText, "PhysicalSchemaScan [#graph.edges() as e]"));
        Assert.AreEqual(1, CountOccurrences(inspection.PhysicalPlanText, "PhysicalValuesScan [2 rows as expected]"));
        Assert.AreEqual(1, CountOccurrences(inspection.ExecutionPlanText, "CreateValuesRows ["));
        Assert.IsTrue(
            inspection.ExecutionPlanText.IndexOf("CreateValuesRows [", StringComparison.Ordinal) <
            inspection.ExecutionPlanText.IndexOf("RecursiveMember", StringComparison.Ordinal));
        using var compiled = Compile(query, provider);

        var table = TableMaterializationTestHelper.Materialize(compiled.Run());

        TableMaterializationTestHelper.AssertRowsInOrder(table, [1, 0], [2, 1], [3, 2]);
        Assert.AreEqual(1, provider.Recorder.Created("edges"));
        Assert.AreEqual(1, provider.Recorder.Enumerated("edges"));
        Assert.AreEqual(1, provider.Recorder.Disposed("edges"));
    }

    [TestMethod]
    public void RecursiveEarlierMaterializedCte_ShouldReuseStoredRowsWithoutSnapshotCopy()
    {
        const string query =
            "with recursive steps (Amount) as (select Amount from values {{ Amount: 1 }} step), " +
            "counter (Value) as (select 1 from values {{ Seed: 1 }} seed union all " +
            "select c.Value + s.Amount from counter c cross join steps s where c.Value < 3) " +
            "select Value from counter order by Value";
        var inspection = InstanceCreator.CompileForInspection(
            query,
            Guid.NewGuid().ToString(),
            RecursiveGraphFixtures.CreateChainProvider(),
            _loggerResolver,
            new CompilationOptions(usePrimitiveTypeValidation: false));

        Assert.AreEqual(1, CountOccurrences(inspection.GeneratedCSharpCode, "new List<Cte0Row0>"));
        Assert.AreEqual(0, CountOccurrences(inspection.GeneratedCSharpCode, "Invariant0Row0"));
        Assert.AreEqual(2, CountOccurrences(inspection.GeneratedCSharpCode, "_cteRowResults.Slot0"));

        using var compiled = Compile(query, RecursiveGraphFixtures.CreateChainProvider());
        var table = TableMaterializationTestHelper.Materialize(compiled.Run());
        TableMaterializationTestHelper.AssertRowsInOrder(table, [1], [2], [3]);
    }

    [TestMethod]
    public void RecursiveCorrelatedApply_ShouldInvokeSourceForEachAcceptedFrontierRow()
    {
        var provider = RecursiveGraphFixtures.CreateChainProvider();
        using var compiled = Compile(CrossApplyQuery, provider);

        var table = TableMaterializationTestHelper.Materialize(compiled.Run());

        TableMaterializationTestHelper.AssertRowsInOrder(table, [1, 0], [2, 1], [3, 2]);
        Assert.AreEqual(3, provider.Recorder.NeighborInvocations);
        Assert.AreEqual(3, provider.Recorder.Created("neighbors"));
        Assert.AreEqual(3, provider.Recorder.Enumerated("neighbors"));
        Assert.AreEqual(3, provider.Recorder.Disposed("neighbors"));
    }

    [TestMethod]
    public void RecursiveInvariantSourceInspection_ShouldPlaceSnapshotBeforeFixedPointLoop()
    {
        var provider = RecursiveGraphFixtures.CreateChainProvider();
        var inspection = InstanceCreator.CompileForInspection(
            InnerJoinQuery,
            Guid.NewGuid().ToString(),
            provider,
            _loggerResolver,
            new CompilationOptions(usePrimitiveTypeValidation: false));

        Assert.Contains("InvariantSetup", inspection.ExecutionPlanText);
        Assert.Contains(
            "CreateGeneratedRow [cte0Invariant0Row <- Cte0Invariant0Row0",
            inspection.ExecutionPlanText);
        Assert.IsTrue(
            inspection.ExecutionPlanText.IndexOf("InvariantSetup", StringComparison.Ordinal) <
            inspection.ExecutionPlanText.IndexOf("RecursiveMember", StringComparison.Ordinal));
        Assert.AreEqual(1, CountOccurrences(inspection.ExecutionPlanText, "SourceScan [e: RecursiveGraphEdge]"));
        Assert.AreEqual(1, CountOccurrences(inspection.GeneratedCSharpCode, "var cte0Invariant0Hash = new Dictionary<"));
        Assert.AreEqual(0, CountOccurrences(inspection.GeneratedCSharpCode, "MaterializeChunkedRows"));
        Assert.IsTrue(
            inspection.GeneratedCSharpCode.IndexOf("var cte0Invariant0Hash = new Dictionary<", StringComparison.Ordinal) <
            inspection.GeneratedCSharpCode.IndexOf("while (cte0CurrentFrontier.Count > 0)", StringComparison.Ordinal));
    }

    [TestMethod]
    public void UnusedRecursiveCte_WithFailingExternalAnchor_ShouldRemainDead()
    {
        const string query =
            "with recursive dead (Id) as (" +
            "select RootId from #graph.roots() union all " +
            "select d.Id + 1 from dead d where d.Id < 3), " +
            "live (Value) as (select Value from values {{ Value: 42 }} row) " +
            "select Value from live";
        var provider = new RecursiveGraphSchemaProvider(new RecursiveGraphData
        {
            RootsFactory = static () => throw new InvalidOperationException("dead source opened")
        });
        using var compiled = Compile(query, provider);

        var table = TableMaterializationTestHelper.Materialize(compiled.Run());

        TableMaterializationTestHelper.AssertRowsInOrder(table, [42]);
        Assert.AreEqual(0, provider.Recorder.Created("roots"));
        Assert.AreEqual(0, provider.Recorder.Enumerated("roots"));
    }

    private CompiledQuery Compile(string query, RecursiveGraphSchemaProvider provider)
    {
        return InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            provider,
            _loggerResolver,
            new CompilationOptions(usePrimitiveTypeValidation: false));
    }

    private static int CountOccurrences(string text, string value)
    {
        return text.Split(value, StringSplitOptions.None).Length - 1;
    }

    private static IEnumerable<IReadOnlyList<RecursiveGraphEdge>> FailingEdgeChunks()
    {
        yield return [new RecursiveGraphEdge(1, 2, 1m, "one-two")];
        throw new InvalidOperationException("snapshot failed after first chunk");
    }

    private static IEnumerable<IReadOnlyList<RecursiveGraphEdge>> CancellingEdgeChunks(
        CancellationTokenSource cancellation)
    {
        yield return [new RecursiveGraphEdge(1, 2, 1m, "one-two")];
        cancellation.Cancel();
        yield return [new RecursiveGraphEdge(2, 3, 1m, "two-three")];
    }
}
