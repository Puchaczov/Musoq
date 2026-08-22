using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Logical.Nodes;
using Musoq.Evaluator.IR.Physical.Nodes;
using Musoq.Evaluator.Tests.Schema.Generic;

namespace Musoq.Evaluator.Tests.IR;

public sealed partial class PhysicalToExecutionPlanBuilderTests
{
    [TestMethod]
    public void Build_WhenOuterApplyRightFilterUsesCoalesce_ShouldEvaluateFallbackForUnmatchedRows()
    {
        var people = CreateScan();
        var orders = CreateOrderScan();
        var apply = new PhysicalNestedLoopApplyNode(ApplyKind.Outer, people, orders);
        var coalesce = new Coalesce(
            [
                new ColumnRef("o", "Description", typeof(string)),
                new Literal("missing", typeof(string))
            ],
            typeof(string));
        var filter = new PhysicalFilterNode(
            new BinaryOp(
                BinaryOpKind.Equal,
                coalesce,
                new Literal("missing", typeof(string)),
                typeof(bool)),
            apply);
        var project = new PhysicalProjectNode(
            [
                new ProjectedField("Name", new ColumnRef("p", "Name", typeof(string)), 0),
                new ProjectedField("Description", new ColumnRef("o", "Description", typeof(string)), 1)
            ],
            filter);
        var builder = CreateJoinBuilder();

        var result = builder.Build(project, "Q_OuterApplyRightFilterCoalesce");
        var plan = RequireExecutionPlan(result);

        var expected = string.Join("\n",
            "ExecutionPlan [Q_OuterApplyRightFilterCoalesce]",
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
            "        If [(COALESCE(o.Description, 'missing') = 'missing')]",
            "          AppendShape [result <- ResultShape0(Name: p.Name, Description: o.Description)]",
            "      If [NOT oHasMatch]",
            "        If [('missing' = 'missing')]",
            "          AppendShape [result <- ResultShape0(Name: p.Name, Description: NULL)]",
            "    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]");

        Assert.AreEqual(expected, PrintPlanWithoutPhaseBoundaries(plan));
    }

    [TestMethod]
    public void Build_WhenOuterApplyRightFilterCaseBranchResultDependsOnRightAlias_ShouldNullSubstituteBranchResult()
    {
        var people = CreateScan();
        var orders = CreateOrderScan();
        var apply = new PhysicalNestedLoopApplyNode(ApplyKind.Outer, people, orders);
        var caseWhen = new CaseWhen(
            [
                new CaseWhenBranch(
                    new BinaryOp(
                        BinaryOpKind.Equal,
                        new ColumnRef("p", "Name", typeof(string)),
                        new Literal("Ada", typeof(string)),
                        typeof(bool)),
                    new BinaryOp(
                        BinaryOpKind.Equal,
                        new ColumnRef("o", "Description", typeof(string)),
                        new Literal("paid", typeof(string)),
                        typeof(bool)))
            ],
            new Literal(true, typeof(bool)),
            typeof(bool));
        var filter = new PhysicalFilterNode(caseWhen, apply);
        var project = new PhysicalProjectNode(
            [
                new ProjectedField("Name", new ColumnRef("p", "Name", typeof(string)), 0),
                new ProjectedField("Description", new ColumnRef("o", "Description", typeof(string)), 1)
            ],
            filter);
        var builder = CreateJoinBuilder();

        var result = builder.Build(project, "Q_OuterApplyCaseBranchResult");
        var plan = RequireExecutionPlan(result);
        var planText = ExecutionPlanPrinter.Print(plan);

        StringAssert.Contains(planText, "If [(CASE WHEN (p.Name = 'Ada') THEN NULL ELSE TRUE END = TRUE)]");
    }

    [TestMethod]
    public void Build_WhenOuterApplyRightFilterCaseElseResultDependsOnRightAlias_ShouldNullSubstituteElseResult()
    {
        var people = CreateScan();
        var orders = CreateOrderScan();
        var apply = new PhysicalNestedLoopApplyNode(ApplyKind.Outer, people, orders);
        var caseWhen = new CaseWhen(
            [
                new CaseWhenBranch(
                    new BinaryOp(
                        BinaryOpKind.Equal,
                        new ColumnRef("p", "Name", typeof(string)),
                        new Literal("Ada", typeof(string)),
                        typeof(bool)),
                    new Literal(true, typeof(bool)))
            ],
            new BinaryOp(
                BinaryOpKind.Equal,
                new ColumnRef("o", "Description", typeof(string)),
                new Literal("paid", typeof(string)),
                typeof(bool)),
            typeof(bool));
        var filter = new PhysicalFilterNode(caseWhen, apply);
        var project = new PhysicalProjectNode(
            [
                new ProjectedField("Name", new ColumnRef("p", "Name", typeof(string)), 0),
                new ProjectedField("Description", new ColumnRef("o", "Description", typeof(string)), 1)
            ],
            filter);
        var builder = CreateJoinBuilder();

        var result = builder.Build(project, "Q_OuterApplyCaseElseResult");
        var plan = RequireExecutionPlan(result);
        var planText = ExecutionPlanPrinter.Print(plan);

        StringAssert.Contains(planText, "If [(CASE WHEN (p.Name = 'Ada') THEN TRUE ELSE NULL END = TRUE)]");
    }

    [TestMethod]
    public void Build_WhenPlanHasOuterApplyAccessMethodSource_ShouldNullExtendUnmatchedValue()
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
        var apply = new PhysicalNestedLoopApplyNode(ApplyKind.Outer, people, accessMethod);
        var project = new PhysicalProjectNode(
            [
                new ProjectedField("Name", new ColumnRef("p", "Name", typeof(string)), 0),
                new ProjectedField("Value", new ColumnRef("b", "Value", typeof(string)), 1)
            ],
            apply);
        var builder = CreateBuilder();

        var result = builder.Build(project, "Q_OuterApplyAccessMethod");
        var plan = RequireExecutionPlan(result);

        var expected = string.Join("\n",
            "ExecutionPlan [Q_OuterApplyAccessMethod]",
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
            "      Let [bHasMatch: bool = FALSE]",
            "      ChunkedForEach [b in bRows]",
            "        Assign [bHasMatch = TRUE]",
            "        AppendShape [result <- ResultShape0(Name: p.Name, Value: b.Value)]",
            "      If [NOT bHasMatch]",
            "        AppendShape [result <- ResultShape0(Name: p.Name, Value: NULL)]",
            "    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]");

        Assert.AreEqual(expected, PrintPlanWithoutPhaseBoundaries(plan));
    }
}
