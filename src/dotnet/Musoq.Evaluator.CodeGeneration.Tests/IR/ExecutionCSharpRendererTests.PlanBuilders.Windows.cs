using System;
using Musoq.Evaluator.IR.Execution;
using Musoq.Plugins;
using Musoq.Plugins.Attributes;

namespace Musoq.Evaluator.Tests.IR;

public sealed partial class ExecutionCSharpRendererTests
{
    private static ExecutionPlan CreateWindowRenderNodePlan()
    {
        var sourceShape = new SourceEntityShape(
            "p",
            typeof(Person),
            [
                new FieldBinding("Name", "p.Name", 0, typeof(string), FieldNullability.Unknown, new ClrPropertyAccess("Name")),
                new FieldBinding("Age", "p.Age", 1, typeof(int), FieldNullability.Unknown, new ClrPropertyAccess("Age"))
            ]);
        var resultShape = new GeneratedRowShape(
            "ResultRow0",
            [
                new FieldBinding("RowNum", "RowNum", 0, typeof(long), FieldNullability.Unknown, new GeneratedFieldAccess("RowNum")),
                new FieldBinding("PrevName", "PrevName", 1, typeof(string), FieldNullability.Unknown, new GeneratedFieldAccess("PrevName")),
                new FieldBinding("RunningAge", "RunningAge", 2, typeof(decimal), FieldNullability.Unknown, new GeneratedFieldAccess("RunningAge"))
            ]);
        var source = new ExecutionVariable("p", typeof(Person));
        var sourceRows = new ExecutionVariable("pRows", typeof(object));
        var buffer = new ExecutionVariable("resultWindowRows", typeof(object));
        var resultTable = new ExecutionVariable("result", typeof(object));
        var windowIndex = new ExecutionVariable("windowIndex", typeof(int));
        var rowNumbers = new ExecutionVariable("resultRowNumbers", typeof(long[]));
        var lags = new ExecutionVariable("resultLags", typeof(string[]));
        var runningAges = new ExecutionVariable("resultWindowSums", typeof(decimal[]));
        var sharedOrderKeys = new ExecutionVariable("resultSharedOrderKeys", typeof(int[]));
        var runningAgePartitions = new ExecutionWindowPartitionSet(
            new ExecutionVariable("resultWindowSumsPartitions", typeof(object)),
            true);
        var runningAgeSortedPartitions = new ExecutionWindowPartitionSet(
            new ExecutionVariable("resultWindowSumsSortedPartitions", typeof(object)),
            true);
        var sourceBinding = new ExecutionSourceBinding(
            "test",
            "data",
            "p:1",
            0,
            [],
            sourceShape.Fields);
        var orderKeys = new[]
        {
            new ExecutionWindowOrderKey(new ExecutionFieldRead("p", "Age", typeof(int)), false)
        };
        var loweredOrderKeys = WindowKeyShapePolicy.CreateOrderKeyArray(sharedOrderKeys.Name, orderKeys);
        var partitionExpression = new ExecutionFieldRead("p", "Name", typeof(string));
        var loweredPartitionKeys = WindowKeyShapePolicy.CreatePartitionKeyArray("resultWindowSumsPartitionKeys", partitionExpression);
        var frame = new ExecutionWindowFrame(
            ExecutionWindowFrameKind.Rows,
            new ExecutionWindowFrameBound(ExecutionWindowFrameBoundKind.UnboundedPreceding, 0),
            new ExecutionWindowFrameBound(ExecutionWindowFrameBoundKind.CurrentRow, 0));
        var sumFactory = typeof(TypedWindowLibrary).GetMethod(nameof(TypedWindowLibrary.WindowRunningAge), [typeof(int)]) ??
                         throw new InvalidOperationException("WindowRunningAge method was not found.");

        return new ExecutionPlan(
            "Q_WindowRenderNodes",
            [sourceShape, resultShape],
            new ExecutionBlock(
            [
                new ExecutionSourceScan(source, sourceRows, sourceBinding),
                new ExecutionMaterializeList(new ExecutionRowStream(sourceRows, ExecutionRowStreamKind.Chunks), buffer),
                new ExecutionComputeRankingWindow(
                    buffer,
                    source,
                    ExecutionRowAccessMode.Direct,
                    null,
                    orderKeys,
                    ExecutionRankingWindowFunction.RowNumber,
                    rowNumbers,
                    null,
                    loweredOrderKeys)
                {
                    LibraryGet = ExecutionClrBindingFactory.FromClr(typeof(RowNumberKernel).GetMethod(nameof(RowNumberKernel.Get))!),
                    RequiresPeerMetadata = false
                },
                new ExecutionComputeOffsetWindow(
                    buffer,
                    source,
                    ExecutionRowAccessMode.Direct,
                    null,
                    orderKeys,
                    new ExecutionFieldRead("p", "Name", typeof(string)),
                    new ExecutionLiteral(1, typeof(int)),
                    new ExecutionLiteral(null, typeof(object)),
                    ExecutionOffsetWindowFunction.Lag,
                    lags,
                    null,
                    loweredOrderKeys with { ShouldExtract = false }),
                new ExecutionWindowAggregateKernel(
                    buffer,
                    source,
                    ExecutionRowAccessMode.Direct,
                    partitionExpression,
                    orderKeys,
                    new ExecutionFieldRead("p", "Age", typeof(int)),
                    null,
                    frame,
                    CreateAgeKernelDescriptor(sumFactory, ExecutionWindowAggregateMode.Running),
                    runningAges,
                    loweredPartitionKeys,
                    loweredOrderKeys with { ShouldExtract = false },
                    runningAgePartitions,
                    runningAgeSortedPartitions) { UseIntegerOrderIndex = true },
                new ExecutionCreateTable(resultTable, resultShape),
                new ExecutionForEachIndexed(
                    source,
                    windowIndex,
                    buffer,
                    ExecutionRowAccessMode.Direct,
                    new ExecutionBlock(
                    [
                        new ExecutionAppendRow(
                            resultTable,
                            resultShape,
                            [
                                new ExecutionRowValue("RowNum", new ExecutionWindowValueRead(rowNumbers, windowIndex, typeof(long))),
                                new ExecutionRowValue("PrevName", new ExecutionWindowValueRead(lags, windowIndex, typeof(string))),
                                new ExecutionRowValue("RunningAge", new ExecutionWindowValueRead(runningAges, windowIndex, typeof(decimal)))
                            ],
                            ExecutionAppendMode.Direct)
                    ])),
                new ExecutionReturnTable(resultTable)
            ]));
    }

    private static ExecutionPlan CreateTypedPluginWindowPlan()
    {
        var sourceShape = new SourceEntityShape(
            "p",
            typeof(Person),
            [
                new FieldBinding("Age", "p.Age", 0, typeof(int), FieldNullability.Unknown, new ClrPropertyAccess("Age"))
            ]);
        var resultShape = new GeneratedRowShape(
            "ResultRow0",
            [
                new FieldBinding("RunningAge", "RunningAge", 0, typeof(decimal), FieldNullability.Unknown, new GeneratedFieldAccess("RunningAge"))
            ]);
        var source = new ExecutionVariable("p", typeof(Person));
        var sourceRows = new ExecutionVariable("pRows", typeof(object));
        var buffer = new ExecutionVariable("resultWindowRows", typeof(object));
        var resultTable = new ExecutionVariable("result", typeof(object));
        var windowIndex = new ExecutionVariable("windowIndex", typeof(int));
        var runningAges = new ExecutionVariable("resultWindowSums", typeof(decimal[]));
        var partitions = new ExecutionWindowPartitionSet(
            new ExecutionVariable("resultWindowSumsPartitions", typeof(object)),
            true);
        var sourceBinding = new ExecutionSourceBinding(
            "test",
            "data",
            "p:1",
            0,
            [],
            sourceShape.Fields);
        var sumFactory = typeof(TypedWindowLibrary).GetMethod(nameof(TypedWindowLibrary.WindowRunningAge), [typeof(int)]) ??
                         throw new InvalidOperationException("WindowRunningAge method was not found.");

        return new ExecutionPlan(
            "Q_TypedPluginWindow",
            [sourceShape, resultShape],
            new ExecutionBlock(
            [
                new ExecutionSourceScan(source, sourceRows, sourceBinding),
                new ExecutionMaterializeList(new ExecutionRowStream(sourceRows, ExecutionRowStreamKind.Chunks), buffer),
                new ExecutionWindowAggregateKernel(
                    buffer,
                    source,
                    ExecutionRowAccessMode.Direct,
                    null,
                    [],
                    new ExecutionFieldRead("p", "Age", typeof(int)),
                    null,
                    null,
                    CreateAgeKernelDescriptor(sumFactory, ExecutionWindowAggregateMode.WholePartition),
                    runningAges,
                    null,
                    null,
                    partitions),
                new ExecutionCreateTable(resultTable, resultShape),
                new ExecutionForEachIndexed(
                    source,
                    windowIndex,
                    buffer,
                    ExecutionRowAccessMode.Direct,
                    new ExecutionBlock(
                    [
                        new ExecutionAppendRow(
                            resultTable,
                            resultShape,
                            [
                                new ExecutionRowValue("RunningAge", new ExecutionWindowValueRead(runningAges, windowIndex, typeof(decimal)))
                            ],
                            ExecutionAppendMode.Direct)
                    ])),
                new ExecutionReturnTable(resultTable)
            ]));
    }

    private static ExecutionWindowAggregateKernelDescriptor CreateAgeKernelDescriptor(System.Reflection.MethodInfo declaration,
        ExecutionWindowAggregateMode mode)
    {
        var kernel = AggregateKernelDescriptor.CreateForWindow(declaration);
        return new ExecutionWindowAggregateKernelDescriptor(ExecutionWindowAggregateFunction.Library, mode,
            typeof(int), typeof(decimal), kernel.StateType)
        {
            LibraryKernel = new ExecutionAggregateKernelOperations(kernel)
        };
    }

    public sealed class TypedWindowLibrary : LibraryBase
    {
        [WindowFunction(typeof(RunningAgeKernel), WindowEvaluationModes.Running | WindowEvaluationModes.WholePartition,
            AggregateEmptyResultBehavior.Zero)]
        public decimal WindowRunningAge(int value) => AggregateFunction.NotInvoked<decimal>();
    }

    public static class RunningAgeKernel
    {
        public struct State { public decimal Sum; }
        public static void Reset(ref State state) => state = default;
        public static void Set(ref State state, int value) => state.Sum += value;
        public static decimal Get(in State state) => state.Sum;
    }
}
