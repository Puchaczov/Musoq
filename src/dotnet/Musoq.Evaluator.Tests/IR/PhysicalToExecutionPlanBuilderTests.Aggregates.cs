using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.Tests.IR;

public sealed partial class PhysicalToExecutionPlanBuilderTests
{
    [TestMethod]
    public void Build_WhenPlanHasSingleKeyAggregate_ShouldReturnAggregateExecutionPlan()
    {
        var scan = CreateScan();
        var binding = CreateCountBinding();
        var aggregate = new PhysicalSingleKeyAggregateNode(
            new ColumnRef("p", "Name", typeof(string)),
            "p.Name",
            typeof(string),
            [binding],
            scan);
        var project = new PhysicalProjectNode(
            [
                new ProjectedField("Name", new ColumnRef("p", "Name", typeof(string)), 0),
                new ProjectedField("Count", new AggregateRef("p.Count", typeof(long)), 1)
            ],
            aggregate);
        var builder = CreateSerialBuilder();

        var result = builder.Build(project, "Q_SingleKeyAggregate");
        var plan = RequireExecutionPlan(result);
        AssertFinalShapeResult(plan, "result", "ResultRow0", "Name", "Count");

        var expected = string.Join("\n",
            "ExecutionPlan [Q_SingleKeyAggregate]",
            "  Shapes",
            "    SourceEntity [p: Person]",
            "      Name: string <- property Name",
            "      Age: int <- property Age",
            "    AggregateGroup [ResultAggregateGroup; keys: 1; typed aggs: 1]",
            "    Generated [ResultRow0]",
            "      Name: string <- field Name",
            "      Count: long <- field Count_",
            string.Empty,
            "  Body",
            "    SourceScan [p: Person] -> pRows",
            "    CreateShapeRows [result: ResultShape0 from ResultRow0]",
            "    CreateSingleKeyAggregateContext [groups: string -> ResultAggregateGroup]",
            "    ChunkedForEach [p in pRows]",
            "      GetOrAddSingleKeyAggregateGroup [group = groups[p.Name] by p.Name; typed: ResultAggregateGroup]",
            "      TypedAggregateSet [Set(group.__agg0, 1)]",
            "    EnsureShapeCapacity [result <- Candidate(result <- groupsToFinalize.Count)]",
            "    ForEach [finalGroup in groupsToFinalize]",
            "      AppendShape [result <- ResultShape0(Name: finalGroup.p.Name, Count: Count('p.Count'))]",
            "    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]");

        Assert.AreEqual(expected, PrintPlanWithoutPhaseBoundaries(plan));
    }

    [TestMethod]
    public void Build_WhenParallelizationModeIsFullForMergeableSingleKeyAggregate_ShouldReturnParallelAggregateExecutionPlan()
    {
        var scan = CreateScan();
        var binding = CreateCountBinding();
        var aggregate = new PhysicalSingleKeyAggregateNode(
            new ColumnRef("p", "Name", typeof(string)),
            "p.Name",
            typeof(string),
            [binding],
            scan);
        var project = new PhysicalProjectNode(
            [
                new ProjectedField("Name", new ColumnRef("p", "Name", typeof(string)), 0),
                new ProjectedField("Count", new AggregateRef("p.Count", typeof(long)), 1)
            ],
            aggregate);
        var builder = CreateBuilder();

        var result = builder.Build(project, "Q_ParallelSingleKeyAggregate");
        var plan = RequireExecutionPlan(result);
        var text = ExecutionPlanPrinter.Print(plan);

        Assert.Contains("ParallelSingleKeyAggregateLoop [p in pRows by p.Name; threshold 4096, sample 8192/6144", text);
        Assert.Contains("ParallelAccumulate", text);
        Assert.Contains("TypedAggregateSet [Set(group.__agg0, 1)]", text);
        Assert.IsFalse(text.Contains("SequentialKernel", StringComparison.Ordinal));
        Assert.Contains("ForEach [finalGroup in groupsToFinalize]", text);
    }

    [TestMethod]
    public void Build_WhenFinalStatementProjectsSingleKeyAggregate_ShouldFinalizeDirectlyIntoResult()
    {
        var scan = CreateScan();
        var binding = CreateCountBinding();
        var aggregate = new PhysicalSingleKeyAggregateNode(
            new ColumnRef("p", "Name", typeof(string)),
            "p.Name",
            typeof(string),
            [binding],
            scan);
        var aggregateProject = new PhysicalProjectNode(
            [
                new ProjectedField("p.Name", new ColumnRef("p", "Name", typeof(string)), 0),
                new ProjectedField("p.Count", new AggregateRef("p.Count", typeof(long)), 1)
            ],
            aggregate);
        var cteRef = new PhysicalCteRefNode(
            "pScore",
            "pScore",
            new OutputSchema(
            [
                new ColumnSchema("p.Name", typeof(string), 0),
                new ColumnSchema("p.Count", typeof(long), 1)
            ]));
        var finalProject = new PhysicalProjectNode(
            [
                new ProjectedField("Name", new ColumnRef("p", "Name", typeof(string)), 0),
                new ProjectedField("Count", new ColumnRef("p", "Count", typeof(long)), 1)
            ],
            cteRef);
        var multiStatement = new PhysicalMultiStatementNode(
        [
            aggregateProject,
            new PhysicalTakeNode(2, new PhysicalSkipNode(1, finalProject))
        ]);
        var builder = CreateSerialBuilder();

        var result = builder.Build(multiStatement, "Q_FinalAggregateProjection");
        var plan = RequireExecutionPlan(result);
        var planText = ExecutionPlanPrinter.Print(plan);

        var expected = string.Join("\n",
            "ExecutionPlan [Q_FinalAggregateProjection]",
            "  Shapes",
            "    SourceEntity [p: Person]",
            "      Name: string <- property Name",
            "      Age: int <- property Age",
            "    AggregateGroup [ResultAggregateGroup; keys: 1; typed aggs: 1]",
            "    Generated [ResultRow0]",
            "      Name: string <- field Name",
            "      Count: long <- field Count_",
            string.Empty,
            "  Body",
            "    SourceScan [p: Person] -> pRows",
            "    CreateShapeRows [result: ResultShape0 from ResultRow0]",
            "    CreateSingleKeyAggregateContext [groups: string -> ResultAggregateGroup]",
            "    ChunkedForEach [p in pRows]",
            "      GetOrAddSingleKeyAggregateGroup [group = groups[p.Name] by p.Name; typed: ResultAggregateGroup]",
            "      TypedAggregateSet [Set(group.__agg0, 1)]",
            "    EnsureShapeCapacity [result <- Candidate(result <- groupsToFinalize.Count)]",
            "    ForEach [finalGroup in groupsToFinalize]",
            "      AppendShape [result <- ResultShape0(Name: finalGroup.p.Name, Count: Count('p.Count'))]",
            "    SliceShapeRows [result -> resultSliced, skip 1, take 2; capacity: Candidate(resultSliced <- Min(Max(result.Count - 1, 0), 2))]",
            "    ReturnDeferredTable [resultSliced: ResultRow0 <- ResultShape0]");

        Assert.AreEqual(expected, PrintPlanWithoutPhaseBoundaries(plan));
        Assert.IsFalse(planText.Contains("StoreTable", StringComparison.Ordinal));
        Assert.IsFalse(planText.Contains("Statement0Row0", StringComparison.Ordinal));
    }

    [TestMethod]
    public void Build_WhenFinalStatementProjectsCaseOverAggregate_ShouldKeepMaterializedCtePath()
    {
        var scan = CreateScan();
        var binding = CreateCountBinding();
        var aggregate = new PhysicalSingleKeyAggregateNode(
            new ColumnRef("p", "Name", typeof(string)),
            "p.Name",
            typeof(string),
            [binding],
            scan);
        var aggregateProject = new PhysicalProjectNode(
            [
                new ProjectedField("p.Name", new ColumnRef("p", "Name", typeof(string)), 0),
                new ProjectedField("p.Count", new AggregateRef("p.Count", typeof(long)), 1)
            ],
            aggregate);
        var cteRef = new PhysicalCteRefNode(
            "pScore",
            "pScore",
            new OutputSchema(
            [
                new ColumnSchema("p.Name", typeof(string), 0),
                new ColumnSchema("p.Count", typeof(long), 1)
            ]));
        var bucket = new CaseWhen(
            [
                new CaseWhenBranch(
                    new BinaryOp(
                        BinaryOpKind.GreaterThan,
                        new ColumnRef("p", "Count", typeof(long)),
                        new Literal(1, typeof(int)),
                        typeof(bool)),
                    new Literal("Many", typeof(string)))
            ],
            new Literal("One", typeof(string)),
            typeof(string));
        var finalProject = new PhysicalProjectNode(
            [
                new ProjectedField("Name", new ColumnRef("p", "Name", typeof(string)), 0),
                new ProjectedField("Bucket", bucket, 1)
            ],
            cteRef);
        var multiStatement = new PhysicalMultiStatementNode([aggregateProject, finalProject]);
        var builder = CreateSerialBuilder();

        var result = builder.Build(multiStatement, "Q_FinalAggregateCaseProjection");
        var plan = RequireExecutionPlan(result);
        var planText = ExecutionPlanPrinter.Print(plan);

        StringAssert.Contains(planText, "StoreTable [statement0 -> _cteRowResults.Slot0: List<Statement0Row0>]");
        StringAssert.Contains(planText, "Generated [Statement0Row0]");
        StringAssert.Contains(planText, "AppendShape [result <- ResultShape0(Name: pScore.p.Name, Bucket: CASE WHEN (pScore.p.Count > 1) THEN 'Many' ELSE 'One' END)]");
    }
}
