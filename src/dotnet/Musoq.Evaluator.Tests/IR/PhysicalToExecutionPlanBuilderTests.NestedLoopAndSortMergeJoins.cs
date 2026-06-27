using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Logical.Nodes;
using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.Tests.IR;

public sealed partial class PhysicalToExecutionPlanBuilderTests
{

    [TestMethod]
    public void Build_WhenPlanHasInnerNestedLoopJoin_ShouldReturnNestedLoopExecutionPlan()
    {
        var people = CreateScan();
        var orders = CreateOrderScan();
        var predicate = new BinaryOp(
            BinaryOpKind.GreaterThan,
            new ColumnRef("p", "Age", typeof(int)),
            new ColumnRef("o", "PersonAge", typeof(int)),
            typeof(bool));
        var join = new PhysicalNestedLoopJoinNode(JoinKind.Inner, predicate, people, orders);
        var project = new PhysicalProjectNode(
            [
                new ProjectedField("Name", new ColumnRef("p", "Name", typeof(string)), 0),
                new ProjectedField("Description", new ColumnRef("o", "Description", typeof(string)), 1)
            ],
            join);
        var builder = CreateJoinBuilder();

        var result = builder.Build(project, "Q_NestedJoin");
        var plan = RequireExecutionPlan(result);

        var expected = string.Join("\n",
            "ExecutionPlan [Q_NestedJoin]",
            "  Shapes",
            "    SourceEntity [p: Person]",
            "      Name: string <- property Name",
            "      Age: int <- property Age",
            "    SourceEntity [o: Order]",
            "      PersonAge: int <- property PersonAge",
            "      Description: string <- property Description",
            "    Generated [ResultRow0]",
            "      Name: string <- field Name",
            "      Description: string <- field Description",
            string.Empty,
            "  Body",
            "    SourceScan [p: Person] -> pRows",
            "    SourceScan [o: Order] -> oRows",
            "    CreateShapeRows [result: ResultShape0 from ResultRow0]",
            "    MaterializeChunked [oRows -> oRowsBuffer]",
            "    ChunkedForEach [p in pRows]",
            "      ForEach [o in oRowsBuffer]",
            "        If [(p.Age > o.PersonAge)]",
            "          AppendShape [result <- ResultShape0(Name: p.Name, Description: o.Description)]",
            "    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]");

        Assert.AreEqual(expected, ExecutionPlanPrinter.Print(plan));
    }

    [TestMethod]
    public void Build_WhenPlanHasCrossNestedLoopJoin_ShouldReturnCartesianExecutionPlan()
    {
        var people = CreateScan();
        var orders = CreateOrderScan();
        var join = new PhysicalNestedLoopJoinNode(
            JoinKind.Cross,
            new Literal(true, typeof(bool)),
            people,
            orders);
        var project = new PhysicalProjectNode(
            [
                new ProjectedField("Name", new ColumnRef("p", "Name", typeof(string)), 0),
                new ProjectedField("Description", new ColumnRef("o", "Description", typeof(string)), 1)
            ],
            join);
        var builder = CreateJoinBuilder();

        var result = builder.Build(project, "Q_CrossJoin");
        var plan = RequireExecutionPlan(result);

        var expected = string.Join("\n",
            "ExecutionPlan [Q_CrossJoin]",
            "  Shapes",
            "    SourceEntity [p: Person]",
            "      Name: string <- property Name",
            "      Age: int <- property Age",
            "    SourceEntity [o: Order]",
            "      PersonAge: int <- property PersonAge",
            "      Description: string <- property Description",
            "    Generated [ResultRow0]",
            "      Name: string <- field Name",
            "      Description: string <- field Description",
            string.Empty,
            "  Body",
            "    SourceScan [p: Person] -> pRows",
            "    SourceScan [o: Order] -> oRows",
            "    CreateShapeRows [result: ResultShape0 from ResultRow0]",
            "    MaterializeChunked [oRows -> oRowsBuffer]",
            "    ChunkedForEach [p in pRows]",
            "      ForEach [o in oRowsBuffer]",
            "        AppendShape [result <- ResultShape0(Name: p.Name, Description: o.Description)]",
            "    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]");

        Assert.AreEqual(expected, ExecutionPlanPrinter.Print(plan));
    }

    [TestMethod]
    public void Build_WhenPlanHasInnerSortMergeJoin_ShouldReturnRangeProbeExecutionPlan()
    {
        var people = CreateScan();
        var orders = CreateOrderScan();
        var predicate = new BinaryOp(
            BinaryOpKind.GreaterThan,
            new ColumnRef("p", "Age", typeof(int)),
            new ColumnRef("o", "PersonAge", typeof(int)),
            typeof(bool));
        var join = new PhysicalSortMergeJoinNode(
            JoinKind.Inner,
            new ColumnRef("p", "Age", typeof(int)),
            new ColumnRef("o", "PersonAge", typeof(int)),
            BinaryOpKind.GreaterThan,
            predicate,
            people,
            orders);
        var project = new PhysicalProjectNode(
            [
                new ProjectedField("Name", new ColumnRef("p", "Name", typeof(string)), 0),
                new ProjectedField("Description", new ColumnRef("o", "Description", typeof(string)), 1)
            ],
            join);
        var builder = CreateJoinBuilder();

        var result = builder.Build(project, "Q_SortMergeJoin");
        var plan = RequireExecutionPlan(result);

        var expected = string.Join("\n",
            "ExecutionPlan [Q_SortMergeJoin]",
            "  Shapes",
            "    SourceEntity [p: Person]",
            "      Name: string <- property Name",
            "      Age: int <- property Age",
            "    SourceEntity [o: Order]",
            "      PersonAge: int <- property PersonAge",
            "      Description: string <- property Description",
            "    Generated [ResultRow0]",
            "      Name: string <- field Name",
            "      Description: string <- field Description",
            string.Empty,
            "  Body",
            "    SourceScan [p: Person] -> pRows",
            "    SourceScan [o: Order] -> oRows",
            "    CreateShapeRows [result: ResultShape0 from ResultRow0]",
            "    CreateRangeIndex [resultRangeIndex <- oRows by oRangeCandidate.PersonAge >]",
            "    ChunkedForEach [p in pRows]",
            "      RangeProbe [o <- resultRangeIndex where p.Age]",
            "        If [(p.Age > o.PersonAge)]",
            "          AppendShape [result <- ResultShape0(Name: p.Name, Description: o.Description)]",
            "    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]");

        Assert.AreEqual(expected, ExecutionPlanPrinter.Print(plan));
    }

}
