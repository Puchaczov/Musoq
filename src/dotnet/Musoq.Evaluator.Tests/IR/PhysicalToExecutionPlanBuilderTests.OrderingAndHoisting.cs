using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Physical.Nodes;
using Musoq.Evaluator.IR.Planning;

namespace Musoq.Evaluator.Tests.IR;

public sealed partial class PhysicalToExecutionPlanBuilderTests
{
    [TestMethod]
    public void Build_WhenProjectionRepeatsFieldRead_ShouldHoistLocalBinding()
    {
        var scan = CreateScan();
        var project = new PhysicalProjectNode(
            [
                new ProjectedField("FirstName", new ColumnRef("p", "Name", typeof(string)), 0),
                new ProjectedField("SecondName", new ColumnRef("p", "Name", typeof(string)), 1)
            ],
            scan);
        var builder = CreateBuilder();

        var result = builder.Build(project, "Q_Hoist");
        var plan = RequireExecutionPlan(result);

        var expected = string.Join("\n",
            "ExecutionPlan [Q_Hoist]",
            "  Shapes",
            "    SourceEntity [p: Person]",
            "      Name: string <- property Name",
            "      Age: int <- property Age",
            "    Generated [ResultRow0]",
            "      FirstName: string <- field FirstName",
            "      SecondName: string <- field SecondName",
            string.Empty,
            "  Body",
            "    SourceScan [p: Person] -> pRows",
            "    CreateShapeRows [result: ResultShape0 from ResultRow0]",
            "    ChunkedForEach [p in pRows]",
            "      AppendShape [result <- ResultShape0(FirstName: p.Name, SecondName: p.Name)]",
            "    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]");

        Assert.AreEqual(expected, ExecutionPlanPrinter.Print(plan));
    }

    [TestMethod]
    public void Build_WhenFilterAndProjectionShareFieldRead_ShouldHoistBeforeGuard()
    {
        var scan = CreateScan();
        var predicate = new BinaryOp(
            BinaryOpKind.GreaterThan,
            new ColumnRef("p", "Age", typeof(int)),
            new Literal(18, typeof(int)),
            typeof(bool));
        var filter = new PhysicalFilterNode(predicate, scan);
        var project = new PhysicalProjectNode(
            [new ProjectedField("Age", new ColumnRef("p", "Age", typeof(int)), 0)],
            filter);
        var builder = CreateSerialBuilder();

        var result = builder.Build(project, "Q_FilterHoist");
        var plan = RequireExecutionPlan(result);

        var expected = string.Join("\n",
            "ExecutionPlan [Q_FilterHoist]",
            "  Shapes",
            "    SourceEntity [p: Person]",
            "      Name: string <- property Name",
            "      Age: int <- property Age",
            "    Generated [ResultRow0]",
            "      Age: int <- field Age",
            string.Empty,
            "  Body",
            "    SourceScan [p: Person] -> pRows",
            "    CreateShapeRows [result: ResultShape0 from ResultRow0]",
            "    ChunkedForEach [p in pRows]",
            "      If [(p.Age > 18)]",
            "        AppendShape [result <- ResultShape0(Age: p.Age)]",
            "    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]");

        Assert.AreEqual(expected, ExecutionPlanPrinter.Print(plan));
    }

    [TestMethod]
    public void Build_WhenPlanHasSortSkipTake_ShouldAddTableOperations()
    {
        var scan = CreateScan();
        var project = new PhysicalProjectNode(
            [new ProjectedField("Name", new ColumnRef("p", "Name", typeof(string)), 0)],
            scan);
        var sort = new PhysicalSortNode(
            [new OrderField(new ColumnRef("p", "Name", typeof(string)), Descending: true)],
            project);
        var skip = new PhysicalSkipNode(1, sort);
        var take = new PhysicalTakeNode(2, skip);
        var builder = CreateBuilder();

        var result = builder.Build(take, "Q_SortPage");
        var plan = RequireExecutionPlan(result);

        var expected = string.Join("\n",
            "ExecutionPlan [Q_SortPage]",
            "  Shapes",
            "    SourceEntity [p: Person]",
            "      Name: string <- property Name",
            "      Age: int <- property Age",
            "    Generated [ResultRow0]",
            "      Name: string <- field Name",
            string.Empty,
            "  Body",
            "    SourceScan [p: Person] -> pRows",
            "    CreateShapeRows [result: ResultShape0 from ResultRow0]",
            "    ChunkedForEach [p in pRows]",
            "      AppendShape [result <- ResultShape0(Name: p.Name)]",
            "    SortShapeRows [result -> resultSorted by Name DESC; capacity: Candidate(resultSorted <- result.Count)]",
            "    SliceShapeRows [resultSorted -> resultSortedSliced, skip 1, take 2; capacity: Candidate(resultSortedSliced <- Min(Max(resultSorted.Count - 1, 0), 2))]",
            "    ReturnDeferredTable [resultSortedSliced: ResultRow0 <- ResultShape0]");

        Assert.AreEqual(expected, ExecutionPlanPrinter.Print(plan));
    }

    [TestMethod]
    public void Build_WhenPlanHasTopN_ShouldUseTypedTakeOrderRecords()
    {
        var scan = CreateScan();
        var project = new PhysicalProjectNode(
            [new ProjectedField("Name", new ColumnRef("p", "Name", typeof(string)), 0)],
            scan);
        var topN = new PhysicalTopNNode(
            2,
            [new OrderField(new ColumnRef("p", "Name", typeof(string)), Descending: true)],
            project);
        var builder = CreateBuilder();

        var result = builder.Build(topN, "Q_TopN");
        var plan = RequireExecutionPlan(result);
        var orderRecords = CollectNodes<ExecutionCreateBoundedRecordList>(plan.Body).Single();
        var materialize = CollectNodes<ExecutionMaterializeRecordListToTable>(plan.Body).Single();
        var selection = (ExecutionTakeOrderRecordSelection)orderRecords.Selection;
        var printed = ExecutionPlanPrinter.Print(plan);

        Assert.IsFalse(CollectNodes<ExecutionTopNTable>(plan.Body).Any());
        Assert.IsFalse(CollectNodes<ExecutionOrderRecordList>(plan.Body).Any());
        Assert.AreEqual("resultOrderRecords", orderRecords.List.Name);
        Assert.AreEqual("Name", orderRecords.Keys.Single().FieldName);
        Assert.IsTrue(orderRecords.Keys.Single().Descending);
        Assert.AreEqual(2, selection.Count);
        Assert.AreEqual(ExecutionAppendMode.Direct, materialize.AppendMode);
        Assert.IsInstanceOfType<ExecutionConstantCapacityHintCandidate>(materialize.CapacityHint);
        StringAssert.Contains(printed, "GeneratedRecord [ResultRow0WithSortKeys]");
        StringAssert.Contains(printed, "CreateBoundedRecordList [resultOrderRecords: ResultRow0WithSortKeys by Name DESC, take 2]");
        StringAssert.Contains(printed, "AppendRecord [resultOrderRecords <- ResultRow0WithSortKeys(Name: p.Name)]");
        StringAssert.Contains(
            printed,
            "MaterializeRecordListToShapeRows [resultOrderRecords -> result: ResultShape0 fields 0; capacity: Candidate(result <- 2)]");
        StringAssert.Contains(printed, "ReturnDeferredTable [result: ResultRow0 <- ResultShape0]");
    }

    [TestMethod]
    public void Build_WhenTopNOrdersByNonProjectedField_ShouldMaterializeHiddenOrderKey()
    {
        var scan = CreateScan();
        var project = new PhysicalProjectNode(
            [new ProjectedField("Name", new ColumnRef("p", "Name", typeof(string)), 0)],
            scan);
        var topN = new PhysicalTopNNode(
            2,
            [new OrderField(new ColumnRef("p", "Age", typeof(int)), Descending: false)],
            project);
        var builder = CreateBuilder();

        var result = builder.Build(topN, "Q_TopNHiddenOrderKey");
        var plan = RequireExecutionPlan(result);
        var printed = ExecutionPlanPrinter.Print(plan);

        StringAssert.Contains(
            printed,
            "AppendRecord [resultOrderRecords <- ResultRow0WithSortKeys(Name: p.Name, __sortKey0: p.Age)]");
        StringAssert.Contains(
            printed,
            "CreateBoundedRecordList [resultOrderRecords: ResultRow0WithSortKeys by __sortKey0 ASC, take 2]");
        StringAssert.Contains(
            printed,
            "MaterializeRecordListToShapeRows [resultOrderRecords -> result: ResultShape0 fields 0; capacity: Candidate(result <- 2)]");
    }

    [TestMethod]
    public void Build_WhenProvidedRowWidthPlanIsDiagnosticOnlyForHiddenOrderKey_ShouldRejectPruning()
    {
        var scan = CreateScan();
        var project = new PhysicalProjectNode(
            [new ProjectedField("Name", new ColumnRef("p", "Name", typeof(string)), 0)],
            scan);
        var topN = new PhysicalTopNNode(
            2,
            [new OrderField(new ColumnRef("p", "Age", typeof(int)), Descending: false)],
            project);
        var strategies = CreateExecutionStrategies(
        [
            new RowWidthPruningPlan(
                "top-n:0",
                BoundaryRowShapeKind.TopN,
                RowWidthPruningStrategy.DiagnosticOnly,
                ["p.Age"],
                [],
                PlanningConfidence.Medium,
                "Test diagnostic-only row-width pruning plan.")
        ]);
        var builder = CreateBuilder(strategies);

        var result = builder.Build(topN, "Q_TopNHiddenOrderKeyDiagnosticOnly");

        Assert.IsFalse(result.Supported);
        Assert.Contains("requires an applied RowWidthPruningPlan for TopN", RequireUnsupportedReason(result));
    }
}
