using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Logical.Nodes;
using Musoq.Evaluator.IR.Physical.Nodes;
using Musoq.Evaluator.IR.Planning;
using Musoq.Evaluator.Visitors.Helpers.CteDependencyGraph;
using Musoq.Plugins;
using ExecutionStrategyPlan = Musoq.Evaluator.IR.Planning.ExecutionStrategyPlan;

namespace Musoq.Evaluator.Tests.IR;

[TestClass]
public sealed partial class PhysicalToExecutionPlanBuilderTests
{
    private static PhysicalSchemaScanNode CreateScan(string alias = "p")
    {
        return new PhysicalSchemaScanNode(
            "test",
            "data",
            [],
            alias,
            [],
            [],
            new OutputSchema(
            [
                new ColumnSchema("Name", typeof(string), 0),
                new ColumnSchema("Age", typeof(int), 1)
            ]));
    }

    private static IEnumerable<TNode> CollectNodes<TNode>(ExecutionBlock block)
        where TNode : ExecutionNode
    {
        return ExecutionIrAnalysis.CollectNodes<TNode>(block);
    }

    private static ExecutionPlan RequireExecutionPlan(ExecutionPlanBuildResult result) =>
        result.ExecutionPlan ??
        throw new AssertFailedException(result.UnsupportedReason ?? "Expected an execution plan.");

    private static string RequireUnsupportedReason(ExecutionPlanBuildResult result) =>
        result.UnsupportedReason ?? throw new AssertFailedException("Expected an unsupported reason.");

    private static CteExecutionPlan CreateIndependentCteExecutionPlan(params string[] names)
    {
        var nodes = new Dictionary<string, CteGraphNode>(StringComparer.OrdinalIgnoreCase);
        foreach (var name in names)
        {
            nodes[name] = new CteGraphNode(name, null)
            {
                IsReachable = true,
                ExecutionLevel = 0
            };
        }

        var outer = new CteGraphNode(CteGraphNode.OuterQueryNodeName, null);
        foreach (var name in names)
            outer.Dependencies.Add(name);

        var graph = new CteDependencyGraph(nodes, outer);
        return new CteExecutionPlan(
            [new CteExecutionLevel(0, names.Select(name => nodes[name]).ToArray())],
            graph);
    }

    private static PhysicalSchemaScanNode CreateOrderScan()
    {
        return new PhysicalSchemaScanNode(
            "test",
            "orders",
            [],
            "o",
            [],
            [],
            new OutputSchema(
            [
                new ColumnSchema("PersonAge", typeof(int), 0),
                new ColumnSchema("Description", typeof(string), 1)
            ]));
    }

    private static PhysicalSchemaScanNode CreateApplyItemScan()
    {
        return new PhysicalSchemaScanNode(
            "test",
            "items",
            [],
            "i",
            [],
            [],
            new OutputSchema(
            [
                new ColumnSchema("Name", typeof(string), 0),
                new ColumnSchema("Numbers", typeof(int[]), 1)
            ]));
    }

    private static PhysicalPropertySourceNode CreateNumbersPropertySource(string sourceAlias, string alias)
    {
        return new PhysicalPropertySourceNode(
            sourceAlias,
            [new Musoq.Parser.Nodes.From.PropertyFromNode.PropertyNameAndTypePair("Numbers", typeof(int[]))],
            alias,
            0,
            typeof(int[]),
            ApplyKind.Cross,
            new OutputSchema([new ColumnSchema("Value", typeof(int), 0)]));
    }

    private static PhysicalCteRefNode CreateCteRef(string cteName, string alias)
    {
        return new PhysicalCteRefNode(cteName, alias, new OutputSchema(
        [
            new ColumnSchema("Name", typeof(string), 0)
        ]));
    }

    private static PlannedExecutionBuilder CreateBuilder()
    {
        return CreateBuilder(new CompilationOptions(), null);
    }

    private static PlannedExecutionBuilder CreateSerialBuilder()
    {
        return CreateBuilder(
            new CompilationOptions(parallelizationMode: ParallelizationMode.None),
            null);
    }

    private static PlannedExecutionBuilder CreateBuilder(
        CompilationOptions compilationOptions,
        CteExecutionPlan? cteExecutionPlan)
    {
        var shapeResolver = new ExecutionShapeResolver(
            entityTypesByAlias: new Dictionary<string, Type>
            {
                ["p"] = typeof(Person)
            });

        return new PlannedExecutionBuilder(
            shapeResolver,
            compilationOptions,
            cteExecutionPlan);
    }

    private static PlannedExecutionBuilder CreateBuilder(ExecutionStrategyPlan executionStrategies)
    {
        var shapeResolver = new ExecutionShapeResolver(
            entityTypesByAlias: new Dictionary<string, Type>
            {
                ["p"] = typeof(Person)
            });

        return new PlannedExecutionBuilder(
            shapeResolver,
            fixedExecutionStrategies: executionStrategies);
    }

    private static ExecutionStrategyPlan CreateExecutionStrategies(IReadOnlyList<RowWidthPruningPlan> rowWidthPruningPlans)
    {
        var strategies = new ExecutionStrategyPlan(
            new HashSet<PhysicalSingleKeyAggregateNode>(),
            new HashSet<PhysicalProjectNode>(),
            new Dictionary<PhysicalCteNode, IReadOnlyList<PlannedParallelCteLevel>>(),
            new Dictionary<PhysicalCteNode, CteStrategyDecision>(), new Dictionary<PhysicalCteNode, CteSidecarIndexPlan>(),
            new Dictionary<PhysicalSetOperationNode, SetOperationStrategyDecision>(),
            new Dictionary<string, SourceBoundaryStrategyPlan>(StringComparer.Ordinal),
            new Dictionary<BoundaryRowShapeKind, RowWidthPruningPlan[]>(), []);

        return strategies.WithRowWidthPruningPlans(rowWidthPruningPlans);
    }

    private static PlannedExecutionBuilder CreateApplyItemBuilder()
    {
        var shapeResolver = new ExecutionShapeResolver(
            entityTypesByAlias: new Dictionary<string, Type>
            {
                ["i"] = typeof(ApplyItem)
            });

        return new PlannedExecutionBuilder(shapeResolver);
    }

    private static PlannedExecutionBuilder CreateJoinBuilder()
    {
        var shapeResolver = new ExecutionShapeResolver(
            entityTypesByAlias: new Dictionary<string, Type>
            {
                ["p"] = typeof(Person),
                ["o"] = typeof(Order),
                ["q"] = typeof(Person)
            });

        return new PlannedExecutionBuilder(shapeResolver);
    }

    private static AggregateBinding CreateCountBinding()
    {
        var libraryType = typeof(LibraryBase);
        var getMethod = libraryType.GetMethod(
            nameof(LibraryBase.Count),
            [typeof(int?), typeof(int)]) ?? throw new InvalidOperationException("Count aggregate declaration was not found.");
        var setMethod = getMethod;
        var kernel = AggregateKernelDescriptor.Create(getMethod);

        return new AggregateBinding(
            "p.Count",
            "p.Count",
            setMethod,
            [new Literal("p.Count", typeof(string)), new Literal(1, typeof(int))],
            getMethod,
            [],
            typeof(long),
            kernel);
    }

    private static AggregateBinding CreateCountDescriptionBinding()
    {
        var libraryType = typeof(LibraryBase);
        var getMethod = libraryType.GetMethod(
            nameof(LibraryBase.Count),
            [typeof(string), typeof(int)]) ?? throw new InvalidOperationException("Count aggregate declaration was not found.");
        var setMethod = getMethod;
        var kernel = AggregateKernelDescriptor.Create(getMethod);

        return new AggregateBinding(
            "p.CountDescription",
            "p.CountDescription",
            setMethod,
            [new Literal("p.CountDescription", typeof(string)), new ColumnRef("o", "Description", typeof(string))],
            getMethod,
            [],
            typeof(long),
            kernel);
    }

    private static AggregateBinding CreateCountNameBinding()
    {
        var libraryType = typeof(LibraryBase);
        var getMethod = libraryType.GetMethod(
            nameof(LibraryBase.Count),
            [typeof(string), typeof(int)]) ?? throw new InvalidOperationException("Count aggregate declaration was not found.");
        var setMethod = getMethod;
        var kernel = AggregateKernelDescriptor.Create(getMethod);

        return new AggregateBinding(
            "p.CountName",
            "p.CountName",
            setMethod,
            [new Literal("p.CountName", typeof(string)), new ColumnRef("p", "Name", typeof(string))],
            getMethod,
            [],
            typeof(long),
            kernel);
    }
}
