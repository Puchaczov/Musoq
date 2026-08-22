using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.Tests.IR;

public sealed partial class PhysicalToExecutionPlanBuilderTests
{

    [TestMethod]
    public void Build_WhenAggregateInputRepeatsGroupKeyField_ShouldHoistSharedFieldRead()
    {
        var scan = CreateScan();
        var binding = CreateCountNameBinding();
        var aggregate = new PhysicalSingleKeyAggregateNode(
            new ColumnRef("p", "Name", typeof(string)),
            "p.Name",
            typeof(string),
            [binding],
            scan);
        var project = new PhysicalProjectNode(
            [
                new ProjectedField("Name", new ColumnRef("p", "Name", typeof(string)), 0),
                new ProjectedField("CountName", new AggregateRef("p.CountName", typeof(long)), 1)
            ],
            aggregate);
        var builder = CreateSerialBuilder();

        var result = builder.Build(project, "Q_SingleKeyAggregateSharedRead");
        var plan = RequireExecutionPlan(result);

        var expected = string.Join("\n",
            "ExecutionPlan [Q_SingleKeyAggregateSharedRead]",
            "  Shapes",
            "    SourceEntity [p: Person]",
            "      Name: string <- property Name",
            "      Age: int <- property Age",
            "    AggregateGroup [ResultAggregateGroup; keys: 1; typed aggs: 1]",
            "    Generated [ResultRow0]",
            "      Name: string <- field Name",
            "      CountName: long <- field CountName",
            string.Empty,
            "  Body",
            "    SourceScan [p: Person] -> pRows",
            "    CreateShapeRows [result: ResultShape0 from ResultRow0]",
            "    CreateSingleKeyAggregateContext [groups: string -> ResultAggregateGroup]",
            "    ChunkedForEach [p in pRows]",
            "      GetOrAddSingleKeyAggregateGroup [group = groups[p.Name] by p.Name; typed: ResultAggregateGroup]",
            "      TypedAggregateSet [Set(group.__agg0, p.Name)]",
            "    EnsureShapeCapacity [result <- Candidate(result <- groupsToFinalize.Count)]",
            "    ForEach [finalGroup in groupsToFinalize]",
            "      AppendShape [result <- ResultShape0(Name: finalGroup.p.Name, CountName: Count('p.CountName'))]",
            "    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]");

        Assert.AreEqual(expected, PrintPlanWithoutPhaseBoundaries(plan));
    }

    [TestMethod]
    public void Build_WhenPlanHasSingleKeyAggregateWithComputedProjection_ShouldReturnAggregateExecutionPlan()
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
                new ProjectedField(
                    "CountPlusOne",
                    new BinaryOp(
                        BinaryOpKind.Add,
                        new AggregateRef("p.Count", typeof(long)),
                        new Literal(1L, typeof(long)),
                        typeof(long)),
                    1)
            ],
            aggregate);
        var builder = CreateSerialBuilder();

        var result = builder.Build(project, "Q_SingleKeyAggregateComputedProjection");
        var plan = RequireExecutionPlan(result);

        var expected = string.Join("\n",
            "ExecutionPlan [Q_SingleKeyAggregateComputedProjection]",
            "  Shapes",
            "    SourceEntity [p: Person]",
            "      Name: string <- property Name",
            "      Age: int <- property Age",
            "    AggregateGroup [ResultAggregateGroup; keys: 1; typed aggs: 1]",
            "    Generated [ResultRow0]",
            "      Name: string <- field Name",
            "      CountPlusOne: long <- field CountPlusOne",
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
            "      AppendShape [result <- ResultShape0(Name: finalGroup.p.Name, CountPlusOne: (Count('p.Count') + 1))]",
            "    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]");

        Assert.AreEqual(expected, PrintPlanWithoutPhaseBoundaries(plan));
    }

}
