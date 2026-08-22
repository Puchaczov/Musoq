using System;
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
    public void Build_WhenPlanHasCompositeLeftOuterHashJoin_ShouldReturnHashExecutionPlan()
    {
        var people = CreateScan();
        var orders = CreateOrderScan();
        var join = new PhysicalHashJoinNode(
            JoinKind.LeftOuter,
            [
                new ColumnRef("o", "PersonAge", typeof(int)),
                new ColumnRef("o", "Description", typeof(string))
            ],
            [
                new ColumnRef("p", "Age", typeof(int)),
                new ColumnRef("p", "Name", typeof(string))
            ],
            null,
            people,
            orders);
        var project = new PhysicalProjectNode(
            [
                new ProjectedField("Name", new ColumnRef("p", "Name", typeof(string)), 0),
                new ProjectedField("Description", new ColumnRef("o", "Description", typeof(string)), 1)
            ],
            join);
        var builder = CreateJoinBuilder();

        var result = builder.Build(project, "Q_CompositeOuterHashJoin");
        var plan = RequireExecutionPlan(result);

        var expected = string.Join("\n",
            "ExecutionPlan [Q_CompositeOuterHashJoin]",
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
            "    CreateHash [oHash: ValueTuple<int, string> -> Order]",
            "    ChunkedForEach [o in oRows]",
            "      HashAdd [oHash[(o.PersonAge, o.Description)] += o]",
            "    ChunkedForEach [p in pRows]",
            "      HashProbe [oHash[(p.Age, p.Name)] -> oHashMatches] [match: oHashHasMatch]",
            "        ForEach [o in oHashMatches]",
            "          Assign [oHashHasMatch = TRUE]",
            "          AppendShape [result <- ResultShape0(Name: p.Name, Description: o.Description)]",
            "      HashProbeNoMatch",
            "        AppendShape [result <- ResultShape0(Name: p.Name, Description: NULL)]",
            "    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]");

        Assert.AreEqual(expected, PrintPlanWithoutPhaseBoundaries(plan));
    }

    [TestMethod]
    public void Build_WhenPlanHasCompositeValueTypeHashJoin_ShouldUseValueTupleHashKey()
    {
        var people = CreateScan();
        var orders = CreateOrderScan();
        var join = new PhysicalHashJoinNode(
            JoinKind.Inner,
            [
                new ColumnRef("o", "PersonAge", typeof(int)),
                new ColumnRef("o", "PersonAge", typeof(int))
            ],
            [
                new ColumnRef("p", "Age", typeof(int)),
                new ColumnRef("p", "Age", typeof(int))
            ],
            null,
            people,
            orders);
        var project = new PhysicalProjectNode(
            [
                new ProjectedField("Name", new ColumnRef("p", "Name", typeof(string)), 0),
                new ProjectedField("Description", new ColumnRef("o", "Description", typeof(string)), 1)
            ],
            join);
        var builder = CreateJoinBuilder();

        var result = builder.Build(project, "Q_CompositeValueHashJoin");
        var plan = RequireExecutionPlan(result);
        var planText = ExecutionPlanPrinter.Print(plan);

        Assert.Contains("CreateHash [oHash: ValueTuple<int, int> -> Order]", planText);
        Assert.Contains("HashAdd [oHash[(o.PersonAge, o.PersonAge)] += o]", planText);
        Assert.Contains("HashProbe [oHash[(p.Age, p.Age)] -> oHashMatches]", planText);
        Assert.IsFalse(planText.Contains("CreateNullableHashJoinKey", StringComparison.Ordinal));
    }

    [TestMethod]
    public void Build_WhenOuterHashJoinHasNullableSideFilter_ShouldFilterMatchedAndUnmatchedRows()
    {
        var people = CreateScan();
        var orders = CreateOrderScan();
        var join = new PhysicalHashJoinNode(
            JoinKind.LeftOuter,
            [new ColumnRef("o", "PersonAge", typeof(int))],
            [new ColumnRef("p", "Age", typeof(int))],
            null,
            people,
            orders);
        var filter = new PhysicalFilterNode(
            new IsNullCheck(new ColumnRef("o", "Description", typeof(string)), false, typeof(bool)),
            join);
        var project = new PhysicalProjectNode(
            [
                new ProjectedField("Name", new ColumnRef("p", "Name", typeof(string)), 0),
                new ProjectedField("Description", new ColumnRef("o", "Description", typeof(string)), 1)
            ],
            filter);
        var builder = CreateJoinBuilder();

        var result = builder.Build(project, "Q_OuterHashJoinFilter");
        var plan = RequireExecutionPlan(result);

        var expected = string.Join("\n",
            "ExecutionPlan [Q_OuterHashJoinFilter]",
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
            "    CreateHash [oHash: int -> Order]",
            "    ChunkedForEach [o in oRows]",
            "      HashAdd [oHash[o.PersonAge] += o]",
            "    ChunkedForEach [p in pRows]",
            "      HashProbe [oHash[p.Age] -> oHashMatches] [match: oHashHasMatch]",
            "        ForEach [o in oHashMatches]",
            "          Assign [oHashHasMatch = TRUE]",
            "          If [o.Description IS NULL]",
            "            AppendShape [result <- ResultShape0(Name: p.Name, Description: o.Description)]",
            "      HashProbeNoMatch",
            "        If [TRUE]",
            "          AppendShape [result <- ResultShape0(Name: p.Name, Description: NULL)]",
            "    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]");

        Assert.AreEqual(expected, PrintPlanWithoutPhaseBoundaries(plan));
    }

    [TestMethod]
    public void Build_WhenOuterHashJoinHasResidualPredicate_ShouldTrackFullJoinMatches()
    {
        var people = CreateScan();
        var orders = CreateOrderScan();
        var join = new PhysicalHashJoinNode(
            JoinKind.LeftOuter,
            [new ColumnRef("o", "PersonAge", typeof(int))],
            [new ColumnRef("p", "Age", typeof(int))],
            new BinaryOp(
                BinaryOpKind.Equal,
                new ColumnRef("o", "Description", typeof(string)),
                new Literal("Shipped", typeof(string)),
                typeof(bool)),
            people,
            orders);
        var project = new PhysicalProjectNode(
            [
                new ProjectedField("Name", new ColumnRef("p", "Name", typeof(string)), 0),
                new ProjectedField("Description", new ColumnRef("o", "Description", typeof(string)), 1)
            ],
            join);
        var builder = CreateJoinBuilder();

        var result = builder.Build(project, "Q_OuterHashJoinResidual");
        var plan = RequireExecutionPlan(result);
        var planText = ExecutionPlanPrinter.Print(plan);

        Assert.Contains("HashProbe [oHash[p.Age] -> oHashMatches] [match: oHashHasMatch]", planText);
        Assert.Contains("If [(o.Description = 'Shipped')]", planText);
        Assert.Contains("Assign [oHashHasMatch = TRUE]", planText);
        Assert.Contains("HashProbeNoMatch", planText);
        Assert.Contains("AppendShape [result <- ResultShape0(Name: p.Name, Description: NULL)]", planText);
    }

    [TestMethod]
    public void Build_WhenResidualOuterHashJoinFeedsHashJoin_ShouldMaterializeNestedJoinSource()
    {
        var people = CreateScan();
        var orders = CreateOrderScan();
        var otherPeople = CreateScan("q");
        var residualOuterJoin = new PhysicalHashJoinNode(
            JoinKind.LeftOuter,
            [new ColumnRef("o", "PersonAge", typeof(int))],
            [new ColumnRef("p", "Age", typeof(int))],
            new BinaryOp(
                BinaryOpKind.Equal,
                new ColumnRef("o", "Description", typeof(string)),
                new Literal("Shipped", typeof(string)),
                typeof(bool)),
            people,
            orders);
        var join = new PhysicalHashJoinNode(
            JoinKind.Inner,
            [new ColumnRef("q", "Age", typeof(int))],
            [new ColumnRef("p", "Age", typeof(int))],
            null,
            residualOuterJoin,
            otherPeople);
        var project = new PhysicalProjectNode(
            [
                new ProjectedField("Name", new ColumnRef("p", "Name", typeof(string)), 0),
                new ProjectedField("Description", new ColumnRef("o", "Description", typeof(string)), 1),
                new ProjectedField("OtherName", new ColumnRef("q", "Name", typeof(string)), 2)
            ],
            join);
        var builder = CreateJoinBuilder();

        var result = builder.Build(project, "Q_NestedResidualOuterHashJoin");
        var plan = RequireExecutionPlan(result);
        var planText = ExecutionPlanPrinter.Print(plan);

        Assert.Contains("CreateTable [join_0_p_oTable: join_0_p_oRow0]", planText);
        Assert.Contains("HashProbe [join_0_p_oTableOHash[p.Age] -> join_0_p_oTableOHashMatches] [match: join_0_p_oTableOHashHasMatch]", planText);
        Assert.Contains("HashProbeNoMatch", planText);
        Assert.Contains("CreateHash [qHash: int -> Person]", planText);
        Assert.Contains("HashAdd [qHash[q.Age] += q]", planText);
        Assert.Contains("HashProbe [qHash[join_0_p_o.p.Age] -> qHashMatches]", planText);
        Assert.Contains("AppendShape [result <- ResultShape0(Name: join_0_p_o.p.Name, Description: join_0_p_o.o.Description, OtherName: q.Name)]", planText);
    }
}
