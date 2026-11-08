using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Execution;

namespace Musoq.Evaluator.Tests.IR;

[TestClass]
public sealed class ExecutionPlanInventoryTests
{
    [TestMethod]
    public void CountSlots_WhenPlanIsNull_ShouldReturnZero()
    {
        Assert.AreEqual(0, ExecutionPlanInventory.CountTableSlots(null));
        Assert.AreEqual(0, ExecutionPlanInventory.CountCteIndexSlots(null));
    }

    [TestMethod]
    public void CountTableSlots_ShouldIncludeParallelTasksAndStoredTableRowExpressions()
    {
        var table = new ExecutionVariable("table", typeof(object));
        var row = new ExecutionVariable("row", typeof(object));
        var plan = new ExecutionPlan(
            "Q_TableSlots",
            [],
            new ExecutionBlock(
            [
                new ExecutionParallelBlock(
                    "parallel",
                    2,
                    [
                        new ExecutionParallelTask(
                            "task",
                            new ExecutionVariable("taskRows", typeof(object)),
                            new ExecutionBlock([new ExecutionStoreTable(table, 4)]))
                    ],
                    new ExecutionParallelMerge(new ExecutionBlock(
                    [
                        new ExecutionForEach(
                            row,
                            new ExecutionStoredTableRows(7),
                            new ExecutionBlock([new ExecutionStoreTable(table, 2)]))
                    ])))
            ]));

        Assert.AreEqual(7, ExecutionPlanInventory.FindMaxTableIndex(plan.Body));
        Assert.AreEqual(8, ExecutionPlanInventory.CountTableSlots(plan));
    }

    [TestMethod]
    public void CountCteIndexSlots_ShouldIncludeNestedProbeBodies()
    {
        var index = new ExecutionVariable("index", typeof(object));
        var matches = new ExecutionVariable("matches", typeof(object));
        var plan = new ExecutionPlan(
            "Q_CteSlots",
            [],
            new ExecutionBlock(
            [
                new ExecutionHashProbe(
                    index,
                    matches,
                    new ExecutionLiteral(1, typeof(int)),
                    typeof(int),
                    typeof(object),
                    new ExecutionBlock([new ExecutionStoreCteIndex(index, 3, ExecutionCteSidecarIndexKind.KeySet, typeof(int))]),
                    new ExecutionBlock([new ExecutionLoadCteIndex(index, 5, ExecutionCteSidecarIndexKind.KeySet, typeof(int))]))
            ]));

        Assert.AreEqual(5, ExecutionPlanInventory.FindMaxCteIndexSlot(plan.Body));
        Assert.AreEqual(6, ExecutionPlanInventory.CountCteIndexSlots(plan));
    }

    [TestMethod]
    public void SetOperationLoweringModels_ShouldPreserveArmShapeAndNames()
    {
        var rowShape = new GeneratedRowShape("SetRow", []);
        var item = new ExecutionVariable("item", typeof(object));
        var loop = new ExecutionForEach(
            item,
            new ExecutionStoredTableRows(1),
            ExecutionBlock.Empty);
        var setup = new ExecutionNode[] { new ExecutionContinue() };

        var arm = new StreamingUnionAllArm(rowShape, setup, loop);
        var names = new SetOperationArmNames("leftTable", "LeftRow", "rightTable", "RightRow");

        Assert.AreSame(rowShape, arm.SourceShape);
        Assert.AreSame(setup, arm.Setup);
        Assert.AreSame(loop, arm.Loop);
        Assert.AreEqual("leftTable", names.LeftTableName);
        Assert.AreEqual("LeftRow", names.LeftShapeName);
        Assert.AreEqual("rightTable", names.RightTableName);
        Assert.AreEqual("RightRow", names.RightShapeName);
    }
}
