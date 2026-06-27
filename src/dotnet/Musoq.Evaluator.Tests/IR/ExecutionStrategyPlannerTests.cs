using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;
using Musoq.Evaluator.IR.Planning;
using Musoq.Plugins;
using Musoq.Plugins.Attributes;

namespace Musoq.Evaluator.Tests.IR;

[TestClass]
public sealed class ExecutionStrategyPlannerTests
{
    [TestMethod]
    public void Plan_WhenParallelFilterProjectHasSafeMethod_ShouldEnableStrategy()
    {
        var project = CreateMethodProject(CreateScan(), nameof(StableMethod));

        var result = Plan(project);

        AssertOutcome(result, "ParallelFilterProject", "Enabled");
        Assert.IsTrue(result.Strategies.CanUseParallelFilterProject(project));
    }

    [TestMethod]
    public void Plan_WhenParallelizationModeDisablesFilterProject_ShouldDisableStrategy()
    {
        var project = CreateMethodProject(CreateScan(), nameof(StableMethod));

        var result = Plan(project, new CompilationOptions(parallelizationMode: ParallelizationMode.None));

        AssertOutcome(result, "ParallelFilterProject", "Disabled");
        Assert.IsFalse(result.Strategies.CanUseParallelFilterProject(project));
    }

    [TestMethod]
    public void Plan_WhenFilterProjectHasDynamicSourceShape_ShouldSkipStrategy()
    {
        var project = CreateMethodProject(CreateScan(), nameof(StableMethod));

        var result = Plan(project, CreateShapeResolver(typeof(IReadOnlyDictionary<string, object>)));

        AssertReason(result, "ParallelFilterProject", "Source shape is dynamic");
        Assert.IsFalse(result.Strategies.CanUseParallelFilterProject(project));
    }

    [TestMethod]
    public void Plan_WhenFilterProjectHasPostOperations_ShouldSkipStrategy()
    {
        var project = CreateMethodProject(CreateScan(), nameof(StableMethod));
        var sort = new PhysicalSortNode(
            [new OrderField(new ColumnRef(string.Empty, "Name", typeof(string)), Descending: false)],
            project);

        var result = Plan(sort);

        AssertReason(result, "ParallelFilterProject", "Post-operations are present");
        Assert.IsFalse(result.Strategies.CanUseParallelFilterProject(project));
    }

    [TestMethod]
    public void Plan_WhenFilterProjectHasNonDeterministicMethod_ShouldSkipStrategy()
    {
        var project = CreateMethodProject(CreateScan(), nameof(NonDeterministicMethod));

        var result = Plan(project);

        AssertReason(result, "ParallelFilterProject", "non-deterministic method");
        Assert.IsFalse(result.Strategies.CanUseParallelFilterProject(project));
    }

    [TestMethod]
    public void Plan_WhenFilterProjectInjectsQueryStats_ShouldSkipStrategy()
    {
        var project = CreateMethodProject(CreateScan(), nameof(StatsMethod));

        var result = Plan(project);

        AssertReason(result, "ParallelFilterProject", "injects query statistics");
        Assert.IsFalse(result.Strategies.CanUseParallelFilterProject(project));
    }

    [TestMethod]
    public void Plan_WhenFilterProjectHasNoMethodHeavyExpression_ShouldSkipStrategy()
    {
        var scan = CreateScan();
        var project = new PhysicalProjectNode(
            [new ProjectedField("Name", new ColumnRef("p", "Name", typeof(string)), 0)],
            scan);

        var result = Plan(project);

        AssertReason(result, "ParallelFilterProject", "no method-heavy expression");
        Assert.IsFalse(result.Strategies.CanUseParallelFilterProject(project));
    }

    [TestMethod]
    public void Plan_WhenSingleKeyAggregateHasMergeableKernel_ShouldEnableStrategy()
    {
        var aggregate = CreateSingleKeyAggregate(CreateScan());
        var project = CreateAggregateProject(aggregate);

        var result = Plan(project);

        AssertOutcome(result, "ParallelSingleKeyAggregate", "Enabled");
        Assert.IsTrue(result.Strategies.CanUseParallelSingleKeyAggregate(aggregate));
    }

    [TestMethod]
    public void Plan_WhenSingleKeyAggregateHasHavingFilter_ShouldEnableStrategy()
    {
        var aggregate = CreateSingleKeyAggregate(CreateScan());
        var having = new PhysicalHavingFilterNode(
            new BinaryOp(
                BinaryOpKind.GreaterThan,
                new AggregateRef("p.Count", typeof(long)),
                new Literal(0, typeof(int)),
                typeof(bool)),
            aggregate);
        var project = CreateAggregateProject(having);

        var result = Plan(project);

        AssertOutcome(result, "ParallelSingleKeyAggregate", "Enabled");
        Assert.IsTrue(result.Strategies.CanUseParallelSingleKeyAggregate(aggregate));
    }

    [TestMethod]
    public void Plan_WhenSingleKeyAggregateHasSourceFilter_ShouldSkipStrategy()
    {
        var filter = new PhysicalFilterNode(
            new BinaryOp(
                BinaryOpKind.GreaterThan,
                new ColumnRef("p", "Age", typeof(int)),
                new Literal(18, typeof(int)),
                typeof(bool)),
            CreateScan());
        var aggregate = CreateSingleKeyAggregate(filter);
        var project = CreateAggregateProject(aggregate);

        var result = Plan(project);

        AssertReason(result, "ParallelSingleKeyAggregate", "Source filter is present");
        Assert.IsFalse(result.Strategies.CanUseParallelSingleKeyAggregate(aggregate));
    }

    private static ExecutionStrategyPlanningResult Plan(PhysicalNode node)
    {
        return Plan(node, new CompilationOptions());
    }

    private static ExecutionStrategyPlanningResult Plan(PhysicalNode node, CompilationOptions options)
    {
        return ExecutionStrategyPlanner.Plan(node, options, null, CreateShapeResolver(typeof(Person)));
    }

    private static ExecutionStrategyPlanningResult Plan(PhysicalNode node, ExecutionShapeResolver shapeResolver)
    {
        return ExecutionStrategyPlanner.Plan(node, new CompilationOptions(), null, shapeResolver);
    }

    private static void AssertOutcome(
        ExecutionStrategyPlanningResult result,
        string ruleName,
        string expectedOutcome)
    {
        var decision = FindDecision(result, ruleName);

        Assert.AreEqual(expectedOutcome, decision.Outcome);
    }

    private static void AssertReason(
        ExecutionStrategyPlanningResult result,
        string ruleName,
        string expectedReasonPart)
    {
        var decision = FindDecision(result, ruleName);

        StringAssert.Contains(decision.Reason, expectedReasonPart);
    }

    private static PlanningDecision FindDecision(ExecutionStrategyPlanningResult result, string ruleName)
    {
        return result.Decisions.Single(decision => decision.RuleName == ruleName);
    }

    private static ExecutionShapeResolver CreateShapeResolver(Type entityType)
    {
        return new ExecutionShapeResolver(
            entityTypesByAlias: new Dictionary<string, Type>
            {
                ["p"] = entityType
            });
    }

    private static PhysicalSchemaScanNode CreateScan()
    {
        return new PhysicalSchemaScanNode(
            "test",
            "data",
            [],
            "p",
            [],
            [],
            new OutputSchema(
            [
                new ColumnSchema("Name", typeof(string), 0),
                new ColumnSchema("Age", typeof(int), 1)
            ]));
    }

    private static PhysicalProjectNode CreateMethodProject(PhysicalNode input, string methodName)
    {
        var filter = new PhysicalFilterNode(
            new BinaryOp(
                BinaryOpKind.GreaterThan,
                new ColumnRef("p", "Age", typeof(int)),
                new Literal(18, typeof(int)),
                typeof(bool)),
            input);

        return new PhysicalProjectNode(
            [
                new ProjectedField(
                    "Name",
                    new MethodCall(ResolveMethod(methodName), [new ColumnRef("p", "Name", typeof(string))], null, typeof(string)),
                    0)
            ],
            filter);
    }

    private static MethodInfo ResolveMethod(string methodName)
    {
        return typeof(ExecutionStrategyPlannerTests).GetMethod(methodName, BindingFlags.Public | BindingFlags.Static) ??
               throw new InvalidOperationException($"Method {methodName} was not found.");
    }

    private static PhysicalSingleKeyAggregateNode CreateSingleKeyAggregate(PhysicalNode input)
    {
        return new PhysicalSingleKeyAggregateNode(
            new ColumnRef("p", "Name", typeof(string)),
            "p.Name",
            typeof(string),
            [CreateCountBinding()],
            input);
    }

    private static PhysicalProjectNode CreateAggregateProject(PhysicalNode input)
    {
        return new PhysicalProjectNode(
            [
                new ProjectedField("Name", new ColumnRef("p", "Name", typeof(string)), 0),
                new ProjectedField("Count", new AggregateRef("p.Count", typeof(long)), 1)
            ],
            input);
    }

    private static AggregateBinding CreateCountBinding()
    {
        var getMethod = typeof(LibraryBase).GetMethod(
            nameof(LibraryBase.Count),
            [typeof(int?), typeof(int)]) ?? throw new InvalidOperationException("Count aggregate declaration was not found.");

        return new AggregateBinding(
            "p.Count",
            "p.Count",
            getMethod,
            [new Literal("p.Count", typeof(string)), new Literal(1, typeof(int))],
            getMethod,
            [],
            typeof(long),
            AggregateKernelDescriptor.Create(getMethod));
    }

    public static string StableMethod(string value)
    {
        return value;
    }

    [NonDeterministic]
    public static string NonDeterministicMethod(string value)
    {
        return value;
    }

    public static string StatsMethod([InjectQueryStats] object stats, string value)
    {
        return value;
    }

    public sealed class Person
    {
        public string Name { get; init; } = string.Empty;

        public int Age { get; init; }
    }
}
