using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Logical.Nodes;
using Musoq.Evaluator.IR.Optimization.Physical;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;
using Musoq.Evaluator.IR.Physical.SourcePlanning;
using Musoq.Evaluator.IR.Planning;
using Musoq.Parser.Nodes;
using PlanProperties = Musoq.Evaluator.IR.Planning.PlanProperties;

namespace Musoq.Evaluator.Tests.IR;

[TestClass]
public sealed class PhysicalPlanningArchitectureCharacterizationTests
{
    private const string SourceContextId = "source-1";
    private const string SourceAlias = "s";

    [TestMethod]
    public void PhysicalOptimizationSession_WhenShapeResolverIsMissing_ShouldThrow()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => new PhysicalOptimizationSession(
            CreateEmptyProperties(),
            new CompilationOptions(),
            shapeResolver: null!));
    }

    [TestMethod]
    public void PhysicalOptimizerApi_ShouldNotExposeTwoArgumentOptimizeWithoutShapeFacts()
    {
        var twoArgumentPropertyOverloads = typeof(PhysicalOptimizer)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(static method => method.Name == nameof(PhysicalOptimizer.Optimize))
            .Where(static method =>
            {
                var parameters = method.GetParameters();
                return parameters.Length == 2 &&
                       typeof(PhysicalNode).IsAssignableFrom(parameters[0].ParameterType) &&
                       parameters[1].ParameterType == typeof(PlanProperties);
            })
            .ToArray();

        Assert.IsEmpty(twoArgumentPropertyOverloads);
    }

    [TestMethod]
    public void PhysicalOptimizer_WhenShapeResolverIdentifiesExpandoHashJoin_ShouldFallbackToNestedLoop()
    {
        var left = CreateSchemaScan("a", ("Id", typeof(int)));
        var right = CreateSchemaScan("b", ("UserId", typeof(int)));
        var candidate = new PhysicalJoinCandidateNode(
            JoinKind.Inner,
            new BinaryOp(
                BinaryOpKind.Equal,
                new ColumnRef("a", "Id", typeof(int)),
                new ColumnRef("b", "UserId", typeof(int)),
                typeof(bool)),
            left,
            right);
        var shapeResolver = new ExecutionShapeResolver(
            entityTypesByAlias: new Dictionary<string, Type>
            {
                ["a"] = typeof(object),
                ["b"] = typeof(ExpandoObject)
            });

        var result = new PhysicalOptimizer().Optimize(
            candidate,
            CreateEmptyProperties(),
            new CompilationOptions(),
            new ExecutionPlanningShapeResolverAdapter(shapeResolver));

        Assert.IsInstanceOfType<PhysicalNestedLoopJoinNode>(result.OptimizedPlan);
        Assert.AreEqual("NestedLoop", result.Decisions[0].Outcome);
        StringAssert.Contains(result.Decisions[0].Reason, "dynamic or expando");
    }

    [TestMethod]
    public void PhysicalOptimizationFacts_WhenApplied_ShouldUpdateOnlyPhysicalFactFields()
    {
        var properties = CreateEmptyProperties();
        var sourcePlanRequest = SourcePlanRequest.Empty(new SourceIdentity("#sp", "items", SourceContextId, SourceAlias));
        var sourcePlanResults = new Dictionary<string, SourcePlanResult>(StringComparer.Ordinal)
        {
            [SourceContextId] = SourcePlanResult.RejectAll(sourcePlanRequest)
        };
        var sourceRewriteFacts = SourceRewriteFacts.From(properties)
            .WithSourcePlanResults(sourcePlanResults);
        var facts = PhysicalOptimizationFacts.From(properties) with
        {
            SourceRewrite = sourceRewriteFacts
        };

        var applied = facts.ApplyTo(properties);

        Assert.AreSame(sourcePlanResults, applied.SourcePlanResultsBySourceId);
        Assert.AreSame(properties.SourcePlanRequestsBySourceId, applied.SourcePlanRequestsBySourceId);
        Assert.AreSame(properties.SourceInteractionPlansBySourceId, applied.SourceInteractionPlansBySourceId);
        Assert.AreSame(properties.RequiredColumnBoundaryPlans, applied.RequiredColumnBoundaryPlans);
        Assert.AreSame(properties.RowWidthPruningPlans, applied.RowWidthPruningPlans);
    }

    [TestMethod]
    public void PlanPropertiesComponents_ShouldExposeExistingFactFamilies()
    {
        var properties = CreateEmptyProperties();

        Assert.AreSame(properties.SourcesById, properties.SourcePlanning.SourcesById);
        Assert.AreSame(properties.SourcePlanResultsBySourceId, properties.SourcePlanning.SourcePlanResultsBySourceId);
        Assert.AreSame(properties.SourceContractDiagnosticLocationsBySourceId, properties.SourcePlanning.SourceContractDiagnosticLocationsBySourceId);
        Assert.AreSame(properties.RequiredColumnBoundaryPlans, properties.RequiredColumns.RequiredColumnBoundaryPlans);
        Assert.AreSame(properties.PredicateMovementPlans, properties.PhysicalStrategies.PredicateMovementPlans);
        Assert.AreSame(properties.ApplyPredicateMovementPlans, properties.PhysicalStrategies.ApplyPredicateMovementPlans);
        Assert.AreSame(properties.BoundaryRowShapePlans, properties.BoundaryPruning.BoundaryRowShapePlans);
        Assert.AreSame(properties.CardinalityFacts, properties.Cardinality.Facts);
    }

    [TestMethod]
    public void PlanningFactsRoundTrip_ShouldPreserveExistingFactFamilyReferences()
    {
        var properties = CreateEmptyProperties();

        var facts = properties.ToFacts();
        var roundTrip = facts.ToPlanProperties();

        Assert.AreSame(properties.SourcesById, facts.SourcePlanning.SourcesById);
        Assert.AreSame(properties.SourceContractDiagnosticLocationsBySourceId, facts.SourcePlanning.SourceContractDiagnosticLocationsBySourceId);
        Assert.AreSame(properties.RequiredColumnUsagesBySourceId, facts.RequiredColumns.RequiredColumnUsagesBySourceId);
        Assert.AreSame(properties.PredicatePlacementPlans, facts.PhysicalStrategies.PredicatePlacementPlans);
        Assert.AreSame(properties.RowWidthPruningPlans, facts.BoundaryPruning.RowWidthPruningPlans);
        Assert.AreSame(properties.CardinalityFacts, facts.Cardinality.Facts);
        Assert.AreSame(facts.SourcePlanning.SourcesById, roundTrip.SourcesById);
        Assert.AreSame(facts.SourcePlanning.SourceContractDiagnosticLocationsBySourceId, roundTrip.SourceContractDiagnosticLocationsBySourceId);
        Assert.AreSame(facts.RequiredColumns.RequiredColumnBoundaryPlans, roundTrip.RequiredColumnBoundaryPlans);
        Assert.AreSame(facts.PhysicalStrategies.PredicateMovementPlans, roundTrip.PredicateMovementPlans);
        Assert.AreSame(facts.PhysicalStrategies.ApplyPredicateMovementPlans, roundTrip.ApplyPredicateMovementPlans);
        Assert.AreSame(facts.BoundaryPruning.BoundaryRowShapePlans, roundTrip.BoundaryRowShapePlans);
        Assert.AreSame(facts.Cardinality.Facts, roundTrip.CardinalityFacts);
    }

    [TestMethod]
    public void PlanningHandoffRecords_ShouldCarryPlanningFactsAsPrimaryData()
    {
        Assert.AreEqual(typeof(PlanningFacts), typeof(PlanningPropertyResult).GetProperty(nameof(PlanningPropertyResult.Facts))!.PropertyType);
        Assert.AreEqual(typeof(PlanProperties), typeof(PlanningPropertyResult).GetProperty(nameof(PlanningPropertyResult.Properties))!.PropertyType);
        Assert.AreEqual(typeof(PlanningFacts), typeof(PhysicalPlanningArtifacts).GetProperty(nameof(PhysicalPlanningArtifacts.OptimizedFacts))!.PropertyType);
        Assert.AreEqual(typeof(PlanProperties), typeof(PhysicalPlanningArtifacts).GetProperty(nameof(PhysicalPlanningArtifacts.OptimizedProperties))!.PropertyType);
        Assert.AreEqual(typeof(PlanningFacts), typeof(PlanningResult).GetProperty(nameof(PlanningResult.Facts))!.PropertyType);
        Assert.AreEqual(typeof(PlanProperties), typeof(PlanningResult).GetProperty(nameof(PlanningResult.Properties))!.PropertyType);

        Assert.IsFalse(HasConstructorParameter<PlanningPropertyResult, PlanProperties>());
        Assert.IsFalse(HasConstructorParameter<PhysicalPlanningArtifacts, PlanProperties>());
        Assert.IsFalse(HasConstructorParameter<PlanningResult, PlanProperties>());
    }

    [TestMethod]
    public void PhysicalOptimizer_WhenShapeResolverIdentifiesExpandoRangeJoin_ShouldFallbackToNestedLoop()
    {
        var left = CreateSchemaScan("a", ("Age", typeof(int)));
        var right = CreateSchemaScan("b", ("Age", typeof(int)));
        var candidate = new PhysicalJoinCandidateNode(
            JoinKind.Inner,
            new BinaryOp(
                BinaryOpKind.GreaterOrEqual,
                new ColumnRef("a", "Age", typeof(int)),
                new ColumnRef("b", "Age", typeof(int)),
                typeof(bool)),
            left,
            right);
        var shapeResolver = new ExecutionShapeResolver(
            entityTypesByAlias: new Dictionary<string, Type>
            {
                ["a"] = typeof(object),
                ["b"] = typeof(ExpandoObject)
            });

        var result = new PhysicalOptimizer().Optimize(
            candidate,
            CreateEmptyProperties(),
            new CompilationOptions(),
            shapeResolver: new ExecutionPlanningShapeResolverAdapter(shapeResolver));

        Assert.IsInstanceOfType<PhysicalNestedLoopJoinNode>(result.OptimizedPlan);
        Assert.AreEqual("NestedLoop", result.Decisions[0].Outcome);
        StringAssert.Contains(result.Decisions[0].Reason, "range join lowering");
    }

    [TestMethod]
    public void SourcePlanPhysicalRewriter_WhenSourceAcceptsSortSkipTake_ShouldRemoveOperationsAndAlignResultFacts()
    {
        var orderField = new OrderField(
            new ColumnRef(SourceAlias, "Category", typeof(string)),
            Descending: true);
        var scan = CreateSchemaScan(SourceAlias, ("Category", typeof(string))) with
        {
            SourceContextId = SourceContextId
        };
        var physicalPlan = new PhysicalTakeNode(
            5,
            new PhysicalSkipNode(
                2,
                new PhysicalSortNode([orderField], scan)));
        var orderBy = new[]
        {
            new OrderByExpression(new SourceColumnRef("Category"), OrderDirection.Descending)
        };

        var rewritten = SourcePlanPhysicalRewriter.Rewrite(
            physicalPlan,
            CreateSourcePlanResults(orderBy, acceptedSkip: 2, acceptedTake: 5));

        Assert.IsInstanceOfType<PhysicalSchemaScanNode>(rewritten.PhysicalPlan);
        var sourcePlan = rewritten.SourcePlanResultsBySourceId[SourceContextId];
        Assert.HasCount(1, sourcePlan.AcceptedOrderBy);
        Assert.AreEqual(OrderDirection.Descending, sourcePlan.AcceptedOrderBy[0].Direction);
        Assert.HasCount(0, sourcePlan.ResidualOrderBy);
        Assert.AreEqual(2, sourcePlan.AcceptedSkip);
        Assert.IsNull(sourcePlan.ResidualSkip);
        Assert.AreEqual(5, sourcePlan.AcceptedTake);
        Assert.IsNull(sourcePlan.ResidualTake);
        Assert.HasCount(1, sourcePlan.ExecutionPlan.AcceptedOrderBy);
        Assert.AreEqual(2, sourcePlan.ExecutionPlan.AcceptedSkip);
        Assert.AreEqual(5, sourcePlan.ExecutionPlan.AcceptedTake);
    }

    [TestMethod]
    public void PhysicalOptimizer_WhenSourcePredicateIsAccepted_ShouldRemoveFilterAndPreserveRewriteFacts()
    {
        var predicate = CreatePredicate(SourceAlias);
        var scan = CreateSchemaScan(SourceAlias, ("Category", typeof(string))) with
        {
            SourceContextId = SourceContextId
        };
        var filter = new PhysicalFilterNode(predicate, scan);
        var acceptedPredicate = CreateSourcePredicate();
        var properties = CreateEmptyProperties() with
        {
            SourcePlanResultsBySourceId = CreateSourcePlanResults(acceptedPredicate),
            SourcePredicatePlansBySourceId = CreateSourcePredicatePlans(predicate)
        };

        var result = new PhysicalOptimizer().Optimize(
            filter,
            properties,
            new CompilationOptions(),
            ConservativeTestPlanningShapeResolver.Instance);

        Assert.IsInstanceOfType<PhysicalSchemaScanNode>(result.OptimizedPlan);
        var sourcePlan = result.OptimizedProperties.SourcePlanResultsBySourceId[SourceContextId];
        Assert.AreEqual(acceptedPredicate, sourcePlan.AcceptedPredicate);
        Assert.AreEqual(acceptedPredicate, sourcePlan.ExecutionPlan.AcceptedPredicate);
        Assert.AreSame(
            properties.SourcePredicatePlansBySourceId,
            result.OptimizedProperties.SourcePredicatePlansBySourceId);
    }

    [TestMethod]
    public void ExecutionStrategyPlan_WhenEquivalentProjectWasNotPlanned_ShouldNotReturnStrategy()
    {
        var project = CreateMethodProject(CreateSchemaScan("p", ("Name", typeof(string)), ("Age", typeof(int))));
        var result = ExecutionStrategyPlanner.Plan(
            project,
            new CompilationOptions(),
            null,
            CreatePlanningShapeResolver(typeof(Person)));
        var equivalentUnplannedProject = CreateMethodProject(CreateSchemaScan("p", ("Name", typeof(string)), ("Age", typeof(int))));

        Assert.IsTrue(result.Strategies.CanUseParallelFilterProject(project));
        Assert.IsTrue(result.Strategies.TryResolvePhysicalNodeId(project, out _));
        Assert.IsFalse(result.Strategies.CanUseParallelFilterProject(equivalentUnplannedProject));
        Assert.IsFalse(result.Strategies.TryResolvePhysicalNodeId(equivalentUnplannedProject, out _));
    }

    [TestMethod]
    public void ParallelPlanningEligibilityRules_WhenMethodCallIsNestedInCompositeExpression_ShouldFindIt()
    {
        var method = typeof(PhysicalPlanningArchitectureCharacterizationTests)
            .GetMethod(nameof(StableMethod), BindingFlags.Public | BindingFlags.Static)!;
        var expression = new StrictCast(
            new CaseWhen(
                [
                    new CaseWhenBranch(
                        new Literal(true, typeof(bool)),
                        new Coalesce(
                            [
                                new Literal(null, typeof(string)),
                                new MethodCall(method, [new Literal("alpha", typeof(string))], null, typeof(string))
                            ],
                            typeof(string)))
                ],
                new Literal("fallback", typeof(string)),
                typeof(string)),
            "string",
            typeof(string));

        var eligibility = ParallelPlanningEligibilityRules.CanUseFilterProjectExpression(
            expression,
            new PlanningRowShape("people", "p", PlanningRowShapeKind.SourceEntity, typeof(Person), []));

        Assert.IsTrue(ParallelPlanningEligibilityRules.ContainsMethodCall(expression));
        Assert.IsTrue(eligibility.IsEligible, eligibility.Reason);
    }

    [TestMethod]
    public void PhysicalPlanningOwnership_PlanningNamespaceOwnsPhysicalPlanningOrchestration()
    {
        var pipelineType = typeof(PhysicalPlanningPipeline);

        Assert.AreEqual("Musoq.Evaluator.IR.Planning", pipelineType.Namespace);
    }

    public static string StableMethod(string value)
    {
        return value;
    }

    private static PhysicalProjectNode CreateMethodProject(PhysicalNode input)
    {
        var filter = new PhysicalFilterNode(
            new BinaryOp(
                BinaryOpKind.GreaterThan,
                new ColumnRef("p", "Age", typeof(int)),
                new Literal(18, typeof(int)),
                typeof(bool)),
            input);
        var method = typeof(PhysicalPlanningArchitectureCharacterizationTests)
            .GetMethod(nameof(StableMethod), BindingFlags.Public | BindingFlags.Static)!;

        return new PhysicalProjectNode(
            [
                new ProjectedField(
                    "Name",
                    new MethodCall(method, [new ColumnRef("p", "Name", typeof(string))], null, typeof(string)),
                    0)
            ],
            filter);
    }

    private static IPlanningShapeResolver CreatePlanningShapeResolver(Type entityType)
    {
        return new ExecutionPlanningShapeResolverAdapter(
            new ExecutionShapeResolver(
                entityTypesByAlias: new Dictionary<string, Type>
                {
                    ["p"] = entityType
                }));
    }

    private static Dictionary<string, SourcePlanResult> CreateSourcePlanResults(
        IReadOnlyList<OrderByExpression> acceptedOrderBy,
        long acceptedSkip,
        long acceptedTake)
    {
        var identity = new SourceIdentity("#sp", "items", SourceContextId, SourceAlias);
        return new Dictionary<string, SourcePlanResult>(StringComparer.Ordinal)
        {
            [SourceContextId] = new()
            {
                ExecutionPlan = SourceExecutionPlan.Empty(identity),
                AcceptedOrderBy = acceptedOrderBy,
                AcceptedSkip = acceptedSkip,
                AcceptedTake = acceptedTake
            }
        };
    }

    private static Dictionary<string, SourcePlanResult> CreateSourcePlanResults(
        SourcePredicateExpression acceptedPredicate)
    {
        var identity = new SourceIdentity("#sp", "items", SourceContextId, SourceAlias);
        return new Dictionary<string, SourcePlanResult>(StringComparer.Ordinal)
        {
            [SourceContextId] = new()
            {
                ExecutionPlan = SourceExecutionPlan.Empty(identity) with
                {
                    AcceptedPredicate = acceptedPredicate
                },
                AcceptedPredicate = acceptedPredicate
            }
        };
    }

    private static Dictionary<string, SourcePredicatePlan> CreateSourcePredicatePlans(
        IrExpression pushedPredicate)
    {
        return new Dictionary<string, SourcePredicatePlan>(StringComparer.Ordinal)
        {
            [SourceContextId] = new(
                SourceContextId,
                SourceAlias,
                new WhereNode(new BooleanNode(true)),
                [pushedPredicate],
                "test",
                PlanningConfidence.High)
        };
    }

    private static BinaryOp CreatePredicate(string alias)
    {
        return new BinaryOp(
            BinaryOpKind.Equal,
            new ColumnRef(alias, "Category", typeof(string)),
            new Literal("alpha", typeof(string)),
            typeof(bool));
    }

    private static SourcePredicateComparison CreateSourcePredicate()
    {
        return new SourcePredicateComparison(
            SourcePredicateComparisonOperator.Equal,
            new SourcePredicateColumn(new SourceColumnRef("Category")),
            new SourcePredicateLiteral("alpha"));
    }

    private static PhysicalSchemaScanNode CreateSchemaScan(
        string alias,
        params (string Name, Type Type)[] columns)
    {
        var schemaColumns = new ColumnSchema[columns.Length];

        for (var i = 0; i < columns.Length; i++)
            schemaColumns[i] = new ColumnSchema(columns[i].Name, columns[i].Type, i);

        return new PhysicalSchemaScanNode(
            "test",
            "items",
            [],
            alias,
            [],
            [],
            new OutputSchema(schemaColumns));
    }

    private static bool HasConstructorParameter<TDeclaring, TParameter>()
    {
        return typeof(TDeclaring)
            .GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Any(static constructor => constructor
                .GetParameters()
                .Any(static parameter => parameter.ParameterType == typeof(TParameter)));
    }

    private static PlanProperties CreateEmptyProperties()
    {
        return PlanPropertiesTestFactory.CreateEmpty();
    }

    public sealed class Person
    {
        public string Name { get; init; } = string.Empty;

        public int Age { get; init; }
    }
}
