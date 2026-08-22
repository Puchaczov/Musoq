using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Optimization.Execution;
using Musoq.Evaluator.IR.Logical.Nodes;
using Musoq.Evaluator.IR.Physical.Nodes;
using Musoq.Evaluator.Tests.Schema.Generic;

namespace Musoq.Evaluator.Tests.IR;

public sealed partial class PhysicalToExecutionPlanBuilderTests
{
    [TestMethod]
    public void Build_WhenPlanHasOuterApplyWithLeftOnlyFilter_ShouldFilterMatchedAndUnmatchedRows()
    {
        var people = CreateScan();
        var orders = CreateOrderScan();
        var apply = new PhysicalNestedLoopApplyNode(ApplyKind.Outer, people, orders);
        var predicate = new BinaryOp(
            BinaryOpKind.Equal,
            new ColumnRef("p", "Name", typeof(string)),
            new Literal("Ada", typeof(string)),
            typeof(bool));
        var filter = new PhysicalFilterNode(predicate, apply);
        var project = new PhysicalProjectNode(
            [
                new ProjectedField("Name", new ColumnRef("p", "Name", typeof(string)), 0),
                new ProjectedField("Description", new ColumnRef("o", "Description", typeof(string)), 1)
            ],
            filter);
        var builder = CreateJoinBuilder();

        var result = builder.Build(project, "Q_OuterApplyFilter");
        var plan = RequireExecutionPlan(result);

        var expected = string.Join("\n",
            "ExecutionPlan [Q_OuterApplyFilter]",
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
            "    CreateShapeRows [result: ResultShape0 from ResultRow0]",
            "    ChunkedForEach [p in pRows]",
            "      SourceScan [o: Order] -> oRows",
            "      Let [oHasMatch: bool = FALSE]",
            "      ChunkedForEach [o in oRows]",
            "        Assign [oHasMatch = TRUE]",
            "        If [(p.Name = 'Ada')]",
            "          AppendShape [result <- ResultShape0(Name: p.Name, Description: o.Description)]",
            "      If [NOT oHasMatch]",
            "        If [(p.Name = 'Ada')]",
            "          AppendShape [result <- ResultShape0(Name: p.Name, Description: NULL)]",
            "    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]");

        Assert.AreEqual(expected, PrintPlanWithoutPhaseBoundaries(plan));
    }

    [TestMethod]
    public void Build_WhenPlanHasOuterApplyWithComputedRightProjection_ShouldNullExtendComputedValue()
    {
        var people = CreateScan();
        var orders = CreateOrderScan();
        var apply = new PhysicalNestedLoopApplyNode(ApplyKind.Outer, people, orders);
        var nextAge = new BinaryOp(
            BinaryOpKind.Add,
            new ColumnRef("o", "PersonAge", typeof(int)),
            new Literal(1, typeof(int)),
            typeof(int));
        var project = new PhysicalProjectNode(
            [
                new ProjectedField("Name", new ColumnRef("p", "Name", typeof(string)), 0),
                new ProjectedField("NextAge", nextAge, 1)
            ],
            apply);
        var builder = CreateJoinBuilder();

        var result = builder.Build(project, "Q_OuterApplyComputedProjection");
        var plan = RequireExecutionPlan(result);

        var expected = string.Join("\n",
            "ExecutionPlan [Q_OuterApplyComputedProjection]",
            "  Shapes",
            "    SourceEntity [p: Person]",
            "      Name: string <- property Name",
            "      Age: int <- property Age",
            "    SourceEntity [o: Order]",
            "      PersonAge: int <- property PersonAge",
            "      Description: string <- property Description",
            "    Generated [ResultRow0]",
            "      Name: string <- field Name",
            "      NextAge: int? <- field NextAge",
            string.Empty,
            "  Body",
            "    SourceScan [p: Person] -> pRows",
            "    CreateShapeRows [result: ResultShape0 from ResultRow0]",
            "    ChunkedForEach [p in pRows]",
            "      SourceScan [o: Order] -> oRows",
            "      Let [oHasMatch: bool = FALSE]",
            "      ChunkedForEach [o in oRows]",
            "        Assign [oHasMatch = TRUE]",
            "        AppendShape [result <- ResultShape0(Name: p.Name, NextAge: (o.PersonAge + 1))]",
            "      If [NOT oHasMatch]",
            "        AppendShape [result <- ResultShape0(Name: p.Name, NextAge: NULL)]",
            "    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]");

        Assert.AreEqual(expected, PrintPlanWithoutPhaseBoundaries(plan));
    }

    [TestMethod]
    public void Build_WhenPlanHasCrossApplyAccessMethodSource_ShouldReturnExecutionPlan()
    {
        var people = CreateScan();
        var method = typeof(GenericLibrary).GetMethod(nameof(GenericLibrary.JustReturnArrayOfString), Type.EmptyTypes) ??
                     throw new InvalidOperationException("Access method was not found.");
        var accessMethod = new PhysicalAccessMethodSourceNode(
            "p",
            new MethodCall(method, [], "p", typeof(string[])),
            "b",
            typeof(string[]),
            ApplyKind.Cross,
            new OutputSchema([new ColumnSchema("Value", typeof(string), 0)]));
        var apply = new PhysicalNestedLoopApplyNode(ApplyKind.Cross, people, accessMethod);
        var project = new PhysicalProjectNode(
            [
                new ProjectedField("Name", new ColumnRef("p", "Name", typeof(string)), 0),
                new ProjectedField("Value", new ColumnRef("b", "Value", typeof(string)), 1)
            ],
            apply);
        var builder = CreateBuilder();

        var result = builder.Build(project, "Q_CrossApplyAccessMethod");
        var plan = RequireExecutionPlan(result);

        var expected = string.Join("\n",
            "ExecutionPlan [Q_CrossApplyAccessMethod]",
            "  Shapes",
            "    SourceEntity [p: Person]",
            "      Name: string <- property Name",
            "      Age: int <- property Age",
            "    SourceEntity [b: string]",
            "      Value: string <- direct scalar value",
            "    Generated [ResultRow0]",
            "      Name: string <- field Name",
            "      Value: string <- field Value",
            string.Empty,
            "  Body",
            "    SourceScan [p: Person] -> pRows",
            "    CreateShapeRows [result: ResultShape0 from ResultRow0]",
            "    ChunkedForEach [p in pRows]",
            "      EnumerableSource [JustReturnArrayOfString() -> bRows]",
            "      ChunkedForEach [b in bRows]",
            "        AppendShape [result <- ResultShape0(Name: p.Name, Value: b.Value)]",
            "    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]");

        Assert.AreEqual(expected, PrintPlanWithoutPhaseBoundaries(plan));
    }

    [TestMethod]
    public void Build_WhenCrossApplyLeftInputIsNestedApply_ShouldStreamApplyChain()
    {
        var items = CreateApplyItemScan();
        var numbers = CreateNumbersPropertySource("i", "n");
        var nestedApply = new PhysicalNestedLoopApplyNode(ApplyKind.Cross, items, numbers);
        var moreNumbers = CreateNumbersPropertySource("i", "m");
        var apply = new PhysicalNestedLoopApplyNode(ApplyKind.Cross, nestedApply, moreNumbers);
        var project = new PhysicalProjectNode(
            [
                new ProjectedField("Name", new ColumnRef("i", "Name", typeof(string)), 0),
                new ProjectedField("FirstValue", new ColumnRef("n", "Value", typeof(int)), 1),
                new ProjectedField("SecondValue", new ColumnRef("m", "Value", typeof(int)), 2)
            ],
            apply);
        var builder = CreateApplyItemBuilder();

        var result = builder.Build(project, "Q_NestedApplySource");
        var plan = RequireExecutionPlan(result);
        var planText = ExecutionPlanPrinter.Print(plan);

        Assert.IsFalse(planText.Contains("CreateTable [apply_0_i_nTable: apply_0_i_nRow0]", StringComparison.Ordinal), planText);
        Assert.IsFalse(planText.Contains("ForEach [apply_0_i_n in apply_0_i_nTable.Rows]", StringComparison.Ordinal), planText);
        StringAssert.Contains(planText, "ChunkedForEach [i in iRows]");
        StringAssert.Contains(planText, "EnumerableSource [i.Numbers -> nRows]");
        StringAssert.Contains(planText, "ChunkedForEach [n in nRows]");
        StringAssert.Contains(planText, "EnumerableSource [i.Numbers -> mRows]");
        StringAssert.Contains(planText, "ChunkedForEach [m in mRows]");
        StringAssert.Contains(planText, "AppendShape [result <- ResultShape0(Name: i.Name, FirstValue: n.Value, SecondValue: m.Value)]");
    }

    [TestMethod]
    public void Build_WhenFinalSideEffectApplyProjectionCanFuse_ShouldEmitSingleUseCandidate()
    {
        var people = CreateScan();
        var method = typeof(GenericLibrary).GetMethod(nameof(GenericLibrary.JustReturnArrayOfString), Type.EmptyTypes) ??
                     throw new InvalidOperationException("Access method was not found.");
        var accessMethod = new PhysicalAccessMethodSourceNode(
            "p",
            new MethodCall(method, [], "p", typeof(string[])),
            "b",
            typeof(string[]),
            ApplyKind.Cross,
            new OutputSchema([new ColumnSchema("Value", typeof(string), 0)]));
        var apply = new PhysicalNestedLoopApplyNode(ApplyKind.Cross, people, accessMethod);
        var producer = new PhysicalProjectNode(
            [
                new ProjectedField("Name", new ColumnRef("p", "Name", typeof(string)), 0),
                new ProjectedField("Value", new ColumnRef("b", "Value", typeof(string)), 1)
            ],
            apply);
        var cteRef = new PhysicalCteRefNode(
            "applyRows",
            "a",
            new OutputSchema(
            [
                new ColumnSchema("Name", typeof(string), 0),
                new ColumnSchema("Value", typeof(string), 1)
            ]));
        var finalProject = new PhysicalProjectNode(
            [
                new ProjectedField("Name", new ColumnRef("a", "Name", typeof(string)), 0),
                new ProjectedField("Value", new ColumnRef("a", "Value", typeof(string)), 1)
            ],
            cteRef);
        var builder = CreateBuilder();

        var result = builder.Build(new PhysicalMultiStatementNode([producer, finalProject]), "Q_FinalApplyProjection");
        var plan = RequireExecutionPlan(result);
        var initialText = ExecutionPlanPrinter.Print(plan);
        var optimizedText = ExecutionPlanPrinter.Print(new ExecutionIrOptimizer().Optimize(plan).OptimizedPlan);

        StringAssert.Contains(initialText, "SingleUseFusionCandidate [cte0]");
        Assert.IsFalse(initialText.Contains("PhaseBoundary [Begin:cte0]", StringComparison.Ordinal), initialText);
        StringAssert.Contains(optimizedText, "PhaseBoundary [Begin:cte0]");
        Assert.IsFalse(optimizedText.Contains("SingleUseFusionCandidate", StringComparison.Ordinal), optimizedText);
    }
}
