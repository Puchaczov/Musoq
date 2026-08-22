using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Logical.Nodes;
using Musoq.Evaluator.IR.Optimization.Execution;
using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.Tests.IR;

public sealed partial class PhysicalToExecutionPlanBuilderTests
{
    [TestMethod]
    public void Build_WhenPlanUsesReadOnceProjectionCte_ShouldFuseCteTable()
    {
        var scan = CreateScan();
        var definition = new PhysicalProjectNode(
            [new ProjectedField("Name", new ColumnRef("p", "Name", typeof(string)), 0)],
            scan);
        var cteRef = new PhysicalCteRefNode("people", "c", new OutputSchema(
        [
            new ColumnSchema("Name", typeof(string), 0)
        ]));
        var query = new PhysicalProjectNode(
            [new ProjectedField("Name", new ColumnRef("c", "Name", typeof(string)), 0)],
            cteRef);
        var cte = new PhysicalCteNode([new PhysicalCteDefinition("people", definition)], query);
        var builder = CreateBuilder();

        var result = builder.Build(cte, "Q_Cte");
        var plan = RequireExecutionPlan(result);

        var parallelProjects = CollectNodes<ExecutionParallelFilterProjectLoop>(plan.Body).ToArray();
        var printed = ExecutionPlanPrinter.Print(plan);

        AssertFinalShapeResult(plan, "result", "ResultRow0", "Name");
        Assert.IsEmpty(parallelProjects);
        StringAssert.Contains(printed, "Generated [ResultRow0]");
        StringAssert.Contains(printed, "CteReadOnceFusionCandidate [cte0]");
        Assert.IsFalse(printed.Contains("PhaseBoundary [Begin:cte0]", StringComparison.Ordinal), printed);
        StringAssert.Contains(printed, "SourceScan [p: Person] -> pRows");
        StringAssert.Contains(printed, "ChunkedForEach [p in pRows]");
        StringAssert.Contains(printed, "AppendShape [result <- ResultShape0(Name: p.Name)]");
        Assert.IsFalse(printed.Contains("Generated [Cte0Row0]", StringComparison.Ordinal));
        Assert.IsFalse(printed.Contains("StoreTable [cte0 -> _tableResults[0]]", StringComparison.Ordinal));
        Assert.IsFalse(printed.Contains("ForEach [c in _tableResults[0].Rows]", StringComparison.Ordinal));
        StringAssert.Contains(printed, "ReturnDeferredTable [result: ResultRow0 <- ResultShape0]");
        StringAssert.Contains(
            ExecutionPlanPrinter.Print(new ExecutionIrOptimizer().Optimize(plan).OptimizedPlan),
            "PhaseBoundary [Begin:cte0]");
    }

    [TestMethod]
    public void Build_WhenMultiStatementProjectionFilterChainIsSingleUse_ShouldEmitFusionCandidate()
    {
        var producer = new PhysicalProjectNode(
            [
                new ProjectedField("Name", new ColumnRef("p", "Name", typeof(string)), 0),
                new ProjectedField("Age", new ColumnRef("p", "Age", typeof(int)), 1)
            ],
            CreateScan());
        var cteRef = new PhysicalCteRefNode("people", "c", new OutputSchema(
        [
            new ColumnSchema("Name", typeof(string), 0),
            new ColumnSchema("Age", typeof(int), 1)
        ]));
        var filter = new PhysicalFilterNode(
            new BinaryOp(
                BinaryOpKind.GreaterThan,
                new ColumnRef("c", "Age", typeof(int)),
                new Literal(18, typeof(int)),
                typeof(bool)),
            cteRef);
        var query = new PhysicalProjectNode(
            [new ProjectedField("Name", new ColumnRef("c", "Name", typeof(string)), 0)],
            filter);
        var builder = CreateBuilder();

        var result = builder.Build(new PhysicalMultiStatementNode([producer, query]), "Q_SingleUseProjectionChain");
        var plan = RequireExecutionPlan(result);
        var initialText = ExecutionPlanPrinter.Print(plan);
        var optimizedText = ExecutionPlanPrinter.Print(new ExecutionIrOptimizer().Optimize(plan).OptimizedPlan);

        StringAssert.Contains(initialText, "SingleUseFusionCandidate [cte0]");
        Assert.IsFalse(initialText.Contains("PhaseBoundary [Begin:cte0]", StringComparison.Ordinal), initialText);
        StringAssert.Contains(initialText, "If [(p.Age > 18)]");
        StringAssert.Contains(optimizedText, "PhaseBoundary [Begin:cte0]");
        Assert.IsFalse(optimizedText.Contains("SingleUseFusionCandidate", StringComparison.Ordinal), optimizedText);
        Assert.IsFalse(optimizedText.Contains("StoreTable [statement0 -> _tableResults[0]]", StringComparison.Ordinal), optimizedText);
    }

    [TestMethod]
    public void Build_WhenCteParallelizationIsEnabledForIndependentDefinitions_ShouldUseParallelBlock()
    {
        var peopleDefinition = new PhysicalProjectNode(
            [new ProjectedField("Name", new ColumnRef("p", "Name", typeof(string)), 0)],
            CreateScan());
        var ordersDefinition = new PhysicalProjectNode(
            [new ProjectedField("Name", new ColumnRef("p", "Name", typeof(string)), 0)],
            CreateScan());
        var cteRef = new PhysicalCteRefNode("people", "c", new OutputSchema(
        [
            new ColumnSchema("Name", typeof(string), 0)
        ]));
        var query = new PhysicalProjectNode(
            [new ProjectedField("Name", new ColumnRef("c", "Name", typeof(string)), 0)],
            cteRef);
        var cte = new PhysicalCteNode(
            [
                new PhysicalCteDefinition("people", peopleDefinition),
                new PhysicalCteDefinition("orders", ordersDefinition)
            ],
            query);
        var builder = CreateBuilder(
            new CompilationOptions(useCteParallelization: true),
            CreateIndependentCteExecutionPlan("people", "orders"));

        var result = builder.Build(cte, "Q_ParallelCte");
        var plan = RequireExecutionPlan(result);
        var text = ExecutionPlanPrinter.Print(plan);
        var parallelBlock = CollectNodes<ExecutionParallelBlock>(plan.Body).Single();

        Assert.Contains("ParallelBlock [cte-level-0, tasks 2, maxDegree 2]", text);
        Assert.Contains("ParallelTask [people -> __parallelCteLevel0Task0Result]", text);
        Assert.Contains("ParallelTask [orders -> __parallelCteLevel0Task1Result]", text);
        Assert.Contains("ParallelMerge", text);
        Assert.Contains("StoreTable [__parallelCteLevel0Task0Result -> _cteRowResults.Slot0: List<Cte0Row0>]", text);
        Assert.Contains("StoreTable [__parallelCteLevel0Task1Result -> _cteRowResults.Slot1: List<Cte1Row0>]", text);
        Assert.HasCount(2, parallelBlock.Tasks);
        Assert.AreEqual(2, parallelBlock.MaxDegreeOfParallelism);
        Assert.AreEqual("List<Cte0Row0>", parallelBlock.Tasks[0].Output.GeneratedRowTypeName);
        Assert.AreEqual("List<Cte1Row0>", parallelBlock.Tasks[1].Output.GeneratedRowTypeName);
        Assert.AreEqual(0, parallelBlock.Tasks[0].RelatedTableIndex);
        Assert.AreEqual(1, parallelBlock.Tasks[1].RelatedTableIndex);
        Assert.IsFalse(text.Contains("_tableResults[0]", StringComparison.Ordinal), text);
        Assert.IsFalse(text.Contains("_tableResults[1]", StringComparison.Ordinal), text);
    }

    [TestMethod]
    public void Build_WhenHashJoinBuildSideIsCteRef_ShouldCarryHashCapacityHint()
    {
        var people = CreateScan();
        var orders = CreateOrderScan();
        var peopleDefinition = new PhysicalProjectNode(
            [new ProjectedField("Name", new ColumnRef("p", "Name", typeof(string)), 0)],
            people);
        var ordersDefinition = new PhysicalProjectNode(
            [new ProjectedField("Name", new ColumnRef("o", "Description", typeof(string)), 0)],
            orders);
        var leftRef = CreateCteRef("people", "l");
        var rightRef = CreateCteRef("orders", "r");
        var join = new PhysicalHashJoinNode(
            JoinKind.Inner,
            [new ColumnRef("r", "Name", typeof(string))],
            [new ColumnRef("l", "Name", typeof(string))],
            null,
            leftRef,
            rightRef);
        var query = new PhysicalProjectNode(
            [new ProjectedField("Name", new ColumnRef("l", "Name", typeof(string)), 0)],
            join);
        var cte = new PhysicalCteNode(
            [
                new PhysicalCteDefinition("people", peopleDefinition),
                new PhysicalCteDefinition("orders", ordersDefinition)
            ],
            query);
        var builder = CreateJoinBuilder(new CompilationOptions(useCteSidecarIndexes: false));

        var result = builder.Build(cte, "Q_CteHashJoinCapacity");
        var plan = RequireExecutionPlan(result);
        var text = ExecutionPlanPrinter.Print(plan);

        Assert.Contains(
            "CreateHash [rHash: string -> Row; capacity: Candidate(rHash <- _cteRowResults.Slot1)]",
            text);
    }

    [TestMethod]
    public void Build_WhenCteQueryIsSetOperation_ShouldReturnExecutionPlan()
    {
        var people = CreateScan();
        var orders = CreateOrderScan();
        var leftDefinition = new PhysicalProjectNode(
            [new ProjectedField("Name", new ColumnRef("p", "Name", typeof(string)), 0)],
            people);
        var rightDefinition = new PhysicalProjectNode(
            [new ProjectedField("Name", new ColumnRef("o", "Description", typeof(string)), 0)],
            orders);
        var leftRef = CreateCteRef("people", "l");
        var rightRef = CreateCteRef("orders", "r");
        var leftArm = new PhysicalProjectNode(
            [new ProjectedField("Name", new ColumnRef("l", "Name", typeof(string)), 0)],
            leftRef);
        var rightArm = new PhysicalProjectNode(
            [new ProjectedField("Name", new ColumnRef("r", "Name", typeof(string)), 0)],
            rightRef);
        var setOperation = new PhysicalSetOperationNode(
            SetOpKind.UnionAll,
            leftArm,
            rightArm,
            [0],
            [typeof(string)]);
        var cte = new PhysicalCteNode(
            [
                new PhysicalCteDefinition("people", leftDefinition),
                new PhysicalCteDefinition("orders", rightDefinition)
            ],
            setOperation);
        var builder = CreateJoinBuilder();

        var result = builder.Build(cte, "Q_CteSetOperation");
        var plan = RequireExecutionPlan(result);

        var planText = ExecutionPlanPrinter.Print(plan);

        AssertFinalShapeResult(plan, "result", "ResultRow0", "Name");
        Assert.Contains("StoreTable [cte0 -> _cteRowResults.Slot0: List<Cte0Row0>]", planText);
        Assert.Contains("StoreTable [cte1 -> _cteRowResults.Slot1: List<Cte1Row0>]", planText);
        Assert.Contains("CreateShapeRows [result: ResultShape0 from ResultRow0]", planText);
        Assert.Contains("ForEach [l in _cteRowResults.Slot0]", planText);
        Assert.Contains("AppendShape [result <- ResultShape0(Name: l.Name)]", planText);
        Assert.Contains("ForEach [r in _cteRowResults.Slot1]", planText);
        Assert.Contains("AppendShape [result <- ResultShape0(Name: r.Name)]", planText);
        Assert.Contains("ReturnDeferredTable [result: ResultRow0 <- ResultShape0]", planText);
        Assert.IsFalse(planText.Contains("CreateTable [left:", StringComparison.Ordinal));
        Assert.IsFalse(planText.Contains("CreateTable [right:", StringComparison.Ordinal));
        Assert.IsFalse(planText.Contains("SetOperation [result = left UnionAll right]", StringComparison.Ordinal));
    }
}
