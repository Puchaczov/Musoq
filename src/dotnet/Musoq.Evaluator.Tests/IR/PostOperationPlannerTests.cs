using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Expressions;

namespace Musoq.Evaluator.Tests.IR;

[TestClass]
public sealed class PostOperationPlannerTests
{
    [TestMethod]
    public void CreatePostOperations_WhenSortSkipTakeAreCollectedInPhysicalOrder_ShouldReverseAttachProjectionAndCombineSlice()
    {
        var planner = new PostOperationPlanner();
        var projectedFields = ProjectedFields();
        var operations = new List<PostOperation>
        {
            new TakeOperation(10),
            new SkipOperation(5),
            new SortOperation([Order("Age", typeof(int), descending: true)])
        };

        var planned = planner.CreatePostOperations(operations, projectedFields);

        Assert.HasCount(2, planned);
        var sort = Assert.IsInstanceOfType<SortOperation>(planned[0]);
        Assert.AreSame(projectedFields, sort.ProjectedFields);
        Assert.AreEqual("Age", ((ColumnRef)sort.Keys[0].Expression).ColumnName);
        var slice = Assert.IsInstanceOfType<SliceOperation>(planned[1]);
        Assert.AreEqual(5, slice.SkipCount);
        Assert.AreEqual(10, slice.TakeCount);
    }

    [TestMethod]
    public void TryCreateStreamingSlice_WhenSingleSliceCanStream_ShouldCreateCountersAndClearRemainingOperations()
    {
        var planner = new PostOperationPlanner((name, index) => $"{name}_{index}");
        IReadOnlyList<PostOperation> operations = [new SliceOperation(3, 7)];

        var streamingSlice = planner.TryCreateStreamingSlice(
            "result",
            operations,
            isDistinct: false,
            finalProjection: null,
            ProjectedFields(),
            out var remaining);

        Assert.IsNotNull(streamingSlice);
        Assert.AreEqual(3, streamingSlice.SkipCount);
        Assert.AreEqual(7, streamingSlice.TakeCount);
        Assert.AreEqual("__resultSkipRemaining_0", streamingSlice.SkipRemaining?.Name);
        Assert.AreEqual("__resultTakeRemaining_0", streamingSlice.TakeRemaining?.Name);
        Assert.IsEmpty(remaining);

        var declarations = planner.CreateStreamingSliceCounterDeclarations(streamingSlice).ToArray();
        Assert.HasCount(2, declarations);
        var skipLet = Assert.IsInstanceOfType<ExecutionLet>(declarations[0]);
        Assert.AreEqual("__resultSkipRemaining_0", skipLet.Variable.Name);
        Assert.AreEqual(3, ((ExecutionLiteral)skipLet.Value).Value.ToClrValue());
        var takeLet = Assert.IsInstanceOfType<ExecutionLet>(declarations[1]);
        Assert.AreEqual("__resultTakeRemaining_0", takeLet.Variable.Name);
        Assert.AreEqual(7, ((ExecutionLiteral)takeLet.Value).Value.ToClrValue());
    }

    [TestMethod]
    public void TryCreateStreamingSlice_WhenFinalProjectionIsRequired_ShouldLeavePostOperations()
    {
        var planner = new PostOperationPlanner();
        IReadOnlyList<PostOperation> operations = [new TakeOperation(5)];
        var shape = CreateShape();
        var finalProjection = new TableProjection(new ExecutionVariable("result", typeof(object)), shape, [0]);

        var streamingSlice = planner.TryCreateStreamingSlice(
            "result",
            operations,
            isDistinct: false,
            finalProjection,
            ProjectedFields(),
            out var remaining);

        Assert.IsNull(streamingSlice);
        Assert.AreSame(operations, remaining);
    }

    [TestMethod]
    public void CreatePostOperation_WhenOrderKeyCannotResolve_ShouldPropagateUnsupportedReason()
    {
        var planner = new PostOperationPlanner();
        var operation = new SortOperation(
            [Order("Missing", typeof(string))],
            ProjectedFields());

        var result = planner.CreatePostOperation(operation, new ExecutionVariable("rows", typeof(object)), CreateShape());

        Assert.IsFalse(result.IsBuilt);
        StringAssert.Contains(result.UnsupportedReason, "cannot resolve order key");
        StringAssert.Contains(result.UnsupportedReason, "Missing");
    }

    [TestMethod]
    public void CreatePostOperation_WhenOperationIsUnknown_ShouldReturnUnsupportedResult()
    {
        var planner = new PostOperationPlanner();

        var result = planner.CreatePostOperation(new UnknownPostOperation(), new ExecutionVariable("rows", typeof(object)), CreateShape());

        Assert.IsFalse(result.IsBuilt);
        StringAssert.Contains(result.UnsupportedReason, "UnknownPostOperation");
    }

    [TestMethod]
    public void CreatePostOperation_WhenPostOperationsAreSupported_ShouldCreateExpectedNodesAndColumnMetadata()
    {
        var planner = new PostOperationPlanner();
        var source = new ExecutionVariable("rows", typeof(object));
        var shape = CreateShape();
        var projectedFields = ProjectedFields();
        var order = new[] { Order("Age", typeof(int), descending: true) };

        var sort = AssertSupportedNode<ExecutionSortTable>(
            planner.CreatePostOperation(new SortOperation(order, projectedFields), source, shape));
        Assert.AreEqual("rowsSorted", sort.Target.Name);
        Assert.AreEqual("Age", sort.Keys[0].FieldName);
        Assert.IsTrue(sort.Keys[0].Descending);
        AssertMetadata(sort.ColumnMetadata, "rowsSorted");

        var topN = AssertSupportedNode<ExecutionTopNTable>(
            planner.CreatePostOperation(new TopNOperation(4, order, projectedFields), source, shape));
        Assert.AreEqual("rowsTopN", topN.Target.Name);
        Assert.AreEqual(4, topN.Count);
        AssertMetadata(topN.ColumnMetadata, "rowsTopN");

        var topOffset = AssertSupportedNode<ExecutionTopOffsetTable>(
            planner.CreatePostOperation(new TopOffsetOperation(2, 4, order, projectedFields), source, shape));
        Assert.AreEqual("rowsTopOffset", topOffset.Target.Name);
        Assert.AreEqual(2, topOffset.SkipCount);
        Assert.AreEqual(4, topOffset.TakeCount);
        Assert.AreEqual(ExecutionTopOffsetStrategy.BoundedHeap, topOffset.Strategy);
        AssertMetadata(topOffset.ColumnMetadata, "rowsTopOffset");

        var skip = AssertSupportedNode<ExecutionSkipTable>(
            planner.CreatePostOperation(new SkipOperation(2), source, shape));
        Assert.AreEqual("rowsSkipped", skip.Target.Name);
        Assert.AreEqual(2, skip.Count);
        AssertMetadata(skip.ColumnMetadata, "rowsSkipped");

        var take = AssertSupportedNode<ExecutionTakeTable>(
            planner.CreatePostOperation(new TakeOperation(4), source, shape));
        Assert.AreEqual("rowsTaken", take.Target.Name);
        Assert.AreEqual(4, take.Count);
        AssertMetadata(take.ColumnMetadata, "rowsTaken");

        var slice = AssertSupportedNode<ExecutionSliceTable>(
            planner.CreatePostOperation(new SliceOperation(2, 4), source, shape));
        Assert.AreEqual("rowsSliced", slice.Target.Name);
        Assert.AreEqual(2, slice.SkipCount);
        Assert.AreEqual(4, slice.TakeCount);
        AssertMetadata(slice.ColumnMetadata, "rowsSliced");
    }

    private static TNode AssertSupportedNode<TNode>(PostOperationResult result)
        where TNode : ExecutionNode
    {
        Assert.IsTrue(result.IsBuilt);

        return Assert.IsInstanceOfType<TNode>(result.Node);
    }

    private static void AssertMetadata(ExecutionColumnMetadata? metadata, string referenceName)
    {
        Assert.IsNotNull(metadata);
        Assert.AreEqual(referenceName, metadata.ReferenceName);
        Assert.AreEqual(ExecutionColumnMetadataKind.TableColumns, metadata.Kind);
        Assert.HasCount(2, metadata.Fields);
        Assert.AreEqual("Name", metadata.Fields[0].Name);
        Assert.AreEqual(0, metadata.Fields[0].Index);
        Assert.AreEqual(typeof(string), metadata.Fields[0].Type.ResolveClrType());
        Assert.AreEqual("Age", metadata.Fields[1].Name);
        Assert.AreEqual(1, metadata.Fields[1].Index);
        Assert.AreEqual(typeof(int), metadata.Fields[1].Type.ResolveClrType());
    }

    private static ProjectedField[] ProjectedFields() =>
    [
        new("Name", new ColumnRef(string.Empty, "Name", typeof(string)), 0),
        new("Age", new ColumnRef(string.Empty, "Age", typeof(int)), 1)
    ];

    private static OrderField Order(string columnName, Type type, bool descending = false) =>
        new(new ColumnRef(string.Empty, columnName, type), descending, NullOrdering.Last);

    private static GeneratedRowShape CreateShape() =>
        new(
            "ResultRow",
            [
                Field("Name", 0, typeof(string)),
                Field("Age", 1, typeof(int))
            ]);

    private static FieldBinding Field(string name, int index, Type type) =>
        new(name, name, index, type, FieldNullability.Unknown, new GeneratedFieldAccess(name));

    private sealed record UnknownPostOperation : PostOperation;
}
