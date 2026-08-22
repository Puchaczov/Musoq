using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.Tests.IR;

public sealed partial class PhysicalToExecutionPlanBuilderTests
{
    [TestMethod]
    public void Build_WhenPlanHasRowNumberWindow_ShouldReturnWindowExecutionPlan()
    {
        var scan = CreateScan();
        var materialize = new PhysicalMaterializeNode(scan);
        var registration = CreateRowNumberRegistration(new OrderField(new ColumnRef("p", "Name", typeof(string)), Descending: false));
        var window = new PhysicalWindowNode([registration], materialize);
        var project = new PhysicalProjectNode(
            [
                new ProjectedField("Name", new ColumnRef("p", "Name", typeof(string)), 0),
                new ProjectedField("RowNum", new WindowFunctionRef(0, typeof(long)), 1)
            ],
            window);
        var builder = CreateBuilder();

        var result = builder.Build(project, "Q_RowNumberWindow");
        var plan = RequireExecutionPlan(result);

        AssertFinalShapeResult(plan, "result", "ResultRow0", "Name", "RowNum");
        var expected = string.Join("\n",
            "ExecutionPlan [Q_RowNumberWindow]",
            "  Shapes",
            "    SourceEntity [p: Person]",
            "      Name: string <- property Name",
            "      Age: int <- property Age",
            "    Generated [ResultRow0]",
            "      Name: string <- field Name",
            "      RowNum: long <- field RowNum",
            string.Empty,
            "  Body",
            "    SourceScan [p: Person] -> pRows",
            "    MaterializeChunked [pRows -> resultWindowRows]",
            "    ComputeRowNumberWindow [resultRowNumbers <- resultWindowRows order by p.Name ASC]",
            "    CreateShapeRows [result: ResultShape0 from ResultRow0]",
            "    ForEachIndexed [windowIndex, p in resultWindowRows]",
            "      AppendShape [result <- ResultShape0(Name: p.Name, RowNum: resultRowNumbers[windowIndex])]",
            "    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]");

        Assert.AreEqual(expected, PrintPlanWithoutPhaseBoundaries(plan));
    }

    [TestMethod]
    public void Build_WhenRowNumberWindowHasPartitionKey_ShouldReturnWindowExecutionPlan()
    {
        var scan = CreateScan();
        var materialize = new PhysicalMaterializeNode(scan);
        var registration = CreateRowNumberRegistration(
            new OrderField(new ColumnRef("p", "Name", typeof(string)), Descending: false),
            [new ColumnRef("p", "Age", typeof(int))]);
        var window = new PhysicalWindowNode([registration], materialize);
        var project = new PhysicalProjectNode(
            [new ProjectedField("RowNum", new WindowFunctionRef(0, typeof(long)), 0)],
            window);
        var builder = CreateBuilder();

        var result = builder.Build(project, "Q_PartitionedRowNumberWindow");
        var plan = RequireExecutionPlan(result);

        var expected = string.Join("\n",
            "ExecutionPlan [Q_PartitionedRowNumberWindow]",
            "  Shapes",
            "    SourceEntity [p: Person]",
            "      Name: string <- property Name",
            "      Age: int <- property Age",
            "    Generated [ResultRow0]",
            "      RowNum: long <- field RowNum",
            string.Empty,
            "  Body",
            "    SourceScan [p: Person] -> pRows",
            "    MaterializeChunked [pRows -> resultWindowRows]",
            "    ComputeRowNumberWindow [resultRowNumbers <- resultWindowRows partition by p.Age order by p.Name ASC]",
            "    CreateShapeRows [result: ResultShape0 from ResultRow0]",
            "    ForEachIndexed [windowIndex, p in resultWindowRows]",
            "      AppendShape [result <- ResultShape0(RowNum: resultRowNumbers[windowIndex])]",
            "    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]");

        Assert.AreEqual(expected, PrintPlanWithoutPhaseBoundaries(plan));
    }

    [TestMethod]
    public void Build_WhenRowNumberWindowHasMultipleOrderKeys_ShouldReturnWindowExecutionPlan()
    {
        var scan = CreateScan();
        var materialize = new PhysicalMaterializeNode(scan);
        var registration = CreateRowNumberRegistration(
            [
                new OrderField(new ColumnRef("p", "Age", typeof(int)), Descending: true),
                new OrderField(new ColumnRef("p", "Name", typeof(string)), Descending: false)
            ]);
        var window = new PhysicalWindowNode([registration], materialize);
        var project = new PhysicalProjectNode(
            [new ProjectedField("RowNum", new WindowFunctionRef(0, typeof(long)), 0)],
            window);
        var builder = CreateBuilder();

        var result = builder.Build(project, "Q_MultiOrderRowNumberWindow");
        var plan = RequireExecutionPlan(result);

        var expected = string.Join("\n",
            "ExecutionPlan [Q_MultiOrderRowNumberWindow]",
            "  Shapes",
            "    SourceEntity [p: Person]",
            "      Name: string <- property Name",
            "      Age: int <- property Age",
            "    Generated [ResultRow0]",
            "      RowNum: long <- field RowNum",
            string.Empty,
            "  Body",
            "    SourceScan [p: Person] -> pRows",
            "    MaterializeChunked [pRows -> resultWindowRows]",
            "    ComputeRowNumberWindow [resultRowNumbers <- resultWindowRows order by p.Age DESC, p.Name ASC]",
            "    CreateShapeRows [result: ResultShape0 from ResultRow0]",
            "    ForEachIndexed [windowIndex, p in resultWindowRows]",
            "      AppendShape [result <- ResultShape0(RowNum: resultRowNumbers[windowIndex])]",
            "    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]");

        Assert.AreEqual(expected, PrintPlanWithoutPhaseBoundaries(plan));
    }

    [TestMethod]
    public void Build_WhenRowNumberWindowHasPreWindowFilter_ShouldReturnFilteredMaterializationPlan()
    {
        var scan = CreateScan();
        var predicate = new BinaryOp(
            BinaryOpKind.GreaterThan,
            new ColumnRef("p", "Age", typeof(int)),
            new Literal(18, typeof(int)),
            typeof(bool));
        var filter = new PhysicalFilterNode(predicate, scan);
        var materialize = new PhysicalMaterializeNode(filter);
        var registration = CreateRowNumberRegistration(new OrderField(new ColumnRef("p", "Name", typeof(string)), Descending: false));
        var window = new PhysicalWindowNode([registration], materialize);
        var project = new PhysicalProjectNode(
            [new ProjectedField("RowNum", new WindowFunctionRef(0, typeof(long)), 0)],
            window);
        var builder = CreateBuilder();

        var result = builder.Build(project, "Q_FilteredRowNumberWindow");
        var plan = RequireExecutionPlan(result);

        var expected = string.Join("\n",
            "ExecutionPlan [Q_FilteredRowNumberWindow]",
            "  Shapes",
            "    SourceEntity [p: Person]",
            "      Name: string <- property Name",
            "      Age: int <- property Age",
            "    Generated [ResultRow0]",
            "      RowNum: long <- field RowNum",
            string.Empty,
            "  Body",
            "    SourceScan [p: Person] -> pRows",
            "    MaterializeFilteredChunked [pRows where (p.Age > 18) -> resultWindowRows]",
            "    ComputeRowNumberWindow [resultRowNumbers <- resultWindowRows order by p.Name ASC]",
            "    CreateShapeRows [result: ResultShape0 from ResultRow0]",
            "    ForEachIndexed [windowIndex, p in resultWindowRows]",
            "      AppendShape [result <- ResultShape0(RowNum: resultRowNumbers[windowIndex])]",
            "    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]");

        Assert.AreEqual(expected, PrintPlanWithoutPhaseBoundaries(plan));
    }
}
