using System;
using System.Linq;
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
    public void Build_WhenPlanHasInnerHashJoin_ShouldReturnHashExecutionPlan()
    {
        var people = CreateScan();
        var orders = CreateOrderScan();
        var join = new PhysicalHashJoinNode(
            JoinKind.Inner,
            [new ColumnRef("o", "PersonAge", typeof(int))],
            [new ColumnRef("p", "Age", typeof(int))],
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

        var result = builder.Build(project, "Q_HashJoin");
        var plan = RequireExecutionPlan(result);

        var expected = string.Join("\n",
            "ExecutionPlan [Q_HashJoin]",
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
            "      HashProbe [oHash[p.Age] -> oHashMatches]",
            "        ForEach [o in oHashMatches]",
            "          AppendShape [result <- ResultShape0(Name: p.Name, Description: o.Description)]",
            "    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]");

        Assert.AreEqual(expected, PrintPlanWithoutPhaseBoundaries(plan));
    }

    [TestMethod]
    public void Build_WhenPlanHasInnerHashJoinFinalProjection_ShouldPruneSourceContexts()
    {
        var people = CreateScan();
        var orders = CreateOrderScan();
        var join = new PhysicalHashJoinNode(
            JoinKind.Inner,
            [new ColumnRef("o", "PersonAge", typeof(int))],
            [new ColumnRef("p", "Age", typeof(int))],
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

        var result = builder.Build(project, "Q_HashJoinContextLayout");
        var plan = RequireExecutionPlan(result);
        var appendRow = CollectNodes<ExecutionAppendRow>(plan.Body).Single();
        var rowShape = plan.Shapes.OfType<GeneratedRowShape>().Single(static shape => shape.TypeName == "ResultRow0");

        Assert.HasCount(0, appendRow.Contexts);
        Assert.IsNull(appendRow.ContextLayout);
        Assert.HasCount(0, rowShape.Contexts);
    }

    [TestMethod]
    public void Build_WhenSingleKeyAggregateReadsHashJoinSource_ShouldStreamHashMatchesIntoAggregate()
    {
        var people = CreateScan();
        var orders = CreateOrderScan();
        var binding = CreateCountDescriptionBinding();
        var join = new PhysicalHashJoinNode(
            JoinKind.Inner,
            [new ColumnRef("o", "PersonAge", typeof(int))],
            [new ColumnRef("p", "Age", typeof(int))],
            null,
            people,
            orders);
        var aggregate = new PhysicalSingleKeyAggregateNode(
            new ColumnRef("p", "Name", typeof(string)),
            "p.Name",
            typeof(string),
            [binding],
            join);
        var project = new PhysicalProjectNode(
            [
                new ProjectedField("Name", new ColumnRef("p", "Name", typeof(string)), 0),
                new ProjectedField("DescriptionCount", new AggregateRef("p.CountDescription", typeof(long)), 1)
            ],
            aggregate);
        var builder = CreateJoinBuilder();

        var result = builder.Build(project, "Q_AggregateHashJoin");
        var plan = RequireExecutionPlan(result);
        var text = ExecutionPlanPrinter.Print(plan);

        Assert.IsFalse(text.Contains("CreateTable [join_0_p_oTable: join_0_p_oRow0]", StringComparison.Ordinal));
        Assert.IsFalse(text.Contains("ForEach [join_0_p_o in join_0_p_oTable.Rows]", StringComparison.Ordinal));
        Assert.Contains("CreateHash [oHash: int -> Order]", text);
        Assert.Contains("HashProbe [oHash[p.Age] -> oHashMatches]", text);
        Assert.Contains("AggregateGroup [ResultAggregateGroup; keys: 1; typed aggs: 1]", text);
        Assert.Contains("GetOrAddSingleKeyAggregateGroup [group = groups[p.Name] by p.Name; typed: ResultAggregateGroup]", text);
        Assert.Contains("TypedAggregateSet [Set(group.__agg0, o.Description)]", text);
    }

}
