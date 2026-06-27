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
    public void Build_WhenOuterApplyFilterReferencesRightAlias_ShouldApplyNullSemanticsToUnmatchedRows()
    {
        var people = CreateScan();
        var orders = CreateOrderScan();
        var apply = new PhysicalNestedLoopApplyNode(ApplyKind.Outer, people, orders);
        var predicate = new BinaryOp(
            BinaryOpKind.Equal,
            new ColumnRef("o", "Description", typeof(string)),
            new Literal("paid", typeof(string)),
            typeof(bool));
        var filter = new PhysicalFilterNode(predicate, apply);
        var project = new PhysicalProjectNode(
            [
                new ProjectedField("Name", new ColumnRef("p", "Name", typeof(string)), 0),
                new ProjectedField("Description", new ColumnRef("o", "Description", typeof(string)), 1)
            ],
            filter);
        var builder = CreateJoinBuilder();

        var result = builder.Build(project, "Q_OuterApplyRightFilter");
        var plan = RequireExecutionPlan(result);

        var expected = string.Join("\n",
            "ExecutionPlan [Q_OuterApplyRightFilter]",
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
            "        If [(o.Description = 'paid')]",
            "          AppendShape [result <- ResultShape0(Name: p.Name, Description: o.Description)]",
            "      If [NOT oHasMatch]",
            "        If [FALSE]",
            "          AppendShape [result <- ResultShape0(Name: p.Name, Description: NULL)]",
            "    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]");

        Assert.AreEqual(expected, ExecutionPlanPrinter.Print(plan));
    }

    [TestMethod]
    public void Build_WhenOuterApplyRightFilterIsOrWithLeftPredicate_ShouldKeepLeftPredicateForUnmatchedRows()
    {
        var people = CreateScan();
        var orders = CreateOrderScan();
        var apply = new PhysicalNestedLoopApplyNode(ApplyKind.Outer, people, orders);
        var leftPredicate = new BinaryOp(
            BinaryOpKind.Equal,
            new ColumnRef("p", "Name", typeof(string)),
            new Literal("Ada", typeof(string)),
            typeof(bool));
        var rightPredicate = new BinaryOp(
            BinaryOpKind.Equal,
            new ColumnRef("o", "Description", typeof(string)),
            new Literal("paid", typeof(string)),
            typeof(bool));
        var filter = new PhysicalFilterNode(
            new BinaryOp(BinaryOpKind.Or, leftPredicate, rightPredicate, typeof(bool)),
            apply);
        var project = new PhysicalProjectNode(
            [
                new ProjectedField("Name", new ColumnRef("p", "Name", typeof(string)), 0),
                new ProjectedField("Description", new ColumnRef("o", "Description", typeof(string)), 1)
            ],
            filter);
        var builder = CreateJoinBuilder();

        var result = builder.Build(project, "Q_OuterApplyRightFilterOr");
        var plan = RequireExecutionPlan(result);

        var expected = string.Join("\n",
            "ExecutionPlan [Q_OuterApplyRightFilterOr]",
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
            "        If [((p.Name = 'Ada') OR (o.Description = 'paid'))]",
            "          AppendShape [result <- ResultShape0(Name: p.Name, Description: o.Description)]",
            "      If [NOT oHasMatch]",
            "        If [(p.Name = 'Ada')]",
            "          AppendShape [result <- ResultShape0(Name: p.Name, Description: NULL)]",
            "    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]");

        Assert.AreEqual(expected, ExecutionPlanPrinter.Print(plan));
    }

    [TestMethod]
    public void Build_WhenOuterApplyRightFilterUsesIsNull_ShouldEmitUnmatchedNullExtendedRow()
    {
        var people = CreateScan();
        var orders = CreateOrderScan();
        var apply = new PhysicalNestedLoopApplyNode(ApplyKind.Outer, people, orders);
        var filter = new PhysicalFilterNode(
            new IsNullCheck(new ColumnRef("o", "Description", typeof(string)), false, typeof(bool)),
            apply);
        var project = new PhysicalProjectNode(
            [
                new ProjectedField("Name", new ColumnRef("p", "Name", typeof(string)), 0),
                new ProjectedField("Description", new ColumnRef("o", "Description", typeof(string)), 1)
            ],
            filter);
        var builder = CreateJoinBuilder();

        var result = builder.Build(project, "Q_OuterApplyRightFilterIsNull");
        var plan = RequireExecutionPlan(result);

        var expected = string.Join("\n",
            "ExecutionPlan [Q_OuterApplyRightFilterIsNull]",
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
            "        If [o.Description IS NULL]",
            "          AppendShape [result <- ResultShape0(Name: p.Name, Description: o.Description)]",
            "      If [NOT oHasMatch]",
            "        If [TRUE]",
            "          AppendShape [result <- ResultShape0(Name: p.Name, Description: NULL)]",
            "    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]");

        Assert.AreEqual(expected, ExecutionPlanPrinter.Print(plan));
    }

    [TestMethod]
    public void Build_WhenOuterApplyRightFilterUsesBetweenLikeAndIn_ShouldFilterUnmatchedRows()
    {
        var people = CreateScan();
        var orders = CreateOrderScan();
        var apply = new PhysicalNestedLoopApplyNode(ApplyKind.Outer, people, orders);
        var between = new Between(
            new ColumnRef("o", "PersonAge", typeof(int)),
            new Literal(18, typeof(int)),
            new Literal(30, typeof(int)),
            typeof(bool));
        var like = new PatternMatch(
            new ColumnRef("o", "Description", typeof(string)),
            new Literal("p%", typeof(string)),
            PatternKind.Like,
            typeof(bool));
        var inCheck = new InCheck(
            new ColumnRef("o", "Description", typeof(string)),
            [new Literal("paid", typeof(string))],
            typeof(bool));
        var filter = new PhysicalFilterNode(
            new BinaryOp(
                BinaryOpKind.Or,
                between,
                new BinaryOp(BinaryOpKind.Or, like, inCheck, typeof(bool)),
                typeof(bool)),
            apply);
        var project = new PhysicalProjectNode(
            [
                new ProjectedField("Name", new ColumnRef("p", "Name", typeof(string)), 0),
                new ProjectedField("Description", new ColumnRef("o", "Description", typeof(string)), 1)
            ],
            filter);
        var builder = CreateJoinBuilder();

        var result = builder.Build(project, "Q_OuterApplyRightFilterRawForms");
        var plan = RequireExecutionPlan(result);

        var expected = string.Join("\n",
            "ExecutionPlan [Q_OuterApplyRightFilterRawForms]",
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
            "        If [(o.PersonAge BETWEEN 18 AND 30 OR (o.Description LIKE 'p%' OR o.Description IN ('paid')))]",
            "          AppendShape [result <- ResultShape0(Name: p.Name, Description: o.Description)]",
            "      If [NOT oHasMatch]",
            "        If [FALSE]",
            "          AppendShape [result <- ResultShape0(Name: p.Name, Description: NULL)]",
            "    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]");

        Assert.AreEqual(expected, ExecutionPlanPrinter.Print(plan));
    }

}
