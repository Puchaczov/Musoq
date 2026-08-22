using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Physical.Nodes;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Tests.IR;

public sealed partial class PhysicalToExecutionPlanBuilderTests
{
    [TestMethod]
    public void Build_WhenPlanHasFirstValueWindow_ShouldReturnWindowExecutionPlan()
    {
        var scan = CreateScan();
        var materialize = new PhysicalMaterializeNode(scan);
        var registration = CreateFirstValueRegistration(
            new ColumnRef("p", "Name", typeof(string)),
            [new OrderField(new ColumnRef("p", "Age", typeof(int)), Descending: false)]);
        var window = new PhysicalWindowNode([registration], materialize);
        var project = new PhysicalProjectNode(
            [new ProjectedField("FirstName", new WindowFunctionRef(0, typeof(object)), 0)],
            window);
        var builder = CreateBuilder();

        var result = builder.Build(project, "Q_FirstValueWindow");
        var plan = RequireExecutionPlan(result);

        var expected = string.Join("\n",
            "ExecutionPlan [Q_FirstValueWindow]",
            "  Shapes",
            "    SourceEntity [p: Person]",
            "      Name: string <- property Name",
            "      Age: int <- property Age",
            "    Generated [ResultRow0]",
            "      FirstName: object <- field FirstName",
            string.Empty,
            "  Body",
            "    SourceScan [p: Person] -> pRows",
            "    MaterializeChunked [pRows -> resultWindowRows]",
            "    ComputeFirstValueWindow [resultFirstValues <- resultWindowRows value p.Name order by p.Age ASC frame range between unbounded preceding and current row]",
            "    CreateShapeRows [result: ResultShape0 from ResultRow0]",
            "    ForEachIndexed [windowIndex, p in resultWindowRows]",
            "      AppendShape [result <- ResultShape0(FirstName: resultFirstValues[windowIndex])]",
            "    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]");

        Assert.AreEqual(expected, PrintPlanWithoutPhaseBoundaries(plan));
    }

    [TestMethod]
    public void Build_WhenPlanHasFramedFirstValueWindow_ShouldReturnWindowExecutionPlan()
    {
        var scan = CreateScan();
        var materialize = new PhysicalMaterializeNode(scan);
        var registration = CreateFirstValueRegistration(
            new ColumnRef("p", "Name", typeof(string)),
            [new OrderField(new ColumnRef("p", "Age", typeof(int)), Descending: false)],
            new WindowFrameNode(
                WindowFrameType.Rows,
                new WindowFrameBoundNode(WindowFrameBoundType.OffsetPreceding, 1),
                new WindowFrameBoundNode(WindowFrameBoundType.OffsetFollowing, 1)));
        var window = new PhysicalWindowNode([registration], materialize);
        var project = new PhysicalProjectNode(
            [new ProjectedField("FirstName", new WindowFunctionRef(0, typeof(object)), 0)],
            window);
        var builder = CreateBuilder();

        var result = builder.Build(project, "Q_FramedFirstValueWindow");
        var plan = RequireExecutionPlan(result);

        var expected = string.Join("\n",
            "ExecutionPlan [Q_FramedFirstValueWindow]",
            "  Shapes",
            "    SourceEntity [p: Person]",
            "      Name: string <- property Name",
            "      Age: int <- property Age",
            "    Generated [ResultRow0]",
            "      FirstName: object <- field FirstName",
            string.Empty,
            "  Body",
            "    SourceScan [p: Person] -> pRows",
            "    MaterializeChunked [pRows -> resultWindowRows]",
            "    ComputeFirstValueWindow [resultFirstValues <- resultWindowRows value p.Name order by p.Age ASC frame rows between 1 preceding and 1 following]",
            "    CreateShapeRows [result: ResultShape0 from ResultRow0]",
            "    ForEachIndexed [windowIndex, p in resultWindowRows]",
            "      AppendShape [result <- ResultShape0(FirstName: resultFirstValues[windowIndex])]",
            "    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]");

        Assert.AreEqual(expected, PrintPlanWithoutPhaseBoundaries(plan));
    }

    [TestMethod]
    public void Build_WhenRowNumberWindowHasQualifyPredicate_ShouldReturnFilteredWindowExecutionPlan()
    {
        var scan = CreateScan();
        var materialize = new PhysicalMaterializeNode(scan);
        var registration = CreateRowNumberRegistration(new OrderField(new ColumnRef("p", "Name", typeof(string)), Descending: false));
        var window = new PhysicalWindowNode([registration], materialize);
        var qualify = new PhysicalQualifyFilterNode(
            new BinaryOp(
                BinaryOpKind.LessOrEqual,
                new WindowFunctionRef(0, typeof(long)),
                new Literal(1L, typeof(long)),
                typeof(bool)),
            window);
        var project = new PhysicalProjectNode(
            [new ProjectedField("Name", new ColumnRef("p", "Name", typeof(string)), 0)],
            qualify);
        var builder = CreateBuilder();

        var result = builder.Build(project, "Q_RowNumberWindowQualify");
        var plan = RequireExecutionPlan(result);

        var expected = string.Join("\n",
            "ExecutionPlan [Q_RowNumberWindowQualify]",
            "  Shapes",
            "    SourceEntity [p: Person]",
            "      Name: string <- property Name",
            "      Age: int <- property Age",
            "    Generated [ResultRow0]",
            "      Name: string <- field Name",
            string.Empty,
            "  Body",
            "    SourceScan [p: Person] -> pRows",
            "    MaterializeChunked [pRows -> resultWindowRows]",
            "    ComputeRowNumberWindow [resultRowNumbers <- resultWindowRows order by p.Name ASC qualify <= 1]",
            "    CreateShapeRows [result: ResultShape0 from ResultRow0]",
            "    ForEachIndexed [windowIndex, p in resultWindowRows]",
            "      If [((resultRowNumbers[windowIndex] > 0) AND (resultRowNumbers[windowIndex] <= 1))]",
            "        AppendShape [result <- ResultShape0(Name: p.Name)]",
            "    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]");

        Assert.AreEqual(expected, PrintPlanWithoutPhaseBoundaries(plan));
    }
}
