using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Execution;

namespace Musoq.Evaluator.Tests.IR;

[TestClass]
public sealed class AggregateLoweringModelTests
{
    [TestMethod]
    public void AggregateSetBuildResult_Factories_ShouldPreserveSupportState()
    {
        var nodes = new ExecutionNode[]
        {
            new ExecutionContinue()
        };
        var accumulators = new Dictionary<string, AggregateAccumulatorField>(StringComparer.OrdinalIgnoreCase);

        var success = AggregateSetBuildResult.Success(nodes, accumulators);
        var unsupported = AggregateSetBuildResult.Unsupported("missing kernel");

        Assert.IsTrue(success.IsBuilt);
        Assert.AreSame(nodes, success.Nodes);
        Assert.AreSame(accumulators, success.TypedAccumulators);
        Assert.AreEqual(string.Empty, success.UnsupportedReason);
        Assert.IsFalse(unsupported.IsBuilt);
        Assert.HasCount(0, unsupported.Nodes);
        Assert.HasCount(0, unsupported.TypedAccumulators);
        Assert.AreEqual("missing kernel", unsupported.UnsupportedReason);
    }

    [TestMethod]
    public void AggregateGroupValueCaptureBuildResult_Factories_ShouldPreserveSupportState()
    {
        var nodes = new ExecutionNode[]
        {
            new ExecutionBreak()
        };
        var capturedValues = new Dictionary<string, AggregateCapturedValue>(StringComparer.OrdinalIgnoreCase)
        {
            ["City"] = new AggregateCapturedValue("__city", typeof(string))
        };

        var success = AggregateGroupValueCaptureBuildResult.Success(nodes, capturedValues);
        var unsupported = AggregateGroupValueCaptureBuildResult.Unsupported("raw expression");

        Assert.IsTrue(success.IsBuilt);
        Assert.AreSame(nodes, success.Nodes);
        Assert.AreSame(capturedValues, success.CapturedValues);
        Assert.IsFalse(unsupported.IsBuilt);
        Assert.HasCount(0, unsupported.Nodes);
        Assert.HasCount(0, unsupported.CapturedValues);
        Assert.AreEqual("raw expression", unsupported.UnsupportedReason);
    }

    [TestMethod]
    public void AggregateGroupLowering_Shape_ShouldReturnPlanLeafShape()
    {
        var shape = new AggregateGroupShape("ResultAggregateGroup", [], [], []);
        var plan = new AggregateGroupPlan(shape, [new AggregateGroupLevelPlan(0, shape)]);

        var lowering = new AggregateGroupLowering(
            plan,
            new Dictionary<string, AggregateAccumulatorField>(StringComparer.OrdinalIgnoreCase),
            new Dictionary<string, AggregateCapturedField>(StringComparer.OrdinalIgnoreCase));

        Assert.AreSame(shape, lowering.Shape);
        Assert.AreSame(plan, lowering.Plan);
    }

    [TestMethod]
    public void AggregateLoweringResources_ShouldPreserveComposedResourceParts()
    {
        var shape = new AggregateGroupShape("ResultAggregateGroup", [], [], []);
        var plan = new AggregateGroupPlan(shape, [new AggregateGroupLevelPlan(0, shape)]);
        var group = new AggregateGroupLowering(
            plan,
            new Dictionary<string, AggregateAccumulatorField>(StringComparer.OrdinalIgnoreCase),
            new Dictionary<string, AggregateCapturedField>(StringComparer.OrdinalIgnoreCase));
        var libraryNodes = new ExecutionNode[] { new ExecutionContinue() };
        var setNodes = AggregateSetBuildResult.Success([], new Dictionary<string, AggregateAccumulatorField>(StringComparer.OrdinalIgnoreCase));
        var capture = AggregateGroupValueCaptureBuildResult.Success([], new Dictionary<string, AggregateCapturedValue>(StringComparer.OrdinalIgnoreCase));
        var context = CreateFinalizationContext(shape);

        var resources = new AggregateLoweringResources(group, libraryNodes, setNodes, capture, context);

        Assert.AreSame(group, resources.Group);
        Assert.AreSame(libraryNodes, resources.LibraryNodes);
        Assert.AreSame(setNodes, resources.SetNodes);
        Assert.AreSame(capture, resources.ValueCapture);
        Assert.AreSame(context, resources.FinalizationContext);
    }

    [TestMethod]
    public void AggregateTableCompletion_ShouldPreserveInputs()
    {
        var shape = new AggregateGroupShape("ResultAggregateGroup", [], [], []);
        var plan = new AggregateGroupPlan(shape, [new AggregateGroupLevelPlan(0, shape)]);
        var group = new AggregateGroupLowering(
            plan,
            new Dictionary<string, AggregateAccumulatorField>(StringComparer.OrdinalIgnoreCase),
            new Dictionary<string, AggregateCapturedField>(StringComparer.OrdinalIgnoreCase));
        var resources = new AggregateLoweringResources(
            group,
            [],
            AggregateSetBuildResult.Unsupported("unused"),
            AggregateGroupValueCaptureBuildResult.Unsupported("unused"),
            CreateFinalizationContext(shape));
        var resultTable = new ExecutionVariable("resultTable", typeof(object));
        var resultShape = new GeneratedRowShape("ResultRow", []);
        var groups = new ExecutionVariable("groups", typeof(IReadOnlyList<object>));
        var finalGroup = new ExecutionVariable("finalGroup", typeof(object));

        var completion = new AggregateTableCompletion(
            [shape],
            [new ExecutionContinue()],
            resources,
            resultTable,
            resultShape,
            new ExecutionContinue(),
            new ExecutionBreak(),
            groups,
            finalGroup,
            ExecutionBlock.Empty,
            [],
            IsDistinct: true);

        Assert.AreSame(resources, completion.Aggregate);
        Assert.AreSame(resultTable, completion.ResultTable);
        Assert.AreSame(resultShape, completion.ResultShape);
        Assert.AreSame(groups, completion.GroupsToFinalize);
        Assert.AreSame(finalGroup, completion.FinalGroup);
        Assert.IsTrue(completion.IsDistinct);
    }

    private static AggregateFinalizationContext CreateFinalizationContext(AggregateGroupShape shape)
    {
        return new AggregateFinalizationContext(
            new ExecutionVariable("group", typeof(object)),
            new AggregateFinalizationGroupKeys([], [], []),
            [],
            new Dictionary<string, AggregateBinding>(StringComparer.OrdinalIgnoreCase),
            new Dictionary<string, AggregateCapturedValue>(StringComparer.OrdinalIgnoreCase),
            new Dictionary<string, AggregateAccumulatorField>(StringComparer.OrdinalIgnoreCase),
            shape,
            "aggregate");
    }
}
