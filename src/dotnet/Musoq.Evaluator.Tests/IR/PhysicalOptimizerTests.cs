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
using Musoq.Evaluator.IR.Optimization;
using Musoq.Evaluator.IR.Optimization.Physical;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;
using Musoq.Evaluator.IR.Planning;
using Musoq.Schema;
using Musoq.Schema.Optimization;
using PlanProperties = Musoq.Evaluator.IR.Planning.PlanProperties;

namespace Musoq.Evaluator.Tests.IR;

[TestClass]
public sealed class PhysicalOptimizerTests
{
    [TestMethod]
    public void Optimize_WhenDefaultCompatibilityPassesDoNotRewrite_ShouldKeepPlanAndTracePasses()
    {
        var plan = new PhysicalValuesScanNode(
            "v",
            [],
            new OutputSchema([new ColumnSchema("Value", typeof(int), 0)]));
        var properties = CreateEmptyProperties();

        var result = Optimize(plan, properties);

        Assert.AreSame(plan, result.InitialPlan);
        Assert.AreSame(plan, result.OptimizedPlan);
        Assert.AreEqual(properties, result.OptimizedProperties);
        Assert.HasCount(11, result.Trace.Entries);
        Assert.AreEqual("SourcePredicateMetadata", result.Trace.Entries[0].PassName);
        Assert.AreEqual("SourceProjectionMetadata", result.Trace.Entries[1].PassName);
        Assert.AreEqual("ProjectionPruning", result.Trace.Entries[2].PassName);
        Assert.AreEqual("AggregateStrategySelection", result.Trace.Entries[3].PassName);
        Assert.AreEqual("PredicateMovement", result.Trace.Entries[4].PassName);
        Assert.AreEqual("JoinStrategySelection", result.Trace.Entries[5].PassName);
        Assert.AreEqual("OrderingStrategySelection", result.Trace.Entries[6].PassName);
        Assert.AreEqual("WindowMaterialization", result.Trace.Entries[7].PassName);
        Assert.AreEqual("SourcePredicatePhysicalRewrite", result.Trace.Entries[8].PassName);
        Assert.AreEqual("SourcePlanPhysicalRewrite", result.Trace.Entries[9].PassName);
        Assert.AreEqual("RecursiveCteInvariantPlanning", result.Trace.Entries[10].PassName);
        Assert.IsTrue(result.Trace.Entries.All(static entry => !entry.IsChanged));
        AssertTraceEntriesAreMeaningful(result.Trace.Entries);
    }

    [TestMethod]
    public void Optimize_WhenAggregateCandidateHasSingleKey_ShouldSelectSingleKeyAggregate()
    {
        var input = new PhysicalValuesScanNode(
            "v",
            [],
            new OutputSchema([new ColumnSchema("Value", typeof(int), 0)]));
        var candidate = new PhysicalAggregateCandidateNode(
            [new ColumnRef("v", "Value", typeof(int))],
            ["Value"],
            [typeof(int)],
            [],
            input);

        var result = Optimize(candidate, CreateEmptyProperties());

        Assert.IsInstanceOfType<PhysicalSingleKeyAggregateNode>(result.OptimizedPlan);
        var aggregate = (PhysicalSingleKeyAggregateNode)result.OptimizedPlan;
        Assert.AreEqual("Value", aggregate.GroupKeyName);
        Assert.AreSame(input, aggregate.Input);
        Assert.HasCount(1, result.Decisions);
        Assert.AreEqual(PlanningDecisionCategory.AggregateStrategy, result.Decisions[0].Category);
        Assert.AreEqual("SingleKey", result.Decisions[0].Outcome);
        Assert.IsTrue(result.Trace.Entries[3].IsChanged);
    }

    [TestMethod]
    public void Optimize_WhenJoinCandidateIsPureEquiJoin_ShouldSelectHashJoin()
    {
        var left = CreateValuesScan("a", ("Id", typeof(int)));
        var right = CreateValuesScan("b", ("UserId", typeof(int)));
        var candidate = new PhysicalJoinCandidateNode(
            JoinKind.Inner,
            new BinaryOp(
                BinaryOpKind.Equal,
                new ColumnRef("a", "Id", typeof(int)),
                new ColumnRef("b", "UserId", typeof(int)),
                typeof(bool)),
            left,
            right);

        var result = Optimize(candidate, CreateEmptyProperties());

        Assert.IsInstanceOfType<PhysicalHashJoinNode>(result.OptimizedPlan);
        var join = (PhysicalHashJoinNode)result.OptimizedPlan;
        Assert.AreEqual("b", ((ColumnRef)join.BuildKeys[0]).Alias);
        Assert.AreEqual("a", ((ColumnRef)join.ProbeKeys[0]).Alias);
        Assert.HasCount(1, result.Decisions);
        Assert.AreEqual(PlanningDecisionCategory.JoinStrategy, result.Decisions[0].Category);
        Assert.AreEqual("HashJoin", result.Decisions[0].Outcome);
        Assert.IsTrue(result.Trace.Entries[5].IsChanged);
    }

    [TestMethod]
    public void Optimize_WhenHashJoinInputIsExpando_ShouldSelectNestedLoopBeforeLowering()
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

        var result = Optimize(
            candidate,
            CreateEmptyProperties(),
            shapeResolver: new ExecutionPlanningShapeResolverAdapter(shapeResolver));

        Assert.IsInstanceOfType<PhysicalNestedLoopJoinNode>(result.OptimizedPlan);
        Assert.AreEqual("NestedLoop", result.Decisions[0].Outcome);
        Assert.Contains("dynamic or expando", result.Decisions[0].Reason);
    }

    [TestMethod]
    public void Optimize_WhenSortMergeProbeInputIsExpando_ShouldSelectNestedLoopBeforeLowering()
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

        var result = Optimize(
            candidate,
            CreateEmptyProperties(),
            shapeResolver: new ExecutionPlanningShapeResolverAdapter(shapeResolver));

        Assert.IsInstanceOfType<PhysicalNestedLoopJoinNode>(result.OptimizedPlan);
        Assert.AreEqual("NestedLoop", result.Decisions[0].Outcome);
        Assert.Contains("range join lowering", result.Decisions[0].Reason);
    }

    [TestMethod]
    public void Optimize_WhenJoinCandidateIsAsOfJoin_ShouldSelectNestedLoop()
    {
        var left = CreateValuesScan("a", ("Symbol", typeof(string)), ("Timestamp", typeof(long)));
        var right = CreateValuesScan("b", ("Symbol", typeof(string)), ("Timestamp", typeof(long)));
        var candidate = new PhysicalJoinCandidateNode(
            JoinKind.AsofLeft,
            new BinaryOp(
                BinaryOpKind.And,
                new BinaryOp(
                    BinaryOpKind.Equal,
                    new ColumnRef("a", "Symbol", typeof(string)),
                    new ColumnRef("b", "Symbol", typeof(string)),
                    typeof(bool)),
                new BinaryOp(
                    BinaryOpKind.GreaterOrEqual,
                    new ColumnRef("a", "Timestamp", typeof(long)),
                    new ColumnRef("b", "Timestamp", typeof(long)),
                    typeof(bool)),
                typeof(bool)),
            left,
            right);

        var result = Optimize(candidate, CreateEmptyProperties());

        Assert.IsInstanceOfType<PhysicalNestedLoopJoinNode>(result.OptimizedPlan);
        Assert.AreEqual("NestedLoop", result.Decisions[0].Outcome);
    }

    [TestMethod]
    public void Optimize_WhenSortSkipTakeShapeExists_ShouldSelectTopOffset()
    {
        var input = CreateValuesScan("v", ("Value", typeof(int)));
        var sort = new PhysicalSortNode(
            [new OrderField(new ColumnRef("v", "Value", typeof(int)), false)],
            input);
        var skip = new PhysicalSkipNode(2, sort);
        var take = new PhysicalTakeNode(5, skip);

        var result = Optimize(take, CreateEmptyProperties());

        Assert.IsInstanceOfType<PhysicalTopOffsetNode>(result.OptimizedPlan);
        var topOffset = (PhysicalTopOffsetNode)result.OptimizedPlan;
        Assert.AreEqual(2, topOffset.Skip);
        Assert.AreEqual(5, topOffset.Take);
        Assert.AreSame(input, topOffset.Input);
        Assert.AreEqual(PlanningDecisionCategory.OrderingStrategy, result.Decisions[0].Category);
        Assert.AreEqual("TopOffset", result.Decisions[0].Outcome);
    }

    [TestMethod]
    public void Optimize_WhenWindowInputIsNotMaterialized_ShouldInsertMaterialize()
    {
        var input = CreateValuesScan("v", ("Value", typeof(int)));
        var window = new PhysicalWindowNode(
            [
                new WindowRegistration(
                    typeof(string).GetMethod("ToUpper", Type.EmptyTypes)!,
                    "ToUpper",
                    [],
                    [],
                    [],
                    0,
                    typeof(string))
            ],
            input);

        var result = Optimize(window, CreateEmptyProperties());

        Assert.IsInstanceOfType<PhysicalWindowNode>(result.OptimizedPlan);
        var optimizedWindow = (PhysicalWindowNode)result.OptimizedPlan;
        Assert.IsInstanceOfType<PhysicalMaterializeNode>(optimizedWindow.Input);
        Assert.AreEqual(PlanningDecisionCategory.WindowStrategy, result.Decisions[0].Category);
        Assert.AreEqual("MaterializeInput", result.Decisions[0].Outcome);
    }

    [TestMethod]
    public void Optimize_WhenSourceMetadataExists_ShouldApplyPredicateAndProjectionToScan()
    {
        var predicate = new BinaryOp(
            BinaryOpKind.GreaterThan,
            new ColumnRef("v", "Value", typeof(int)),
            new Literal(1, typeof(int)),
            typeof(bool));
        var scan = new PhysicalSchemaScanNode(
            "test",
            "items",
            [],
            "v",
            [],
            [],
            new OutputSchema([new ColumnSchema("Value", typeof(int), 0)]),
            "source-1");
        var properties = CreateEmptyProperties() with
        {
            PushedPredicatesBySourceId = new Dictionary<string, IrExpression[]>
            {
                ["source-1"] = [predicate]
            },
            ProjectedColumnsBySourceId = new Dictionary<string, string[]>
            {
                ["source-1"] = ["Value"]
            }
        };

        var result = Optimize(scan, properties);

        Assert.IsInstanceOfType<PhysicalSchemaScanNode>(result.OptimizedPlan);
        var optimizedScan = (PhysicalSchemaScanNode)result.OptimizedPlan;
        Assert.AreSame(predicate, optimizedScan.PushedPredicates[0]);
        Assert.AreEqual("Value", optimizedScan.ProjectedColumns[0]);
    }

    [TestMethod]
    public void Optimize_WhenSimpleProjectionChainHasUnusedInnerFields_ShouldPruneInnerProjection()
    {
        var scan = CreateValuesScan("v", ("Id", typeof(int)), ("Name", typeof(string)), ("Age", typeof(int)));
        var inner = new PhysicalProjectNode(
            [
                new ProjectedField("Id", new ColumnRef("v", "Id", typeof(int)), 0),
                new ProjectedField("Name", new ColumnRef("v", "Name", typeof(string)), 1),
                new ProjectedField("Age", new ColumnRef("v", "Age", typeof(int)), 2)
            ],
            scan);
        var outer = new PhysicalProjectNode(
            [
                new ProjectedField("Name", new ColumnRef("inner", "Name", typeof(string)), 0)
            ],
            inner);

        var result = Optimize(outer, CreateEmptyProperties());

        Assert.IsInstanceOfType<PhysicalProjectNode>(result.OptimizedPlan);
        var optimizedOuter = (PhysicalProjectNode)result.OptimizedPlan;
        var optimizedInner = (PhysicalProjectNode)optimizedOuter.Input;

        Assert.HasCount(1, optimizedInner.Fields);
        Assert.AreEqual("Name", optimizedInner.Fields[0].OutputName);
        Assert.AreEqual(0, optimizedInner.Fields[0].OutputIndex);
        Assert.AreSame(scan, optimizedInner.Input);
        Assert.IsTrue(result.Trace.Entries[2].IsChanged);
        Assert.Contains("Pruned 2 unused projected field(s)", result.Trace.Entries[2].Reason);
    }

    [TestMethod]
    public void Optimize_WhenInnerProjectionIsDistinct_ShouldNotPruneProjectionChain()
    {
        var scan = CreateValuesScan("v", ("Id", typeof(int)), ("Name", typeof(string)));
        var inner = new PhysicalProjectNode(
            [
                new ProjectedField("Id", new ColumnRef("v", "Id", typeof(int)), 0),
                new ProjectedField("Name", new ColumnRef("v", "Name", typeof(string)), 1)
            ],
            scan)
        {
            IsDistinct = true
        };
        var outer = new PhysicalProjectNode(
            [
                new ProjectedField("Name", new ColumnRef("inner", "Name", typeof(string)), 0)
            ],
            inner);

        var result = Optimize(outer, CreateEmptyProperties());

        Assert.AreSame(outer, result.OptimizedPlan);
        Assert.IsFalse(result.Trace.Entries[2].IsChanged);
    }

    [TestMethod]
    public void Optimize_WhenProjectionChainCrossesFilter_ShouldRetainFilterColumnsAndPrune()
    {
        var scan = CreateValuesScan("v", ("Id", typeof(int)), ("Name", typeof(string)), ("Age", typeof(int)));
        var inner = new PhysicalProjectNode(
            [
                new ProjectedField("Id", new ColumnRef("v", "Id", typeof(int)), 0),
                new ProjectedField("Name", new ColumnRef("v", "Name", typeof(string)), 1),
                new ProjectedField("Age", new ColumnRef("v", "Age", typeof(int)), 2)
            ],
            scan);
        var filter = new PhysicalFilterNode(
            new BinaryOp(
                BinaryOpKind.GreaterThan,
                new ColumnRef("inner", "Age", typeof(int)),
                new Literal(0, typeof(int)),
                typeof(bool)),
            inner);
        var outer = new PhysicalProjectNode(
            [
                new ProjectedField("Name", new ColumnRef("inner", "Name", typeof(string)), 0)
            ],
            filter);

        var result = Optimize(outer, CreateEmptyProperties());

        Assert.IsInstanceOfType<PhysicalProjectNode>(result.OptimizedPlan);
        var optimizedOuter = (PhysicalProjectNode)result.OptimizedPlan;
        Assert.IsInstanceOfType<PhysicalFilterNode>(optimizedOuter.Input);
        var optimizedFilter = (PhysicalFilterNode)optimizedOuter.Input;
        Assert.IsInstanceOfType<PhysicalProjectNode>(optimizedFilter.Input);
        var optimizedInner = (PhysicalProjectNode)optimizedFilter.Input;

        CollectionAssert.AreEqual(
            new[] { "Name", "Age" },
            optimizedInner.Fields.Select(static field => field.OutputName).ToArray());
        CollectionAssert.AreEqual(
            new[] { 0, 1 },
            optimizedInner.Fields.Select(static field => field.OutputIndex).ToArray());
        Assert.AreSame(scan, optimizedInner.Input);
        Assert.IsTrue(result.Trace.Entries[2].IsChanged);
        Assert.Contains("Pruned 1 unused projected field(s)", result.Trace.Entries[2].Reason);
    }

    [TestMethod]
    public void Optimize_WhenProjectionChainCrossesSort_ShouldRetainSortColumnsAndPrune()
    {
        var scan = CreateValuesScan("v", ("Id", typeof(int)), ("Name", typeof(string)), ("Age", typeof(int)));
        var inner = new PhysicalProjectNode(
            [
                new ProjectedField("Id", new ColumnRef("v", "Id", typeof(int)), 0),
                new ProjectedField("Name", new ColumnRef("v", "Name", typeof(string)), 1),
                new ProjectedField("Age", new ColumnRef("v", "Age", typeof(int)), 2)
            ],
            scan);
        var sort = new PhysicalSortNode(
            [new OrderField(new ColumnRef("inner", "Age", typeof(int)), false)],
            inner);
        var outer = new PhysicalProjectNode(
            [
                new ProjectedField("Name", new ColumnRef("inner", "Name", typeof(string)), 0)
            ],
            sort);

        var result = Optimize(outer, CreateEmptyProperties());

        Assert.IsInstanceOfType<PhysicalProjectNode>(result.OptimizedPlan);
        var optimizedOuter = (PhysicalProjectNode)result.OptimizedPlan;
        Assert.IsInstanceOfType<PhysicalSortNode>(optimizedOuter.Input);
        var optimizedSort = (PhysicalSortNode)optimizedOuter.Input;
        Assert.IsInstanceOfType<PhysicalProjectNode>(optimizedSort.Input);
        var optimizedInner = (PhysicalProjectNode)optimizedSort.Input;

        CollectionAssert.AreEqual(
            new[] { "Name", "Age" },
            optimizedInner.Fields.Select(static field => field.OutputName).ToArray());
        CollectionAssert.AreEqual(
            new[] { 0, 1 },
            optimizedInner.Fields.Select(static field => field.OutputIndex).ToArray());
        Assert.AreSame(scan, optimizedInner.Input);
        Assert.IsTrue(result.Trace.Entries[2].IsChanged);
        Assert.Contains("Pruned 1 unused projected field(s)", result.Trace.Entries[2].Reason);
    }

    [TestMethod]
    public void Optimize_WhenInnerJoinInputsHaveUnusedProjectedPayload_ShouldPrunePayloadAndRetainJoinKeys()
    {
        var leftScan = CreateValuesScan("a", ("Name", typeof(string)), ("City", typeof(string)), ("Payload", typeof(decimal)));
        var rightScan = CreateValuesScan("b", ("City", typeof(string)), ("Payload", typeof(decimal)));
        var leftProject = new PhysicalProjectNode(
            [
                new ProjectedField("Name", new ColumnRef("a", "Name", typeof(string)), 0),
                new ProjectedField("City", new ColumnRef("a", "City", typeof(string)), 1),
                new ProjectedField("Payload", new ColumnRef("a", "Payload", typeof(decimal)), 2)
            ],
            leftScan);
        var rightProject = new PhysicalProjectNode(
            [
                new ProjectedField("City", new ColumnRef("b", "City", typeof(string)), 0),
                new ProjectedField("Payload", new ColumnRef("b", "Payload", typeof(decimal)), 1)
            ],
            rightScan);
        var join = new PhysicalJoinCandidateNode(
            JoinKind.Inner,
            new BinaryOp(
                BinaryOpKind.Equal,
                new ColumnRef("a", "City", typeof(string)),
                new ColumnRef("b", "City", typeof(string)),
                typeof(bool)),
            leftProject,
            rightProject);
        var project = new PhysicalProjectNode(
            [new ProjectedField("Name", new ColumnRef("a", "Name", typeof(string)), 0)],
            join);

        var result = Optimize(project, CreateEmptyProperties());

        Assert.IsInstanceOfType<PhysicalProjectNode>(result.OptimizedPlan);
        var optimizedProject = (PhysicalProjectNode)result.OptimizedPlan;
        Assert.IsInstanceOfType<PhysicalHashJoinNode>(optimizedProject.Input);
        var optimizedJoin = (PhysicalHashJoinNode)optimizedProject.Input;
        var optimizedLeft = (PhysicalProjectNode)optimizedJoin.Left;
        var optimizedRight = (PhysicalProjectNode)optimizedJoin.Right;

        CollectionAssert.AreEqual(
            new[] { "Name", "City" },
            optimizedLeft.Fields.Select(static field => field.OutputName).ToArray());
        CollectionAssert.AreEqual(
            new[] { "City" },
            optimizedRight.Fields.Select(static field => field.OutputName).ToArray());
        Assert.IsTrue(result.Trace.Entries[2].IsChanged);
        Assert.Contains("2 join input(s)", result.Trace.Entries[2].Reason);
    }

    [TestMethod]
    public void Optimize_WhenSemiJoinHasUnusedRightPayload_ShouldPruneBuildSidePayload()
    {
        var leftScan = CreateValuesScan("a", ("Name", typeof(string)), ("City", typeof(string)));
        var rightScan = CreateValuesScan("b", ("City", typeof(string)), ("Payload", typeof(decimal)));
        var leftProject = new PhysicalProjectNode(
            [
                new ProjectedField("Name", new ColumnRef("a", "Name", typeof(string)), 0),
                new ProjectedField("City", new ColumnRef("a", "City", typeof(string)), 1)
            ],
            leftScan);
        var rightProject = new PhysicalProjectNode(
            [
                new ProjectedField("City", new ColumnRef("b", "City", typeof(string)), 0),
                new ProjectedField("Payload", new ColumnRef("b", "Payload", typeof(decimal)), 1)
            ],
            rightScan);
        var join = new PhysicalJoinCandidateNode(
            JoinKind.LeftSemi,
            new BinaryOp(
                BinaryOpKind.Equal,
                new ColumnRef("a", "City", typeof(string)),
                new ColumnRef("b", "City", typeof(string)),
                typeof(bool)),
            leftProject,
            rightProject);
        var project = new PhysicalProjectNode(
            [new ProjectedField("Name", new ColumnRef("a", "Name", typeof(string)), 0)],
            join);

        var result = Optimize(project, CreateEmptyProperties());

        Assert.IsInstanceOfType<PhysicalProjectNode>(result.OptimizedPlan);
        var optimizedProject = (PhysicalProjectNode)result.OptimizedPlan;
        Assert.IsInstanceOfType<PhysicalHashJoinNode>(optimizedProject.Input);
        var optimizedJoin = (PhysicalHashJoinNode)optimizedProject.Input;
        var optimizedLeft = (PhysicalProjectNode)optimizedJoin.Left;
        var optimizedRight = (PhysicalProjectNode)optimizedJoin.Right;

        CollectionAssert.AreEqual(
            new[] { "Name", "City" },
            optimizedLeft.Fields.Select(static field => field.OutputName).ToArray());
        CollectionAssert.AreEqual(
            new[] { "City" },
            optimizedRight.Fields.Select(static field => field.OutputName).ToArray());
        Assert.IsTrue(result.Trace.Entries[2].IsChanged);
        Assert.Contains("1 join input(s)", result.Trace.Entries[2].Reason);
    }

    [TestMethod]
    public void Optimize_WhenAggregateInputProjectHasUnusedPayload_ShouldPruneToGroupAndAggregateArguments()
    {
        var scan = CreateValuesScan("e", ("City", typeof(string)), ("Population", typeof(decimal)), ("Payload", typeof(string)));
        var input = new PhysicalProjectNode(
            [
                new ProjectedField("City", new ColumnRef("e", "City", typeof(string)), 0),
                new ProjectedField("Population", new ColumnRef("e", "Population", typeof(decimal)), 1),
                new ProjectedField("Payload", new ColumnRef("e", "Payload", typeof(string)), 2)
            ],
            scan);
        var aggregate = new PhysicalSingleKeyAggregateNode(
            new ColumnRef("e", "City", typeof(string)),
            "City",
            typeof(string),
            [
                new AggregateBinding(
                    "sum_population",
                    "Population",
                    typeof(PhysicalOptimizerTests).GetMethod(nameof(SetAggregate), BindingFlags.NonPublic | BindingFlags.Static)!,
                    [new ColumnRef("e", "Population", typeof(decimal))],
                    typeof(PhysicalOptimizerTests).GetMethod(nameof(GetAggregate), BindingFlags.NonPublic | BindingFlags.Static)!,
                    [],
                    typeof(decimal))
            ],
            input);

        var result = Optimize(aggregate, CreateEmptyProperties());

        Assert.IsInstanceOfType<PhysicalSingleKeyAggregateNode>(result.OptimizedPlan);
        var optimizedAggregate = (PhysicalSingleKeyAggregateNode)result.OptimizedPlan;
        var optimizedInput = (PhysicalProjectNode)optimizedAggregate.Input;

        CollectionAssert.AreEqual(
            new[] { "City", "Population" },
            optimizedInput.Fields.Select(static field => field.OutputName).ToArray());
        Assert.IsTrue(result.Trace.Entries[2].IsChanged);
        Assert.Contains("1 aggregate input(s)", result.Trace.Entries[2].Reason);
    }

    [TestMethod]
    public void Optimize_WhenWindowInputProjectHasUnusedPayload_ShouldPruneToDownstreamAndWindowColumns()
    {
        var scan = CreateValuesScan(
            "e",
            ("Name", typeof(string)),
            ("Country", typeof(string)),
            ("Population", typeof(decimal)),
            ("Payload", typeof(string)));
        var input = new PhysicalProjectNode(
            [
                new ProjectedField("Name", new ColumnRef("e", "Name", typeof(string)), 0),
                new ProjectedField("Country", new ColumnRef("e", "Country", typeof(string)), 1),
                new ProjectedField("Population", new ColumnRef("e", "Population", typeof(decimal)), 2),
                new ProjectedField("Payload", new ColumnRef("e", "Payload", typeof(string)), 3)
            ],
            scan);
        var window = new PhysicalWindowNode(
            [
                new WindowRegistration(
                    typeof(string).GetMethod(nameof(string.ToUpper), Type.EmptyTypes)!,
                    "RowNumber",
                    [new ColumnRef("e", "Country", typeof(string))],
                    [new OrderField(new ColumnRef("e", "Population", typeof(decimal)), true)],
                    [],
                    0,
                    typeof(long))
            ],
            input);
        var project = new PhysicalProjectNode(
            [
                new ProjectedField("Name", new ColumnRef("e", "Name", typeof(string)), 0),
                new ProjectedField("RowNum", new WindowFunctionRef(0, typeof(long)), 1)
            ],
            window);

        var result = Optimize(project, CreateEmptyProperties());

        Assert.IsInstanceOfType<PhysicalProjectNode>(result.OptimizedPlan);
        var optimizedProject = (PhysicalProjectNode)result.OptimizedPlan;
        Assert.IsInstanceOfType<PhysicalWindowNode>(optimizedProject.Input);
        var optimizedWindow = (PhysicalWindowNode)optimizedProject.Input;
        Assert.IsInstanceOfType<PhysicalMaterializeNode>(optimizedWindow.Input);
        var materializedWindowInput = (PhysicalMaterializeNode)optimizedWindow.Input;
        var optimizedInput = (PhysicalProjectNode)materializedWindowInput.Input;

        CollectionAssert.AreEqual(
            new[] { "Name", "Country", "Population" },
            optimizedInput.Fields.Select(static field => field.OutputName).ToArray());
        Assert.IsTrue(result.Trace.Entries[2].IsChanged);
        Assert.Contains("1 window input(s)", result.Trace.Entries[2].Reason);
    }

    [TestMethod]
    public void Optimize_WhenSetOperationInputsHaveUnusedPayload_ShouldPruneSymmetricArmPayload()
    {
        var leftScan = CreateValuesScan("l", ("City", typeof(string)), ("Payload", typeof(string)));
        var rightScan = CreateValuesScan("r", ("City", typeof(string)), ("Payload", typeof(string)));
        var leftProject = new PhysicalProjectNode(
            [
                new ProjectedField("City", new ColumnRef("l", "City", typeof(string)), 0),
                new ProjectedField("Payload", new ColumnRef("l", "Payload", typeof(string)), 1)
            ],
            leftScan);
        var rightProject = new PhysicalProjectNode(
            [
                new ProjectedField("City", new ColumnRef("r", "City", typeof(string)), 0),
                new ProjectedField("Payload", new ColumnRef("r", "Payload", typeof(string)), 1)
            ],
            rightScan);
        var setOperation = new PhysicalSetOperationNode(
            SetOpKind.Union,
            leftProject,
            rightProject,
            [0],
            [typeof(string)]);
        var project = new PhysicalProjectNode(
            [new ProjectedField("City", new ColumnRef("set", "City", typeof(string)), 0)],
            setOperation);

        var result = Optimize(project, CreateEmptyProperties());

        Assert.IsInstanceOfType<PhysicalProjectNode>(result.OptimizedPlan);
        var optimizedProject = (PhysicalProjectNode)result.OptimizedPlan;
        var optimizedSetOperation = (PhysicalSetOperationNode)optimizedProject.Input;
        var optimizedLeft = (PhysicalProjectNode)optimizedSetOperation.Left;
        var optimizedRight = (PhysicalProjectNode)optimizedSetOperation.Right;

        CollectionAssert.AreEqual(
            new[] { "City" },
            optimizedLeft.Fields.Select(static field => field.OutputName).ToArray());
        CollectionAssert.AreEqual(
            new[] { "City" },
            optimizedRight.Fields.Select(static field => field.OutputName).ToArray());
        CollectionAssert.AreEqual(new[] { 0 }, optimizedSetOperation.FieldIndexes);
        CollectionAssert.AreEqual(new[] { typeof(string) }, optimizedSetOperation.FieldTypes);
        Assert.IsTrue(result.Trace.Entries[2].IsChanged);
        Assert.Contains("1 set-operation input(s)", result.Trace.Entries[2].Reason);
    }

    [TestMethod]
    public void Optimize_WhenSetOperationNeedsAllComparerColumns_ShouldNotPruneArmPayload()
    {
        var leftScan = CreateValuesScan("l", ("City", typeof(string)), ("Payload", typeof(string)));
        var rightScan = CreateValuesScan("r", ("City", typeof(string)), ("Payload", typeof(string)));
        var leftProject = new PhysicalProjectNode(
            [
                new ProjectedField("City", new ColumnRef("l", "City", typeof(string)), 0),
                new ProjectedField("Payload", new ColumnRef("l", "Payload", typeof(string)), 1)
            ],
            leftScan);
        var rightProject = new PhysicalProjectNode(
            [
                new ProjectedField("City", new ColumnRef("r", "City", typeof(string)), 0),
                new ProjectedField("Payload", new ColumnRef("r", "Payload", typeof(string)), 1)
            ],
            rightScan);
        var setOperation = new PhysicalSetOperationNode(
            SetOpKind.Union,
            leftProject,
            rightProject,
            [0, 1],
            [typeof(string), typeof(string)]);
        var project = new PhysicalProjectNode(
            [new ProjectedField("City", new ColumnRef("set", "City", typeof(string)), 0)],
            setOperation);

        var result = Optimize(project, CreateEmptyProperties());

        Assert.AreSame(project, result.OptimizedPlan);
        Assert.IsFalse(result.Trace.Entries[2].IsChanged);
    }

    [TestMethod]
    public void Optimize_WhenCteConsumersRequireSubset_ShouldPruneDefinitionProjectionAndReferenceSchema()
    {
        var scan = CreateValuesScan("e", ("Name", typeof(string)), ("City", typeof(string)), ("Payload", typeof(string)));
        var definitionPlan = new PhysicalProjectNode(
            [
                new ProjectedField("Name", new ColumnRef("e", "Name", typeof(string)), 0),
                new ProjectedField("City", new ColumnRef("e", "City", typeof(string)), 1),
                new ProjectedField("Payload", new ColumnRef("e", "Payload", typeof(string)), 2)
            ],
            scan);
        var cteRef = new PhysicalCteRefNode("people", "p", definitionPlan.OutputSchema);
        var query = new PhysicalProjectNode(
            [new ProjectedField("Name", new ColumnRef("p", "Name", typeof(string)), 0)],
            cteRef);
        var cte = new PhysicalCteNode(
            [new PhysicalCteDefinition("people", definitionPlan)],
            query);

        var result = Optimize(cte, CreateEmptyProperties());

        Assert.IsInstanceOfType<PhysicalCteNode>(result.OptimizedPlan);
        var optimizedCte = (PhysicalCteNode)result.OptimizedPlan;
        var optimizedDefinition = (PhysicalProjectNode)optimizedCte.Definitions[0].Plan;
        var optimizedQuery = (PhysicalProjectNode)optimizedCte.Query;
        var optimizedRef = (PhysicalCteRefNode)optimizedQuery.Input;

        CollectionAssert.AreEqual(
            new[] { "Name" },
            optimizedDefinition.Fields.Select(static field => field.OutputName).ToArray());
        Assert.HasCount(1, optimizedRef.OutputSchema.Columns);
        Assert.AreEqual("Name", optimizedRef.OutputSchema.Columns[0].Name);
        Assert.IsTrue(result.Trace.Entries[2].IsChanged);
        Assert.Contains("1 CTE definition(s)", result.Trace.Entries[2].Reason);
    }

    [TestMethod]
    public void Optimize_WhenJoinCandidateCarriesMovedPredicate_ShouldApplyFilterBeforeJoinSelection()
    {
        var left = CreateValuesScan("a", ("Id", typeof(int)));
        var right = CreateValuesScan("b", ("UserId", typeof(int)));
        var movedPredicate = new BinaryOp(
            BinaryOpKind.GreaterThan,
            new ColumnRef("a", "Id", typeof(int)),
            new Literal(0, typeof(int)),
            typeof(bool));
        var candidate = new PhysicalJoinCandidateNode(
            JoinKind.Inner,
            new BinaryOp(
                BinaryOpKind.Equal,
                new ColumnRef("a", "Id", typeof(int)),
                new ColumnRef("b", "UserId", typeof(int)),
                typeof(bool)),
            left,
            right,
            [movedPredicate],
            []);

        var result = Optimize(candidate, CreateEmptyProperties());

        Assert.IsInstanceOfType<PhysicalHashJoinNode>(result.OptimizedPlan);
        var join = (PhysicalHashJoinNode)result.OptimizedPlan;
        Assert.IsInstanceOfType<PhysicalFilterNode>(join.Left);
        var filter = (PhysicalFilterNode)join.Left;
        Assert.AreSame(movedPredicate, filter.Predicate);
    }

    [TestMethod]
    public void Optimize_WhenMovedPredicateTargetsTransparentJoinSide_ShouldPushFilterBelowSortAndProject()
    {
        var leftScan = CreateValuesScan(
            "a",
            ("Name", typeof(string)),
            ("City", typeof(string)),
            ("Population", typeof(decimal)));
        var leftProject = new PhysicalProjectNode(
            [
                new ProjectedField("Name", new ColumnRef("a", "Name", typeof(string)), 0),
                new ProjectedField("City", new ColumnRef("a", "City", typeof(string)), 1),
                new ProjectedField("Population", new ColumnRef("a", "Population", typeof(decimal)), 2)
            ],
            leftScan);
        var leftSort = new PhysicalSortNode(
            [new OrderField(new ColumnRef("a", "Name", typeof(string)), false)],
            leftProject);
        var right = CreateValuesScan("b", ("City", typeof(string)));
        var movedPredicate = new BinaryOp(
            BinaryOpKind.GreaterThan,
            new ColumnRef("a", "Population", typeof(decimal)),
            new Literal(100m, typeof(decimal)),
            typeof(bool));
        var candidate = new PhysicalJoinCandidateNode(
            JoinKind.Inner,
            new BinaryOp(
                BinaryOpKind.Equal,
                new ColumnRef("a", "City", typeof(string)),
                new ColumnRef("b", "City", typeof(string)),
                typeof(bool)),
            leftSort,
            right,
            [movedPredicate],
            []);
        var project = new PhysicalProjectNode(
            [new ProjectedField("Name", new ColumnRef("a", "Name", typeof(string)), 0)],
            candidate);

        var result = Optimize(project, CreateEmptyProperties());

        Assert.IsInstanceOfType<PhysicalProjectNode>(result.OptimizedPlan);
        var optimizedProject = (PhysicalProjectNode)result.OptimizedPlan;
        var join = (PhysicalHashJoinNode)optimizedProject.Input;
        var sort = (PhysicalSortNode)join.Left;
        var retainedProject = (PhysicalProjectNode)sort.Input;
        var filter = (PhysicalFilterNode)retainedProject.Input;

        Assert.AreSame(movedPredicate, filter.Predicate);
        Assert.AreSame(leftScan, filter.Input);
        Assert.IsTrue(result.Trace.Entries[4].IsChanged);
    }

    [TestMethod]
    public void Optimize_WhenInnerJoinOnHasSourceLocalConjunctsWithoutPlannerMovements_ShouldPrefilterInputs()
    {
        var left = CreateValuesScan("a", ("Id", typeof(int)), ("Age", typeof(int)));
        var right = CreateValuesScan("b", ("UserId", typeof(int)), ("IsActive", typeof(bool)));
        var leftPredicate = new BinaryOp(
            BinaryOpKind.GreaterThan,
            new ColumnRef("a", "Age", typeof(int)),
            new Literal(18, typeof(int)),
            typeof(bool));
        var rightPredicate = new BinaryOp(
            BinaryOpKind.Equal,
            new ColumnRef("b", "IsActive", typeof(bool)),
            new Literal(true, typeof(bool)),
            typeof(bool));
        var equality = new BinaryOp(
            BinaryOpKind.Equal,
            new ColumnRef("a", "Id", typeof(int)),
            new ColumnRef("b", "UserId", typeof(int)),
            typeof(bool));
        var candidate = new PhysicalJoinCandidateNode(
            JoinKind.Inner,
            new BinaryOp(
                BinaryOpKind.And,
                equality,
                new BinaryOp(
                    BinaryOpKind.And,
                    leftPredicate,
                    rightPredicate,
                    typeof(bool)),
                typeof(bool)),
            left,
            right);

        var result = Optimize(candidate, CreateEmptyProperties());

        Assert.IsInstanceOfType<PhysicalHashJoinNode>(result.OptimizedPlan);
        var join = (PhysicalHashJoinNode)result.OptimizedPlan;
        var leftFilter = (PhysicalFilterNode)join.Left;
        var rightFilter = (PhysicalFilterNode)join.Right;

        Assert.AreSame(leftPredicate, leftFilter.Predicate);
        Assert.AreSame(rightPredicate, rightFilter.Predicate);
        Assert.AreSame(left, leftFilter.Input);
        Assert.AreSame(right, rightFilter.Input);
        Assert.IsTrue(result.Trace.Entries[4].IsChanged);
    }

    private static void SetAggregate(decimal value)
    {
    }

    private static decimal GetAggregate()
    {
        return 0m;
    }

    private static PhysicalOptimizationResult Optimize(
        PhysicalNode plan,
        PlanProperties properties,
        CompilationOptions? compilationOptions = null,
        IPlanningShapeResolver? shapeResolver = null)
    {
        return new PhysicalOptimizer().Optimize(
            plan,
            properties,
            compilationOptions ?? new CompilationOptions(),
            shapeResolver ?? ConservativeTestPlanningShapeResolver.Instance);
    }

    private static PhysicalValuesScanNode CreateValuesScan(
        string alias,
        params (string Name, Type Type)[] columns)
    {
        var schemaColumns = new ColumnSchema[columns.Length];

        for (var i = 0; i < columns.Length; i++)
            schemaColumns[i] = new ColumnSchema(columns[i].Name, columns[i].Type, i);

        return new PhysicalValuesScanNode(alias, [], new OutputSchema(schemaColumns));
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

    private static PlanProperties CreateEmptyProperties()
    {
        return PlanPropertiesTestFactory.CreateEmpty();
    }

    private static void AssertTraceEntriesAreMeaningful(
        IReadOnlyList<OptimizationTraceEntry> entries)
    {
        Assert.IsTrue(entries.All(static entry => entry.Stage == OptimizationStage.PhysicalOptimization));
        Assert.IsTrue(entries.All(static entry => !string.IsNullOrWhiteSpace(entry.PassName)));
        Assert.IsTrue(entries.All(static entry => !string.IsNullOrWhiteSpace(entry.Outcome)));
        Assert.IsTrue(entries.All(static entry => !string.IsNullOrWhiteSpace(entry.Reason)));
        Assert.IsTrue(entries.All(static entry =>
            string.Equals(entry.Outcome, entry.IsChanged ? "Changed" : "NoChange", StringComparison.Ordinal)));
    }
}
