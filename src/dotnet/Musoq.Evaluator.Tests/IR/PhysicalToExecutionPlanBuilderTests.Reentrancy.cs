using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Logical.Nodes;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;
using Musoq.Evaluator.IR.Planning;

namespace Musoq.Evaluator.Tests.IR;

public sealed partial class PhysicalToExecutionPlanBuilderTests
{
    [TestMethod]
    public void Build_WhenBuilderInstanceIsReusedAfterCteSidecarPlan_ShouldNotLeakLoweringState()
    {
        var sidecarPlan = CreateSidecarHashJoinCtePlan();
        var plainPlan = CreatePlainProjectionPlan();
        var options = new CompilationOptions(
            useHashJoin: true,
            useSortMergeJoin: false,
            useCteSidecarIndexes: true);
        var shapeResolver = new ExecutionShapeResolver(
            entityTypesByAlias: new Dictionary<string, Type>
            {
                ["p"] = typeof(Person),
                ["q"] = typeof(Person)
            });
        var builder = CreateReusablePhysicalBuilder(sidecarPlan, shapeResolver, options);

        var sidecarResult = builder.Build(sidecarPlan, "Q_Sidecar");
        var sidecarExecutionPlan = RequireExecutionPlan(sidecarResult);

        Assert.IsTrue(CollectNodes<ExecutionCteSidecarIndexBuildCandidate>(sidecarExecutionPlan.Body).Any());
        Assert.IsTrue(CollectNodes<ExecutionCteSidecarAppendRewriteCandidate>(sidecarExecutionPlan.Body).Any());
        Assert.IsTrue(CollectNodes<ExecutionCteSidecarIndexLoadCandidate>(sidecarExecutionPlan.Body).Any());

        var plainResult = builder.Build(plainPlan, "Q_PlainAfterSidecar");
        var plainExecutionPlan = RequireExecutionPlan(plainResult);

        Assert.AreEqual("Q_PlainAfterSidecar", plainExecutionPlan.Identifier);
        AssertFinalShapeResult(plainExecutionPlan, "result", "ResultRow0", "Name");
        Assert.IsFalse(CollectNodes<ExecutionCteSidecarIndexBuildCandidate>(plainExecutionPlan.Body).Any());
        Assert.IsFalse(CollectNodes<ExecutionCteSidecarAppendRewriteCandidate>(plainExecutionPlan.Body).Any());
        Assert.IsFalse(CollectNodes<ExecutionCteSidecarIndexLoadCandidate>(plainExecutionPlan.Body).Any());
    }

    private static PhysicalToExecutionPlanBuilder CreateReusablePhysicalBuilder(
        PhysicalNode planningRoot,
        ExecutionShapeResolver shapeResolver,
        CompilationOptions options)
    {
        var executionStrategies = ExecutionStrategyPlanner
            .Plan(planningRoot, options, null, new ExecutionPlanningShapeResolverAdapter(shapeResolver))
            .Strategies;
        var executionArtifacts = new ExecutionPlanningArtifacts(
            executionStrategies,
            new Dictionary<string, SourceInteractionPlan>(StringComparer.Ordinal),
            []);

        return new PhysicalToExecutionPlanBuilder(
            shapeResolver,
            null,
            options,
            null,
            executionArtifacts);
    }

    private static PhysicalCteNode CreateSidecarHashJoinCtePlan()
    {
        var definition = new PhysicalProjectNode(
            [
                new ProjectedField("Name", new ColumnRef("q", "Name", typeof(string)), 0),
                new ProjectedField("Age", new ColumnRef("q", "Age", typeof(int)), 1)
            ],
            CreateScan("q"));
        var cteRef = new PhysicalCteRefNode("indexed", "i", new OutputSchema(
        [
            new ColumnSchema("Name", typeof(string), 0),
            new ColumnSchema("Age", typeof(int), 1)
        ]));
        var join = new PhysicalHashJoinNode(
            JoinKind.Inner,
            [new ColumnRef("i", "Age", typeof(int))],
            [new ColumnRef("p", "Age", typeof(int))],
            null,
            CreateScan("p"),
            cteRef);
        var query = new PhysicalProjectNode(
            [
                new ProjectedField("Name", new ColumnRef("p", "Name", typeof(string)), 0),
                new ProjectedField("IndexedName", new ColumnRef("i", "Name", typeof(string)), 1)
            ],
            join);

        return new PhysicalCteNode([new PhysicalCteDefinition("indexed", definition)], query);
    }

    private static PhysicalProjectNode CreatePlainProjectionPlan()
    {
        return new PhysicalProjectNode(
            [new ProjectedField("Name", new ColumnRef("p", "Name", typeof(string)), 0)],
            CreateScan("p"));
    }
}
