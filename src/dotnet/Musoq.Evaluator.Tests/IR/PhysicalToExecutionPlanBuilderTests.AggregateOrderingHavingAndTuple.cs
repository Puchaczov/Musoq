using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.Tests.IR;

public sealed partial class PhysicalToExecutionPlanBuilderTests
{
    [TestMethod]
    public void Build_WhenPlanHasSingleKeyAggregateWithAggregateOrderBy_ShouldReturnSortedAggregateExecutionPlan()
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
        var sort = new PhysicalSortNode(
            [new OrderField(new AggregateRef("p.Count", typeof(long)), Descending: true)],
            project);
        var builder = CreateSerialBuilder();

        var result = builder.Build(sort, "Q_SingleKeyAggregateOrderBy");
        var plan = RequireExecutionPlan(result);

        var expected = string.Join("\n",
            "ExecutionPlan [Q_SingleKeyAggregateOrderBy]",
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
            "    SortShapeRows [result -> resultSorted by Count DESC; capacity: Candidate(resultSorted <- result.Count)]",
            "    ReturnDeferredTable [resultSorted: ResultRow0 <- ResultShape0]");

        Assert.AreEqual(expected, ExecutionPlanPrinter.Print(plan));
    }

    [TestMethod]
    public void Build_WhenPlanHasSingleKeyAggregateWithComputedGroupKey_ShouldReturnAggregateExecutionPlan()
    {
        var scan = CreateScan();
        var binding = CreateCountBinding();
        var groupKey = new BinaryOp(
            BinaryOpKind.Add,
            new ColumnRef("p", "Age", typeof(int)),
            new Literal(1, typeof(int)),
            typeof(int));
        var aggregate = new PhysicalSingleKeyAggregateNode(
            groupKey,
            "p.AgePlusOne",
            typeof(int),
            [binding],
            scan);
        var project = new PhysicalProjectNode(
            [
                new ProjectedField("AgePlusOne", groupKey, 0),
                new ProjectedField("Count", new AggregateRef("p.Count", typeof(long)), 1)
            ],
            aggregate);
        var builder = CreateSerialBuilder();

        var result = builder.Build(project, "Q_SingleKeyAggregateComputedGroupKey");
        var plan = RequireExecutionPlan(result);

        var expected = string.Join("\n",
            "ExecutionPlan [Q_SingleKeyAggregateComputedGroupKey]",
            "  Shapes",
            "    SourceEntity [p: Person]",
            "      Name: string <- property Name",
            "      Age: int <- property Age",
            "    AggregateGroup [ResultAggregateGroup; keys: 1; typed aggs: 1]",
            "    Generated [ResultRow0]",
            "      AgePlusOne: int <- field AgePlusOne",
            "      Count: long <- field Count_",
            string.Empty,
            "  Body",
            "    SourceScan [p: Person] -> pRows",
            "    CreateShapeRows [result: ResultShape0 from ResultRow0]",
            "    CreateSingleKeyAggregateContext [groups: int -> ResultAggregateGroup]",
            "    ChunkedForEach [p in pRows]",
            "      GetOrAddSingleKeyAggregateGroup [group = groups[(p.Age + 1)] by p.AgePlusOne; typed: ResultAggregateGroup]",
            "      TypedAggregateSet [Set(group.__agg0, 1)]",
            "    EnsureShapeCapacity [result <- Candidate(result <- groupsToFinalize.Count)]",
            "    ForEach [finalGroup in groupsToFinalize]",
            "      AppendShape [result <- ResultShape0(AgePlusOne: finalGroup.p.AgePlusOne, Count: Count('p.Count'))]",
            "    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]");

        Assert.AreEqual(expected, ExecutionPlanPrinter.Print(plan));
    }

    [TestMethod]
    public void Build_WhenPlanHasSingleKeyAggregateWithHaving_ShouldReturnGuardedAggregateExecutionPlan()
    {
        var scan = CreateScan();
        var binding = CreateCountBinding();
        var aggregate = new PhysicalSingleKeyAggregateNode(
            new ColumnRef("p", "Name", typeof(string)),
            "p.Name",
            typeof(string),
            [binding],
            scan);
        var having = new PhysicalHavingFilterNode(
            new BinaryOp(
                BinaryOpKind.GreaterThan,
                new AggregateRef("p.Count", typeof(long)),
                new Literal(1, typeof(int)),
                typeof(bool)),
            aggregate);
        var project = new PhysicalProjectNode(
            [
                new ProjectedField("Name", new ColumnRef("p", "Name", typeof(string)), 0),
                new ProjectedField("Count", new AggregateRef("p.Count", typeof(long)), 1)
            ],
            having);
        var builder = CreateSerialBuilder();

        var result = builder.Build(project, "Q_SingleKeyAggregateHaving");
        var plan = RequireExecutionPlan(result);

        var expected = string.Join("\n",
            "ExecutionPlan [Q_SingleKeyAggregateHaving]",
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
            "      If [(Count('p.Count') > 1)]",
            "        AppendShape [result <- ResultShape0(Name: finalGroup.p.Name, Count: Count('p.Count'))]",
            "    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]");

        Assert.AreEqual(expected, ExecutionPlanPrinter.Print(plan));
    }

    [TestMethod]
    public void Build_WhenPlanHasValueTupleAggregate_ShouldReturnAggregateExecutionPlan()
    {
        var scan = CreateScan();
        var binding = CreateCountBinding();
        var aggregate = new PhysicalValueTupleAggregateNode(
            [new ColumnRef("p", "Name", typeof(string)), new ColumnRef("p", "Age", typeof(int))],
            ["p.Name", "p.Age"],
            [typeof(string), typeof(int)],
            [binding],
            scan);
        var project = new PhysicalProjectNode(
            [
                new ProjectedField("Name", new ColumnRef("p", "Name", typeof(string)), 0),
                new ProjectedField("Age", new ColumnRef("p", "Age", typeof(int)), 1),
                new ProjectedField("Count", new AggregateRef("p.Count", typeof(long)), 2)
            ],
            aggregate);
        var builder = CreateBuilder();

        var result = builder.Build(project, "Q_ValueTupleAggregate");
        var plan = RequireExecutionPlan(result);

        var expected = string.Join("\n",
            "ExecutionPlan [Q_ValueTupleAggregate]",
            "  Shapes",
            "    SourceEntity [p: Person]",
            "      Name: string <- property Name",
            "      Age: int <- property Age",
            "    AggregateGroup [ResultAggregateGroup; keys: 2; typed aggs: 1]",
            "    Generated [ResultRow0]",
            "      Name: string <- field Name",
            "      Age: int <- field Age",
            "      Count: long <- field Count_",
            string.Empty,
            "  Body",
            "    SourceScan [p: Person] -> pRows",
            "    CreateShapeRows [result: ResultShape0 from ResultRow0]",
            "    CreateValueTupleAggregateContext [groups: (string, int) -> ResultAggregateGroup]",
            "    ChunkedForEach [p in pRows]",
            "      GetOrAddValueTupleAggregateGroup [group = groups[(p.Name, p.Age)] by p.Name, p.Age; typed: ResultAggregateGroup]",
            "      TypedAggregateSet [Set(group.__agg0, 1)]",
            "    EnsureShapeCapacity [result <- Candidate(result <- groupsToFinalize.Count)]",
            "    ForEach [finalGroup in groupsToFinalize]",
            "      AppendShape [result <- ResultShape0(Name: finalGroup.p.Name, Age: finalGroup.p.Age, Count: Count('p.Count'))]",
            "    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]");

        Assert.AreEqual(expected, ExecutionPlanPrinter.Print(plan));
    }
}
