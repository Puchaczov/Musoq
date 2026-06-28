using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Physical.Nodes;
using Musoq.Plugins;

namespace Musoq.Evaluator.Tests.IR;

public sealed partial class PhysicalToExecutionPlanBuilderTests
{
    [TestMethod]
    public void Build_WhenPlanIsPlainScanFilterProject_ShouldReturnExecutionPlan()
    {
        var scan = CreateScan();
        var predicate = new BinaryOp(
            BinaryOpKind.GreaterThan,
            new ColumnRef("p", "Age", typeof(int)),
            new Literal(18, typeof(int)),
            typeof(bool));
        var filter = new PhysicalFilterNode(predicate, scan);
        var project = new PhysicalProjectNode(
            [new ProjectedField("Name", new ColumnRef("p", "Name", typeof(string)), 0)],
            filter);
        var builder = CreateSerialBuilder();

        var result = builder.Build(project, "Q_Plain");
        var plan = RequireExecutionPlan(result);
        AssertFinalShapeResult(plan, "result", "ResultRow0", "Name");

        var expected = string.Join("\n",
            "ExecutionPlan [Q_Plain]",
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
            "      If [(p.Age > 18)]",
            "        AppendShape [result <- ResultShape0(Name: p.Name)]",
            "    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]");

        Assert.AreEqual(expected, ExecutionPlanPrinter.Print(plan));
    }

    [TestMethod]
    public void Build_WhenPlanIsPlainScanFilterProject_ShouldMarkAppendRowForDirectAppend()
    {
        var scan = CreateScan();
        var project = new PhysicalProjectNode(
            [new ProjectedField("Name", new ColumnRef("p", "Name", typeof(string)), 0)],
            scan);
        var builder = CreateBuilder();

        var result = builder.Build(project, "Q_DirectAppend");
        var plan = RequireExecutionPlan(result);
        var appendRow = CollectNodes<ExecutionAppendRow>(plan.Body).Single();

        Assert.AreEqual(ExecutionAppendMode.Direct, appendRow.AppendMode);
    }

    [TestMethod]
    public void Build_WhenParallelizationModeIsFullForPlainScanFilterProject_ShouldUseSerialExecutionPlan()
    {
        var scan = CreateScan();
        var predicate = new BinaryOp(
            BinaryOpKind.GreaterThan,
            new ColumnRef("p", "Age", typeof(int)),
            new Literal(18, typeof(int)),
            typeof(bool));
        var filter = new PhysicalFilterNode(predicate, scan);
        var project = new PhysicalProjectNode(
            [new ProjectedField("Name", new ColumnRef("p", "Name", typeof(string)), 0)],
            filter);
        var builder = CreateBuilder();

        var result = builder.Build(project, "Q_ParallelProject");
        var plan = RequireExecutionPlan(result);
        var planText = ExecutionPlanPrinter.Print(plan);
        var parallelProjects = CollectNodes<ExecutionParallelFilterProjectLoop>(plan.Body).ToArray();

        Assert.IsEmpty(parallelProjects);
        StringAssert.Contains(planText, "ChunkedForEach [p in pRows]");
        StringAssert.Contains(planText, "If [(p.Age > 18)]");
        StringAssert.Contains(planText, "AppendShape [result <- ResultShape0(Name: p.Name)]");
    }

    [TestMethod]
    public void Build_WhenParallelizationModeIsFullForMethodScanFilterProject_ShouldReturnParallelFilterProjectExecutionPlan()
    {
        var scan = CreateScan();
        var toUpper = typeof(LibraryBase).GetMethod(nameof(LibraryBase.ToUpper), [typeof(string)]) ??
                      throw new InvalidOperationException("LibraryBase.ToUpper(string) was not found.");
        var predicate = new BinaryOp(
            BinaryOpKind.GreaterThan,
            new ColumnRef("p", "Age", typeof(int)),
            new Literal(18, typeof(int)),
            typeof(bool));
        var filter = new PhysicalFilterNode(predicate, scan);
        var project = new PhysicalProjectNode(
            [
                new ProjectedField(
                    "Name",
                    new MethodCall(toUpper, [new ColumnRef("p", "Name", typeof(string))], null, typeof(string)),
                    0)
            ],
            filter);
        var builder = CreateBuilder();

        var result = builder.Build(project, "Q_ParallelProject");
        var plan = RequireExecutionPlan(result);
        var parallelProject = CollectNodes<ExecutionParallelFilterProjectLoop>(plan.Body).Single();
        var planText = ExecutionPlanPrinter.Print(plan);

        Assert.AreEqual("p", parallelProject.Source.Name);
        Assert.AreEqual("result", parallelProject.AppendRow.Table.Name);
        StringAssert.Contains(planText, "ParallelFilterProjectLoop [p in pRows where (p.Age > 18); threshold 4096");
        StringAssert.Contains(planText, "ParallelProject");
        StringAssert.Contains(planText, "SequentialKernel");
        Assert.IsFalse(planText.Contains("CreateObjectCandidate", StringComparison.Ordinal));
        StringAssert.Contains(planText, "AppendShape [result <- ResultShape0(Name: ToUpper(p.Name))]");
    }

    [TestMethod]
    public void Build_WhenPlanHasSortSkipTake_ShouldCarryCapacityHintsAndDirectAppend()
    {
        var scan = CreateScan();
        var project = new PhysicalProjectNode(
            [new ProjectedField("Name", new ColumnRef("p", "Name", typeof(string)), 0)],
            scan);
        var sort = new PhysicalSortNode(
            [new OrderField(new ColumnRef(string.Empty, "Name", typeof(string)), Descending: false)],
            project);
        var skip = new PhysicalSkipNode(1, sort);
        var take = new PhysicalTakeNode(2, skip);
        var builder = CreateBuilder();

        var result = builder.Build(take, "Q_TableMetadata");
        var plan = RequireExecutionPlan(result);
        var sortTable = CollectNodes<ExecutionSortTable>(plan.Body).Single();
        var sliceTable = CollectNodes<ExecutionSliceTable>(plan.Body).Single();

        AssertFinalShapeResult(plan, "resultSortedSliced", "ResultRow0", "Name");
        Assert.AreEqual(ExecutionAppendMode.Direct, sortTable.AppendMode);
        Assert.AreEqual(ExecutionAppendMode.Direct, sliceTable.AppendMode);
        Assert.IsInstanceOfType<ExecutionCollectionCountCapacityHintCandidate>(sortTable.CapacityHint);
        Assert.IsInstanceOfType<ExecutionSkipTakeCapacityHintCandidate>(sliceTable.CapacityHint);
    }

    [TestMethod]
    public void Build_WhenPlanHasTopOffset_ShouldUseTypedSkipTakeOrderRecords()
    {
        var scan = CreateScan();
        var project = new PhysicalProjectNode(
            [new ProjectedField("Name", new ColumnRef("p", "Name", typeof(string)), 0)],
            scan);
        var topOffset = new PhysicalTopOffsetNode(
            1,
            2,
            [new OrderField(new ColumnRef(string.Empty, "Name", typeof(string)), Descending: false)],
            project);
        var builder = CreateSerialBuilder();

        var result = builder.Build(topOffset, "Q_TopOffset");
        var plan = RequireExecutionPlan(result);
        var orderRecords = CollectNodes<ExecutionCreateBoundedRecordList>(plan.Body).Single();
        var materialize = CollectNodes<ExecutionMaterializeRecordListToTable>(plan.Body).Single();
        var selection = (ExecutionSkipTakeOrderRecordSelection)orderRecords.Selection;
        var printed = ExecutionPlanPrinter.Print(plan);

        Assert.IsFalse(CollectNodes<ExecutionTopOffsetTable>(plan.Body).Any());
        Assert.IsFalse(CollectNodes<ExecutionOrderRecordList>(plan.Body).Any());
        Assert.AreEqual("resultOrderRecords", orderRecords.List.Name);
        Assert.AreEqual("Name", orderRecords.Keys.Single().FieldName);
        Assert.AreEqual(1, selection.SkipCount);
        Assert.AreEqual(2, selection.TakeCount);
        Assert.AreEqual(ExecutionAppendMode.Direct, materialize.AppendMode);
        Assert.IsInstanceOfType<ExecutionConstantCapacityHintCandidate>(materialize.CapacityHint);
        StringAssert.Contains(printed, "GeneratedRecord [ResultRow0WithSortKeys]");
        StringAssert.Contains(printed, "CreateBoundedRecordList [resultOrderRecords: ResultRow0WithSortKeys by Name ASC, skip 1, take 2]");
        StringAssert.Contains(
            printed,
            "MaterializeRecordListToShapeRows [resultOrderRecords -> result: ResultShape0 fields 0; capacity: Candidate(result <- 2)]");
        StringAssert.Contains(printed, "ReturnDeferredTable [result: ResultRow0 <- ResultShape0]");
    }

    [TestMethod]
    public void Build_WhenSortDoesNotWrapProjection_ShouldReturnReason()
    {
        var scan = CreateScan();
        var sort = new PhysicalSortNode(
            [new OrderField(new ColumnRef("p", "Name", typeof(string)), Descending: false)],
            scan);
        var builder = CreateBuilder();

        var result = builder.Build(sort, "Q_Sort");

        Assert.AreEqual(
            ExecutionPlanBuildResult.CreateUnsupported(
                "Execution IR lowering currently supports Project -> Filter? -> (SchemaScan|CteRef|flat inner Join), aggregate-only direct projections, simple set-operation arms, optional Sort/Skip/Take wrappers, and simple CTE definitions. Found PhysicalSortNode."),
            result);
    }
}
