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
    public void Build_WhenSourceIsErasedExpando_ShouldRejectLegacyTransfer()
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

        var error = Assert.Throws<Musoq.Evaluator.Exceptions.SourcePlanContractException>(
            () => builder.Build(project, "Q_ErasedSource"));
        Assert.AreEqual(Musoq.Parser.Diagnostics.DiagnosticCode.MQ3071_SourceContractError, error.Code);
        Assert.Contains("native query-scoped rows", error.Message);
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
    public void Build_WhenAsOfJoinRightSourceIsErasedExpando_ShouldRejectLegacyTransfer()
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

        var error = Assert.Throws<Musoq.Evaluator.Exceptions.SourcePlanContractException>(
            () => builder.Build(project, "Q_DynamicAsOf"));
        Assert.AreEqual(Musoq.Parser.Diagnostics.DiagnosticCode.MQ3071_SourceContractError, error.Code);
        Assert.Contains("native query-scoped rows", error.Message);
    }
}
