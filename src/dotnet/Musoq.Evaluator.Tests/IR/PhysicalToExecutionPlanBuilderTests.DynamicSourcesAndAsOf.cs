using System;
using System.Collections.Generic;
using System.Dynamic;
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
    public void Build_WhenSourceIsExpando_ShouldReturnAdapterShape()
    {
        var scan = CreateScan();
        var project = new PhysicalProjectNode(
            [new ProjectedField("Name", new ColumnRef("p", "Name", typeof(string)), 0)],
            scan);
        var shapeResolver = new ExecutionShapeResolver(
            entityTypesByAlias: new Dictionary<string, Type>
            {
                ["p"] = typeof(ExpandoObject)
            });
        var builder = new PlannedExecutionBuilder(shapeResolver);

        var result = builder.Build(project, "Q_Dynamic");
        var plan = RequireExecutionPlan(result);

        var expected = string.Join("\n",
            "ExecutionPlan [Q_Dynamic]",
            "  Shapes",
            "    ExpandoAdapter [p: pDynamicRow0]",
            "      Name: string <- expando key \"Name\"",
            "      Age: int <- expando key \"Age\"",
            "    Generated [ResultRow0]",
            "      Name: string <- field Name",
            string.Empty,
            "  Body",
            "    SourceScan [p: ExpandoObject] -> pRows",
            "    CreateShapeRows [result: ResultShape0 from ResultRow0]",
            "    ChunkedForEach [pResolver in pRows]",
            "      AdaptExpando [p: pDynamicRow0 <- pResolver]",
            "      AppendShape [result <- ResultShape0(Name: p.Name)]",
            "    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]");

        Assert.AreEqual(expected, ExecutionPlanPrinter.Print(plan));
    }

    [TestMethod]
    public void Build_WhenAsOfJoinIsSupported_ShouldCreateIndexBeforeProbeLoop()
    {
        var left = CreateScan();
        var right = CreateScan("q");
        var predicate = new BinaryOp(
            BinaryOpKind.GreaterOrEqual,
            new ColumnRef("p", "Age", typeof(int)),
            new ColumnRef("q", "Age", typeof(int)),
            typeof(bool));
        var join = new PhysicalNestedLoopJoinNode(JoinKind.AsofInner, predicate, left, right);
        var project = new PhysicalProjectNode(
            [
                new ProjectedField("LeftName", new ColumnRef("p", "Name", typeof(string)), 0),
                new ProjectedField("RightName", new ColumnRef("q", "Name", typeof(string)), 1)
            ],
            join);
        var shapeResolver = new ExecutionShapeResolver(
            entityTypesByAlias: new Dictionary<string, Type>
            {
                ["p"] = typeof(Person),
                ["q"] = typeof(Person)
            });
        var builder = new PlannedExecutionBuilder(shapeResolver);

        var result = builder.Build(project, "Q_AsOf");
        var plan = RequireExecutionPlan(result);
        var createIndex = CollectNodes<ExecutionCreateAsOfIndex>(plan.Body).Single();
        var probe = CollectNodes<ExecutionAsOfProbe>(plan.Body).Single();
        var topLevelNodes = plan.Body.Nodes.ToArray();
        var createIndexPosition = Array.FindIndex(topLevelNodes, static node => node is ExecutionCreateAsOfIndex);
        var leftLoopPosition = Array.FindIndex(topLevelNodes, static node => node is ExecutionForEach);

        Assert.AreEqual(createIndex.Index, probe.Index);
        Assert.IsGreaterThanOrEqualTo(0, createIndexPosition);
        Assert.IsGreaterThan(createIndexPosition, leftLoopPosition);
    }

    [TestMethod]
    public void Build_WhenAsOfJoinRightSourceIsExpando_ShouldReturnUnsupportedReason()
    {
        var left = CreateScan();
        var right = CreateScan("q");
        var predicate = new BinaryOp(
            BinaryOpKind.GreaterOrEqual,
            new ColumnRef("p", "Age", typeof(int)),
            new ColumnRef("q", "Age", typeof(int)),
            typeof(bool));
        var join = new PhysicalNestedLoopJoinNode(JoinKind.AsofInner, predicate, left, right);
        var project = new PhysicalProjectNode(
            [
                new ProjectedField("LeftName", new ColumnRef("p", "Name", typeof(string)), 0),
                new ProjectedField("RightName", new ColumnRef("q", "Name", typeof(string)), 1)
            ],
            join);
        var shapeResolver = new ExecutionShapeResolver(
            entityTypesByAlias: new Dictionary<string, Type>
            {
                ["p"] = typeof(Person),
                ["q"] = typeof(ExpandoObject)
            });
        var builder = new PlannedExecutionBuilder(shapeResolver);

        var result = builder.Build(project, "Q_DynamicAsOf");

        Assert.IsFalse(result.Supported);
        Assert.AreEqual(
            "Execution IR ASOF join lowering requires a non-dynamic source-entity or table-row right source. Found ExpandoAdapterShape with row type ExpandoObject.",
            RequireUnsupportedReason(result));
    }
}
