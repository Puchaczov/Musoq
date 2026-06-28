using System;
using System.Collections.Generic;
using System.Dynamic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Logical.Nodes;
using Musoq.Evaluator.IR.Physical.Nodes;
using Musoq.Evaluator.IR.Planning;
using ExecutionStrategyPlan = Musoq.Evaluator.IR.Planning.ExecutionStrategyPlan;

namespace Musoq.Evaluator.Tests.IR;

public sealed partial class PhysicalToExecutionPlanBuilderTests
{

    [TestMethod]
    public void Build_WhenSetOperationArmIsNestedSetOperation_ShouldReturnExecutionPlan()
    {
        var people = CreateScan();
        var orders = CreateOrderScan();
        var otherPeople = CreateScan("q");
        var leftLeftArm = new PhysicalProjectNode(
            [new ProjectedField("Name", new ColumnRef("p", "Name", typeof(string)), 0)],
            people);
        var leftRightArm = new PhysicalProjectNode(
            [new ProjectedField("Name", new ColumnRef("o", "Description", typeof(string)), 0)],
            orders);
        var rightArm = new PhysicalProjectNode(
            [new ProjectedField("Name", new ColumnRef("q", "Name", typeof(string)), 0)],
            otherPeople);
        var nestedSetOperation = new PhysicalSetOperationNode(
            SetOpKind.UnionAll,
            leftLeftArm,
            leftRightArm,
            [0],
            [typeof(string)]);
        var setOperation = new PhysicalSetOperationNode(
            SetOpKind.UnionAll,
            nestedSetOperation,
            rightArm,
            [0],
            [typeof(string)]);
        var builder = CreateJoinBuilder();

        var result = builder.Build(setOperation, "Q_NestedSetOperation");
        var plan = RequireExecutionPlan(result);

        var planText = ExecutionPlanPrinter.Print(plan);

        Assert.Contains("CreateRowBuffer [left: List<LeftRow0>]", planText);
        Assert.Contains("SourceScan [p: Person] -> left_pRows", planText);
        Assert.Contains("AppendRowBuffer [left <- LeftRow0(Name: p.Name)]", planText);
        Assert.Contains("SourceScan [o: Order] -> right_oRows", planText);
        Assert.Contains("AppendRowBuffer [left <- LeftRow0(Name: o.Description)]", planText);
        Assert.Contains("CreateRowBuffer [right: List<RightRow0>]", planText);
        Assert.Contains("SetOperation [result = left UnionAll right, AppendLoop]", planText);
        Assert.IsFalse(planText.Contains("CreateTable [leftLeft:", StringComparison.Ordinal));
        Assert.IsFalse(planText.Contains("CreateTable [leftRight:", StringComparison.Ordinal));
        Assert.IsFalse(planText.Contains("SetOperation [left = leftLeft UnionAll leftRight]", StringComparison.Ordinal));
    }

    [TestMethod]
    public void Build_WhenSetOperationHasSortSkipTake_ShouldReturnExecutionPlan()
    {
        var people = CreateScan();
        var orders = CreateOrderScan();
        var leftArm = new PhysicalProjectNode(
            [new ProjectedField("Name", new ColumnRef("p", "Name", typeof(string)), 0)],
            people);
        var rightArm = new PhysicalProjectNode(
            [new ProjectedField("Name", new ColumnRef("o", "Description", typeof(string)), 0)],
            orders);
        var setOperation = new PhysicalSetOperationNode(
            SetOpKind.UnionAll,
            leftArm,
            rightArm,
            [0],
            [typeof(string)]);
        var sort = new PhysicalSortNode(
            [new OrderField(new ColumnRef(string.Empty, "Name", typeof(string)), Descending: false)],
            setOperation);
        var skip = new PhysicalSkipNode(1, sort);
        var take = new PhysicalTakeNode(1, skip);
        var builder = CreateJoinBuilder();

        var result = builder.Build(take, "Q_SetOperationSortPage");
        var plan = RequireExecutionPlan(result);

        var planText = ExecutionPlanPrinter.Print(plan);

        AssertFinalShapeResult(plan, "resultSortedSliced", "ResultRow0", "Name");
        Assert.Contains("CreateShapeRows [result: ResultShape0 from ResultRow0]", planText);
        Assert.Contains("SourceScan [p: Person] -> left_pRows", planText);
        Assert.Contains("AppendShape [result <- ResultShape0(Name: p.Name)]", planText);
        Assert.Contains("SourceScan [o: Order] -> right_oRows", planText);
        Assert.Contains("AppendShape [result <- ResultShape0(Name: o.Description)]", planText);
        Assert.Contains("SortShapeRows [result -> resultSorted by Name ASC; capacity: Candidate(resultSorted <- result.Count)]", planText);
        Assert.Contains(
            "SliceShapeRows [resultSorted -> resultSortedSliced, skip 1, take 1; capacity: Candidate(resultSortedSliced <- Min(Max(resultSorted.Count - 1, 0), 1))]",
            planText);
        Assert.Contains("ReturnDeferredTable [resultSortedSliced: ResultRow0 <- ResultShape0]", planText);
        Assert.IsFalse(planText.Contains("CreateTable [left:", StringComparison.Ordinal));
        Assert.IsFalse(planText.Contains("CreateTable [right:", StringComparison.Ordinal));
        Assert.IsFalse(planText.Contains("SetOperation [result = left UnionAll right]", StringComparison.Ordinal));
    }

    [TestMethod]
    public void Build_WhenSetOperationArmsUseExpandoSources_ShouldReturnExecutionPlan()
    {
        var leftScan = CreateScan();
        var rightScan = CreateScan("q");
        var leftArm = new PhysicalProjectNode(
            [new ProjectedField("Name", new ColumnRef("p", "Name", typeof(string)), 0)],
            leftScan);
        var rightArm = new PhysicalProjectNode(
            [new ProjectedField("Name", new ColumnRef("q", "Name", typeof(string)), 0)],
            rightScan);
        var setOperation = new PhysicalSetOperationNode(
            SetOpKind.UnionAll,
            leftArm,
            rightArm,
            [0],
            [typeof(string)]);
        var shapeResolver = new ExecutionShapeResolver(
            entityTypesByAlias: new Dictionary<string, Type>
            {
                ["p"] = typeof(ExpandoObject),
                ["q"] = typeof(ExpandoObject)
            });
        var builder = new PlannedExecutionBuilder(shapeResolver);

        var result = builder.Build(setOperation, "Q_DynamicSetOperation");
        var plan = RequireExecutionPlan(result);
        var planText = ExecutionPlanPrinter.Print(plan);

        Assert.Contains("ExpandoAdapter [p: pDynamicRow0]", planText);
        Assert.Contains("ExpandoAdapter [q: qDynamicRow0]", planText);
        Assert.Contains("CreateShapeRows [result: ResultShape0 from ResultRow0]", planText);
        Assert.Contains("AppendShape [result <- ResultShape0(Name: p.Name)]", planText);
        Assert.Contains("AppendShape [result <- ResultShape0(Name: q.Name)]", planText);
        Assert.IsFalse(planText.Contains("SetOperation [result = left UnionAll right]", StringComparison.Ordinal));
    }

    [TestMethod]
    public void Build_WhenStreamingUnionAllIsSelectedButArmCannotLower_ShouldReturnUnsupportedReason()
    {
        var leftScan = CreateScan();
        var leftArm = new PhysicalProjectNode(
            [new ProjectedField("Name", new ColumnRef("p", "Name", typeof(string)), 0)],
            leftScan);
        var rightCte = CreateCteRef("missing", "m");
        var rightArm = new PhysicalProjectNode(
            [new ProjectedField("Name", new ColumnRef("m", "Name", typeof(string)), 0)],
            rightCte);
        var setOperation = new PhysicalSetOperationNode(
            SetOpKind.UnionAll,
            leftArm,
            rightArm,
            [0],
            [typeof(string)]);
        var builder = CreateBuilder(CreateSetOperationStrategies(
            setOperation,
            SetOperationStrategyDecision.StreamingUnionAll("forced streaming test")));

        var result = builder.Build(setOperation, "Q_ForcedStreamingUnionAll");
        var reason = RequireUnsupportedReason(result);

        StringAssert.Contains(reason, "Planner selected streaming UnionAll");
        StringAssert.Contains(reason, "right arm");
    }

    private static ExecutionStrategyPlan CreateSetOperationStrategies(
        PhysicalSetOperationNode setOperation,
        SetOperationStrategyDecision decision)
    {
        return new ExecutionStrategyPlan(
            new HashSet<PhysicalSingleKeyAggregateNode>(),
            new HashSet<PhysicalProjectNode>(),
            new Dictionary<PhysicalCteNode, IReadOnlyList<PlannedParallelCteLevel>>(),
            new Dictionary<PhysicalCteNode, CteStrategyDecision>(),
            new Dictionary<PhysicalCteNode, CteSidecarIndexPlan>(),
            new Dictionary<PhysicalSetOperationNode, SetOperationStrategyDecision>
            {
                [setOperation] = decision
            },
            new Dictionary<string, SourceBoundaryStrategyPlan>(StringComparer.Ordinal),
            new Dictionary<BoundaryRowShapeKind, RowWidthPruningPlan[]>(),
            []);
    }

}
