using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.Tests.IR;

public sealed partial class PhysicalToExecutionPlanBuilderTests
{

    [TestMethod]
    public void Build_WhenPlanHasRankWindow_ShouldReturnWindowExecutionPlan()
    {
        var scan = CreateScan();
        var materialize = new PhysicalMaterializeNode(scan);
        var registration = CreateRankRegistration(new OrderField(new ColumnRef("p", "Age", typeof(int)), Descending: true));
        var window = new PhysicalWindowNode([registration], materialize);
        var project = new PhysicalProjectNode(
            [new ProjectedField("Rank", new WindowFunctionRef(0, typeof(long)), 0)],
            window);
        var builder = CreateBuilder();

        var result = builder.Build(project, "Q_RankWindow");
        var plan = RequireExecutionPlan(result);

        var expected = string.Join("\n",
            "ExecutionPlan [Q_RankWindow]",
            "  Shapes",
            "    SourceEntity [p: Person]",
            "      Name: string <- property Name",
            "      Age: int <- property Age",
            "    Generated [ResultRow0]",
            "      Rank: long <- field Rank",
            string.Empty,
            "  Body",
            "    SourceScan [p: Person] -> pRows",
            "    MaterializeChunked [pRows -> resultWindowRows]",
            "    ComputeRankWindow [resultRanks <- resultWindowRows order by p.Age DESC]",
            "    CreateShapeRows [result: ResultShape0 from ResultRow0]",
            "    ForEachIndexed [windowIndex, p in resultWindowRows]",
            "      AppendShape [result <- ResultShape0(Rank: resultRanks[windowIndex])]",
            "    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]");

        Assert.AreEqual(expected, ExecutionPlanPrinter.Print(plan));
    }

    [TestMethod]
    public void Build_WhenPlanHasDenseRankWindow_ShouldReturnWindowExecutionPlan()
    {
        var scan = CreateScan();
        var materialize = new PhysicalMaterializeNode(scan);
        var registration = CreateDenseRankRegistration(
            [new OrderField(new ColumnRef("p", "Name", typeof(string)), Descending: false)],
            [new ColumnRef("p", "Age", typeof(int))]);
        var window = new PhysicalWindowNode([registration], materialize);
        var project = new PhysicalProjectNode(
            [new ProjectedField("DenseRank", new WindowFunctionRef(0, typeof(long)), 0)],
            window);
        var builder = CreateBuilder();

        var result = builder.Build(project, "Q_DenseRankWindow");
        var plan = RequireExecutionPlan(result);

        var expected = string.Join("\n",
            "ExecutionPlan [Q_DenseRankWindow]",
            "  Shapes",
            "    SourceEntity [p: Person]",
            "      Name: string <- property Name",
            "      Age: int <- property Age",
            "    Generated [ResultRow0]",
            "      DenseRank: long <- field DenseRank",
            string.Empty,
            "  Body",
            "    SourceScan [p: Person] -> pRows",
            "    MaterializeChunked [pRows -> resultWindowRows]",
            "    ComputeDenseRankWindow [resultDenseRanks <- resultWindowRows partition by p.Age order by p.Name ASC]",
            "    CreateShapeRows [result: ResultShape0 from ResultRow0]",
            "    ForEachIndexed [windowIndex, p in resultWindowRows]",
            "      AppendShape [result <- ResultShape0(DenseRank: resultDenseRanks[windowIndex])]",
            "    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]");

        Assert.AreEqual(expected, ExecutionPlanPrinter.Print(plan));
    }

    [TestMethod]
    public void Build_WhenPlanHasLagWindow_ShouldReturnWindowExecutionPlan()
    {
        var scan = CreateScan();
        var materialize = new PhysicalMaterializeNode(scan);
        var registration = CreateLagRegistration(
            new ColumnRef("p", "Age", typeof(int)),
            [new OrderField(new ColumnRef("p", "Name", typeof(string)), Descending: false)],
            [],
            [new Literal(1, typeof(int)), new Literal(0, typeof(int))]);
        var window = new PhysicalWindowNode([registration], materialize);
        var project = new PhysicalProjectNode(
            [new ProjectedField("PrevAge", new WindowFunctionRef(0, typeof(object)), 0)],
            window);
        var builder = CreateBuilder();

        var result = builder.Build(project, "Q_LagWindow");
        var plan = RequireExecutionPlan(result);

        var expected = string.Join("\n",
            "ExecutionPlan [Q_LagWindow]",
            "  Shapes",
            "    SourceEntity [p: Person]",
            "      Name: string <- property Name",
            "      Age: int <- property Age",
            "    Generated [ResultRow0]",
            "      PrevAge: object <- field PrevAge",
            string.Empty,
            "  Body",
            "    SourceScan [p: Person] -> pRows",
            "    MaterializeChunked [pRows -> resultWindowRows]",
            "    ComputeLagWindow [resultLags <- resultWindowRows value p.Age order by p.Name ASC offset 1 default 0]",
            "    CreateShapeRows [result: ResultShape0 from ResultRow0]",
            "    ForEachIndexed [windowIndex, p in resultWindowRows]",
            "      AppendShape [result <- ResultShape0(PrevAge: resultLags[windowIndex])]",
            "    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]");

        Assert.AreEqual(expected, ExecutionPlanPrinter.Print(plan));
    }

    [TestMethod]
    public void Build_WhenPlanHasLeadWindowWithPartition_ShouldReturnWindowExecutionPlan()
    {
        var scan = CreateScan();
        var materialize = new PhysicalMaterializeNode(scan);
        var registration = CreateLeadRegistration(
            new ColumnRef("p", "Name", typeof(string)),
            [new OrderField(new ColumnRef("p", "Name", typeof(string)), Descending: false)],
            [new ColumnRef("p", "Age", typeof(int))],
            [new Literal(1, typeof(int)), new Literal("missing", typeof(string))]);
        var window = new PhysicalWindowNode([registration], materialize);
        var project = new PhysicalProjectNode(
            [new ProjectedField("NextName", new WindowFunctionRef(0, typeof(object)), 0)],
            window);
        var builder = CreateBuilder();

        var result = builder.Build(project, "Q_LeadWindow");
        var plan = RequireExecutionPlan(result);

        var expected = string.Join("\n",
            "ExecutionPlan [Q_LeadWindow]",
            "  Shapes",
            "    SourceEntity [p: Person]",
            "      Name: string <- property Name",
            "      Age: int <- property Age",
            "    Generated [ResultRow0]",
            "      NextName: object <- field NextName",
            string.Empty,
            "  Body",
            "    SourceScan [p: Person] -> pRows",
            "    MaterializeChunked [pRows -> resultWindowRows]",
            "    ComputeLeadWindow [resultLeads <- resultWindowRows value p.Name partition by p.Age order by p.Name ASC offset 1 default 'missing']",
            "    CreateShapeRows [result: ResultShape0 from ResultRow0]",
            "    ForEachIndexed [windowIndex, p in resultWindowRows]",
            "      AppendShape [result <- ResultShape0(NextName: resultLeads[windowIndex])]",
            "    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]");

        Assert.AreEqual(expected, ExecutionPlanPrinter.Print(plan));
    }

    [TestMethod]
    public void Build_WhenPlanHasMultipleSupportedWindows_ShouldReturnWindowExecutionPlan()
    {
        var scan = CreateScan();
        var materialize = new PhysicalMaterializeNode(scan);
        var orderFields = new[] { new OrderField(new ColumnRef("p", "Age", typeof(int)), Descending: false) };
        var rowNumberRegistration = CreateRowNumberRegistration(orderFields);
        var lagRegistration = CreateLagRegistration(
            new ColumnRef("p", "Age", typeof(int)),
            orderFields,
            [],
            [],
            windowIndex: 1);
        var window = new PhysicalWindowNode([rowNumberRegistration, lagRegistration], materialize);
        var project = new PhysicalProjectNode(
            [
                new ProjectedField("RowNum", new WindowFunctionRef(0, typeof(long)), 0),
                new ProjectedField("PrevAge", new WindowFunctionRef(1, typeof(object)), 1)
            ],
            window);
        var builder = CreateBuilder();

        var result = builder.Build(project, "Q_MultipleWindows");
        var plan = RequireExecutionPlan(result);

        var expected = string.Join("\n",
            "ExecutionPlan [Q_MultipleWindows]",
            "  Shapes",
            "    SourceEntity [p: Person]",
            "      Name: string <- property Name",
            "      Age: int <- property Age",
            "    Generated [ResultRow0]",
            "      RowNum: long <- field RowNum",
            "      PrevAge: object <- field PrevAge",
            string.Empty,
            "  Body",
            "    SourceScan [p: Person] -> pRows",
            "    MaterializeChunked [pRows -> resultWindowRows]",
            "    ComputeRowNumberWindow [resultRowNumbers0 <- resultWindowRows order by p.Age ASC]",
            "    ComputeLagWindow [resultLags1 <- resultWindowRows value p.Age order by p.Age ASC offset 1 default NULL]",
            "    CreateShapeRows [result: ResultShape0 from ResultRow0]",
            "    ForEachIndexed [windowIndex, p in resultWindowRows]",
            "      AppendShape [result <- ResultShape0(RowNum: resultRowNumbers0[windowIndex], PrevAge: resultLags1[windowIndex])]",
            "    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]");

        Assert.AreEqual(expected, ExecutionPlanPrinter.Print(plan));

        var rowNumber = CollectNodes<ExecutionComputeRankingWindow>(plan.Body).Single();
        var lag = CollectNodes<ExecutionComputeOffsetWindow>(plan.Body).Single();

        Assert.IsNotNull(rowNumber.OrderKeyArray);
        Assert.IsNotNull(lag.OrderKeyArray);
        Assert.IsTrue(rowNumber.OrderKeyArray.ShouldExtract);
        Assert.IsFalse(lag.OrderKeyArray.ShouldExtract);
        Assert.AreEqual(rowNumber.OrderKeyArray.Variable, lag.OrderKeyArray.Variable);
        Assert.AreEqual(typeof(int[]), rowNumber.OrderKeyArray.Variable.Type.ResolveClrType());
        Assert.AreEqual(typeof(int), rowNumber.OrderKeyArray.Shape?.ElementType.ResolveClrType());
        Assert.IsTrue(rowNumber.OrderKeyArray.Shape?.IsTyped);
    }

}
