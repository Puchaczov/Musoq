using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Logical.Nodes;
using Musoq.Evaluator.IR.Optimization.Physical;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Planning;
using PhysicalNodes = Musoq.Evaluator.IR.Physical.Nodes;
using PhysicalPlanBuilder = Musoq.Evaluator.IR.Physical.PhysicalPlanBuilder;
using PhysicalPlanPrinter = Musoq.Evaluator.IR.Physical.PhysicalPlanPrinter;
using PlanProperties = Musoq.Evaluator.IR.Planning.PlanProperties;

namespace Musoq.Evaluator.Tests.IR;

[TestClass]
public class PhysicalPlanBuilderTests
{
    private static OutputSchema CreateSchema(params (string Name, Type Type)[] columns)
    {
        var schemaColumns = new ColumnSchema[columns.Length];

        for (var i = 0; i < columns.Length; i++)
            schemaColumns[i] = new ColumnSchema(columns[i].Name, columns[i].Type, i);

        return new OutputSchema(schemaColumns);
    }

    private static SchemaScanNode CreateScan(string alias = "t", params (string Name, Type Type)[] columns)
    {
        if (columns.Length == 0)
            columns = [("Id", typeof(int)), ("Name", typeof(string))];

        return new SchemaScanNode("test", "data", [], alias, CreateSchema(columns));
    }

    private static AggregateBinding CreateAggregateBinding(string name = "Count")
    {
        var setMethod = typeof(string).GetMethod("Concat", [typeof(string), typeof(string)])!;
        var getMethod = typeof(string).GetMethod("ToUpper", Type.EmptyTypes)!;
        return new AggregateBinding(name, name, setMethod, [], getMethod, [], typeof(int));
    }

    private static PhysicalNode Optimize(PhysicalNode physical, CompilationOptions? options = null)
    {
        return new PhysicalOptimizer()
            .Optimize(
                physical,
                CreateEmptyProperties(),
                options ?? new CompilationOptions(),
                ConservativeTestPlanningShapeResolver.Instance)
            .OptimizedPlan;
    }

    private static PhysicalPlanBuilder CreatePhysicalBuilder(CompilationOptions? options = null)
    {
        return new PhysicalPlanBuilder(predicateMovementPlans: null,
            strategyPlan: new PhysicalStrategyPlan());
    }

    private static PlanProperties CreateEmptyProperties()
    {
        return PlanPropertiesTestFactory.CreateEmpty();
    }

    [TestMethod]
    public void Lower_WhenStrategyPlanIsMissing_ShouldThrow()
    {
        var logical = CreateScan();

        var exception = Assert.Throws<InvalidOperationException>(() => new PhysicalPlanBuilder().Lower(logical));

        StringAssert.Contains(exception.Message, "explicit PhysicalStrategyPlan");
    }

    [TestMethod]
    public void Lower_WhenSchemaScan_ShouldCreatePhysicalSchemaScanWithoutPushdown()
    {
        var logical = CreateScan();

        var physical = CreatePhysicalBuilder().Lower(logical);

        Assert.IsInstanceOfType<PhysicalNodes.PhysicalSchemaScanNode>(physical);
        var scan = (PhysicalNodes.PhysicalSchemaScanNode)physical;
        Assert.IsEmpty(scan.PushedPredicates);
        Assert.IsEmpty(scan.ProjectedColumns);
        Assert.AreEqual("t", scan.Alias);
    }

    [TestMethod]
    public void Lower_WhenSchemaScanHasMatchingSourceContextId_ShouldCreateInitialScanWithoutPushedPredicates()
    {
        var logical = new SchemaScanNode(
            "test",
            "data",
            [],
            "t",
            CreateSchema(("Id", typeof(int))),
            "source-1");
        var physical = CreatePhysicalBuilder().Lower(logical);

        Assert.IsInstanceOfType<PhysicalNodes.PhysicalSchemaScanNode>(physical);
        var scan = (PhysicalNodes.PhysicalSchemaScanNode)physical;
        Assert.IsEmpty(scan.PushedPredicates);
    }

    [TestMethod]
    public void Lower_WhenInterpretSource_ShouldPreserveOperationMetadata()
    {
        var logical = new InterpretSourceNode(
            "packet",
            InterpretSourceKind.TryParse,
            [new Literal("payload", typeof(string))],
            "p",
            typeof(object),
            ApplyKind.Outer,
            CreateSchema(("Value", typeof(string))));

        var physical = CreatePhysicalBuilder().Lower(logical);

        Assert.IsInstanceOfType<PhysicalNodes.PhysicalInterpretSourceNode>(physical);
        var interpret = (PhysicalNodes.PhysicalInterpretSourceNode)physical;
        Assert.AreEqual(InterpretSourceKind.TryParse, interpret.Kind);
        Assert.AreEqual(ApplyKind.Outer, interpret.ApplyKind);
        Assert.AreEqual("payload", ((Literal)interpret.Arguments[0]).Value);
    }

    [TestMethod]
    public void Lower_WhenAggregateHasNoKeys_ShouldCreateAggregateCandidate()
    {
        var logical = new AggregateNode([], [], [], [CreateAggregateBinding()], CreateScan());

        var physical = CreatePhysicalBuilder().Lower(logical);

        Assert.IsInstanceOfType<PhysicalNodes.PhysicalAggregateCandidateNode>(physical);
        var aggregate = (PhysicalNodes.PhysicalAggregateCandidateNode)physical;
        Assert.IsEmpty(aggregate.GroupKeys);
    }

    [TestMethod]
    public void Lower_WhenAggregateHasSingleTypedKey_ShouldCreateAggregateCandidate()
    {
        var logical = new AggregateNode(
            [new ColumnRef("t", "Name", typeof(string))],
            ["Name"],
            [typeof(string)],
            [CreateAggregateBinding()],
            CreateScan());

        var physical = CreatePhysicalBuilder().Lower(logical);

        Assert.IsInstanceOfType<PhysicalNodes.PhysicalAggregateCandidateNode>(physical);
        var aggregate = (PhysicalNodes.PhysicalAggregateCandidateNode)physical;
        Assert.HasCount(1, aggregate.GroupKeys);
        Assert.AreEqual("Name", aggregate.GroupKeyNames[0]);
    }

    [TestMethod]
    public void Lower_WhenAggregateHasTwoTypedKeys_ShouldCreateAggregateCandidate()
    {
        var logical = new AggregateNode(
            [new ColumnRef("t", "Name", typeof(string)), new ColumnRef("t", "Id", typeof(int))],
            ["Name", "Id"],
            [typeof(string), typeof(int)],
            [CreateAggregateBinding()],
            CreateScan());

        var physical = CreatePhysicalBuilder().Lower(logical);

        Assert.IsInstanceOfType<PhysicalNodes.PhysicalAggregateCandidateNode>(physical);
        var aggregate = (PhysicalNodes.PhysicalAggregateCandidateNode)physical;
        Assert.HasCount(2, aggregate.GroupKeys);
    }

    [TestMethod]
    public void Lower_WhenAggregateHasObjectKey_ShouldCreateAggregateCandidate()
    {
        var logical = new AggregateNode(
            [new ColumnRef("t", "Payload", typeof(object))],
            ["Payload"],
            [typeof(object)],
            [CreateAggregateBinding()],
            CreateScan("t", ("Payload", typeof(object))));

        var physical = CreatePhysicalBuilder().Lower(logical);

        Assert.IsInstanceOfType<PhysicalNodes.PhysicalAggregateCandidateNode>(physical);
        var aggregate = (PhysicalNodes.PhysicalAggregateCandidateNode)physical;
        Assert.AreEqual(typeof(object), aggregate.GroupKeyTypes[0]);
    }

    [TestMethod]
    public void Lower_WhenAggregateHasManyKeys_ShouldCreateAggregateCandidate()
    {
        var logical = new AggregateNode(
            [
                new ColumnRef("t", "K0", typeof(string)),
                new ColumnRef("t", "K1", typeof(int)),
                new ColumnRef("t", "K2", typeof(int)),
                new ColumnRef("t", "K3", typeof(int)),
                new ColumnRef("t", "K4", typeof(int)),
                new ColumnRef("t", "K5", typeof(int)),
                new ColumnRef("t", "K6", typeof(int)),
                new ColumnRef("t", "K7", typeof(int)),
                new ColumnRef("t", "K8", typeof(int))
            ],
            ["K0", "K1", "K2", "K3", "K4", "K5", "K6", "K7", "K8"],
            [typeof(string), typeof(int), typeof(int), typeof(int), typeof(int), typeof(int), typeof(int), typeof(int), typeof(int)],
            [CreateAggregateBinding()],
            CreateScan(
                "t",
                ("K0", typeof(string)),
                ("K1", typeof(int)),
                ("K2", typeof(int)),
                ("K3", typeof(int)),
                ("K4", typeof(int)),
                ("K5", typeof(int)),
                ("K6", typeof(int)),
                ("K7", typeof(int)),
                ("K8", typeof(int))));

        var physical = CreatePhysicalBuilder().Lower(logical);

        Assert.IsInstanceOfType<PhysicalNodes.PhysicalAggregateCandidateNode>(physical);
        var aggregate = (PhysicalNodes.PhysicalAggregateCandidateNode)physical;
        Assert.HasCount(9, aggregate.GroupKeys);
    }

    [TestMethod]
    public void Lower_WhenSortTakeShapeIsPresent_ShouldLeaveOrderingStrategyToOptimizer()
    {
        var logical = new TakeNode(
            3,
            new SortNode(
                [new OrderField(new ColumnRef("t", "Name", typeof(string)), false)],
                CreateScan()));

        var physical = CreatePhysicalBuilder().Lower(logical);
        var optimized = Optimize(physical);

        Assert.IsInstanceOfType<PhysicalNodes.PhysicalTakeNode>(physical);
        Assert.IsInstanceOfType<PhysicalNodes.PhysicalSortNode>(((PhysicalNodes.PhysicalTakeNode)physical).Input);
        Assert.IsInstanceOfType<PhysicalNodes.PhysicalTopNNode>(optimized);
    }

    [TestMethod]
    public void Lower_WhenJoinIsPureEquiJoin_ShouldChooseHashJoin()
    {
        var logical = new JoinNode(
            JoinKind.Inner,
            new BinaryOp(
                BinaryOpKind.Equal,
                new ColumnRef("a", "Id", typeof(int)),
                new ColumnRef("b", "UserId", typeof(int)),
                typeof(bool)),
            CreateScan("a", ("Id", typeof(int))),
            CreateScan("b", ("UserId", typeof(int))));

        var physical = Optimize(CreatePhysicalBuilder().Lower(logical));

        Assert.IsInstanceOfType<PhysicalNodes.PhysicalHashJoinNode>(physical);
        var join = (PhysicalNodes.PhysicalHashJoinNode)physical;
        Assert.AreEqual("b", ((ColumnRef)join.BuildKeys[0]).Alias);
        Assert.AreEqual("a", ((ColumnRef)join.ProbeKeys[0]).Alias);
        Assert.IsNull(join.Residual);
    }

    [TestMethod]
    public void Lower_WhenRightOuterJoinIsPureEquiJoin_ShouldFlipBuildAndProbeSides()
    {
        var logical = new JoinNode(
            JoinKind.RightOuter,
            new BinaryOp(
                BinaryOpKind.Equal,
                new ColumnRef("a", "Id", typeof(int)),
                new ColumnRef("b", "UserId", typeof(int)),
                typeof(bool)),
            CreateScan("a", ("Id", typeof(int))),
            CreateScan("b", ("UserId", typeof(int))));

        var physical = Optimize(CreatePhysicalBuilder().Lower(logical));

        Assert.IsInstanceOfType<PhysicalNodes.PhysicalHashJoinNode>(physical);
        var join = (PhysicalNodes.PhysicalHashJoinNode)physical;
        Assert.AreEqual("a", ((ColumnRef)join.BuildKeys[0]).Alias);
        Assert.AreEqual("b", ((ColumnRef)join.ProbeKeys[0]).Alias);
    }

    [TestMethod]
    public void Lower_WhenJoinHasEquiAndResidualPredicate_ShouldChooseHashJoinWithResidual()
    {
        var residual = new BinaryOp(
            BinaryOpKind.GreaterThan,
            new ColumnRef("a", "Score", typeof(int)),
            new ColumnRef("b", "Score", typeof(int)),
            typeof(bool));
        var logical = new JoinNode(
            JoinKind.Inner,
            new BinaryOp(
                BinaryOpKind.And,
                new BinaryOp(
                    BinaryOpKind.Equal,
                    new ColumnRef("a", "Id", typeof(int)),
                    new ColumnRef("b", "UserId", typeof(int)),
                    typeof(bool)),
                residual,
                typeof(bool)),
            CreateScan("a", ("Id", typeof(int)), ("Score", typeof(int))),
            CreateScan("b", ("UserId", typeof(int)), ("Score", typeof(int))));

        var physical = Optimize(CreatePhysicalBuilder().Lower(logical));

        Assert.IsInstanceOfType<PhysicalNodes.PhysicalHashJoinNode>(physical);
        var join = (PhysicalNodes.PhysicalHashJoinNode)physical;
        Assert.AreEqual("b", ((ColumnRef)join.BuildKeys[0]).Alias);
        Assert.AreEqual("a", ((ColumnRef)join.ProbeKeys[0]).Alias);
        Assert.AreSame(residual, join.Residual);
    }

    [TestMethod]
    public void Lower_WhenAsOfJoinHasEquiAndRangePredicate_ShouldChooseNestedLoopJoin()
    {
        var logical = new JoinNode(
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
            CreateScan("a", ("Symbol", typeof(string)), ("Timestamp", typeof(long))),
            CreateScan("b", ("Symbol", typeof(string)), ("Timestamp", typeof(long))));

        var physical = Optimize(CreatePhysicalBuilder().Lower(logical));

        Assert.IsInstanceOfType<PhysicalNodes.PhysicalNestedLoopJoinNode>(physical);
    }

    [TestMethod]
    public void Lower_WhenSortMergeJoinIsEnabledForNonEquiInnerJoin_ShouldChooseSortMergeJoin()
    {
        var predicate = new BinaryOp(
            BinaryOpKind.GreaterThan,
            new ColumnRef("a", "Id", typeof(int)),
            new ColumnRef("b", "UserId", typeof(int)),
            typeof(bool));
        var logical = new JoinNode(
            JoinKind.Inner,
            predicate,
            CreateScan("a", ("Id", typeof(int))),
            CreateScan("b", ("UserId", typeof(int))));

        var physical = Optimize(CreatePhysicalBuilder().Lower(logical));

        Assert.IsInstanceOfType<PhysicalNodes.PhysicalSortMergeJoinNode>(physical);
        var join = (PhysicalNodes.PhysicalSortMergeJoinNode)physical;
        Assert.AreEqual(BinaryOpKind.GreaterThan, join.ComparisonKind);
        Assert.AreEqual("a", ((ColumnRef)join.LeftKey).Alias);
        Assert.AreEqual("b", ((ColumnRef)join.RightKey).Alias);
        Assert.AreSame(predicate, join.Residual);
    }

    [TestMethod]
    public void Lower_WhenSortMergeJoinIsDisabledForNonEquiInnerJoin_ShouldChooseNestedLoopJoin()
    {
        var logical = new JoinNode(
            JoinKind.Inner,
            new BinaryOp(
                BinaryOpKind.GreaterThan,
                new ColumnRef("a", "Id", typeof(int)),
                new ColumnRef("b", "UserId", typeof(int)),
                typeof(bool)),
            CreateScan("a", ("Id", typeof(int))),
            CreateScan("b", ("UserId", typeof(int))));

        var options = new CompilationOptions(useSortMergeJoin: false);
        var physical = Optimize(CreatePhysicalBuilder(options).Lower(logical), options);

        Assert.IsInstanceOfType<PhysicalNodes.PhysicalNestedLoopJoinNode>(physical);
    }

    [TestMethod]
    public void Lower_WhenHashJoinIsDisabled_ShouldChooseNestedLoopJoin()
    {
        var logical = new JoinNode(
            JoinKind.Inner,
            new BinaryOp(
                BinaryOpKind.Equal,
                new ColumnRef("a", "Id", typeof(int)),
                new ColumnRef("b", "UserId", typeof(int)),
                typeof(bool)),
            CreateScan("a", ("Id", typeof(int))),
            CreateScan("b", ("UserId", typeof(int))));

        var options = new CompilationOptions(useHashJoin: false);
        var physical = Optimize(CreatePhysicalBuilder(options).Lower(logical), options);

        Assert.IsInstanceOfType<PhysicalNodes.PhysicalNestedLoopJoinNode>(physical);
    }

    [TestMethod]
    public void Lower_WhenSortMergeJoinPredicateUsesReversedOperands_ShouldNormalizeToLeftProbe()
    {
        var logical = new JoinNode(
            JoinKind.Inner,
            new BinaryOp(
                BinaryOpKind.LessThan,
                new ColumnRef("b", "UserId", typeof(int)),
                new ColumnRef("a", "Id", typeof(int)),
                typeof(bool)),
            CreateScan("a", ("Id", typeof(int))),
            CreateScan("b", ("UserId", typeof(int))));

        var physical = Optimize(CreatePhysicalBuilder().Lower(logical));

        Assert.IsInstanceOfType<PhysicalNodes.PhysicalSortMergeJoinNode>(physical);
        var join = (PhysicalNodes.PhysicalSortMergeJoinNode)physical;
        Assert.AreEqual(BinaryOpKind.GreaterThan, join.ComparisonKind);
        Assert.AreEqual("a", ((ColumnRef)join.LeftKey).Alias);
        Assert.AreEqual("b", ((ColumnRef)join.RightKey).Alias);
    }

    [TestMethod]
    public void Lower_WhenSortMergeJoinPredicateUsesConstantOffset_ShouldKeepRightKeyExpression()
    {
        var rightKey = new BinaryOp(
            BinaryOpKind.Add,
            new ColumnRef("b", "UserId", typeof(int)),
            new Literal(5, typeof(int)),
            typeof(int));
        var logical = new JoinNode(
            JoinKind.Inner,
            new BinaryOp(
                BinaryOpKind.GreaterOrEqual,
                new ColumnRef("a", "Id", typeof(int)),
                rightKey,
                typeof(bool)),
            CreateScan("a", ("Id", typeof(int))),
            CreateScan("b", ("UserId", typeof(int))));

        var physical = Optimize(CreatePhysicalBuilder().Lower(logical));

        Assert.IsInstanceOfType<PhysicalNodes.PhysicalSortMergeJoinNode>(physical);
        var join = (PhysicalNodes.PhysicalSortMergeJoinNode)physical;
        Assert.AreEqual(BinaryOpKind.GreaterOrEqual, join.ComparisonKind);
        Assert.AreSame(rightKey, join.RightKey);
    }

    [TestMethod]
    public void Lower_WhenNonEquiJoinUsesUnsupportedPredicate_ShouldChooseNestedLoopJoin()
    {
        var logical = new JoinNode(
            JoinKind.Inner,
            new BinaryOp(
                BinaryOpKind.NotEqual,
                new ColumnRef("a", "Id", typeof(int)),
                new ColumnRef("b", "UserId", typeof(int)),
                typeof(bool)),
            CreateScan("a", ("Id", typeof(int))),
            CreateScan("b", ("UserId", typeof(int))));

        var physical = Optimize(CreatePhysicalBuilder().Lower(logical));

        Assert.IsInstanceOfType<PhysicalNodes.PhysicalNestedLoopJoinNode>(physical);
    }

    [TestMethod]
    public void Lower_WhenOuterJoinUsesNonEquiPredicate_ShouldChooseNestedLoopJoin()
    {
        var logical = new JoinNode(
            JoinKind.LeftOuter,
            new BinaryOp(
                BinaryOpKind.GreaterThan,
                new ColumnRef("a", "Id", typeof(int)),
                new ColumnRef("b", "UserId", typeof(int)),
                typeof(bool)),
            CreateScan("a", ("Id", typeof(int))),
            CreateScan("b", ("UserId", typeof(int))));

        var physical = Optimize(CreatePhysicalBuilder().Lower(logical));

        Assert.IsInstanceOfType<PhysicalNodes.PhysicalNestedLoopJoinNode>(physical);
    }

    [TestMethod]
    public void Lower_WhenApplyNode_ShouldChooseNestedLoopApply()
    {
        var logical = new ApplyNode(
            ApplyKind.Cross,
            CreateScan("a", ("Id", typeof(int))),
            CreateScan("b", ("Value", typeof(string))));

        var physical = CreatePhysicalBuilder().Lower(logical);

        Assert.IsInstanceOfType<PhysicalNodes.PhysicalNestedLoopApplyNode>(physical);
    }

    [TestMethod]
    public void Lower_WhenWindowNode_ShouldCreateWindowWithoutMaterialization()
    {
        var registration = new WindowRegistration(
            typeof(string).GetMethod("ToUpper", Type.EmptyTypes)!,
            "ToUpper",
            [],
            [],
            [],
            0,
            typeof(string));
        var logical = new WindowNode([registration], CreateScan());

        var physical = CreatePhysicalBuilder().Lower(logical);

        Assert.IsInstanceOfType<PhysicalNodes.PhysicalWindowNode>(physical);
        var window = (PhysicalNodes.PhysicalWindowNode)physical;
        Assert.IsInstanceOfType<PhysicalNodes.PhysicalSchemaScanNode>(window.Input);
    }

    [TestMethod]
    public void Lower_WhenDescNode_ShouldPreserveTypeColumnAndArguments()
    {
        var logical = new DescNode(
            "test",
            "entities",
            DescType.Column,
            "Self.Children",
            [new Literal(7, typeof(int)), new Literal("sample", typeof(string))],
            "desc-source",
            OutputSchema.Empty);

        var physical = CreatePhysicalBuilder().Lower(logical);

        Assert.IsInstanceOfType<PhysicalNodes.PhysicalDescNode>(physical);
        var desc = (PhysicalNodes.PhysicalDescNode)physical;
        Assert.AreEqual(DescType.Column, desc.Type);
        Assert.AreEqual("Self.Children", desc.Column);
        Assert.AreEqual("desc-source", desc.SourceContextId);
        Assert.HasCount(2, desc.Arguments);
        Assert.AreEqual(7, ((Literal)desc.Arguments[0]).Value);
        Assert.AreEqual("sample", ((Literal)desc.Arguments[1]).Value);
    }

    [TestMethod]
    public void Lower_WhenSetOperation_ShouldPopulateFieldIndexesAndTypes()
    {
        var logical = new SetOperationNode(
            SetOpKind.UnionAll,
            CreateScan("a", ("Id", typeof(int)), ("Name", typeof(string))),
            CreateScan("b", ("Id", typeof(int)), ("Name", typeof(string))),
            []);

        var physical = CreatePhysicalBuilder().Lower(logical);

        Assert.IsInstanceOfType<PhysicalNodes.PhysicalSetOperationNode>(physical);
        var setOperation = (PhysicalNodes.PhysicalSetOperationNode)physical;
        CollectionAssert.AreEqual(new[] { 0, 1 }, setOperation.FieldIndexes);
        CollectionAssert.AreEqual(new[] { typeof(int), typeof(string) }, setOperation.FieldTypes);
    }

    [TestMethod]
    public void Lower_WhenSetOperationHasEmptyKeys_ShouldUseEveryOutputColumnAsKey()
    {
        var logical = new SetOperationNode(
            SetOpKind.Union,
            CreateScan(
                "a",
                ("K0", typeof(int)),
                ("K1", typeof(string)),
                ("K2", typeof(decimal))),
            CreateScan(
                "b",
                ("K0", typeof(int)),
                ("K1", typeof(string)),
                ("K2", typeof(decimal))),
            []);

        var physical = CreatePhysicalBuilder().Lower(logical);

        Assert.IsInstanceOfType<PhysicalNodes.PhysicalSetOperationNode>(physical);
        var setOperation = (PhysicalNodes.PhysicalSetOperationNode)physical;
        CollectionAssert.AreEqual(new[] { 0, 1, 2 }, setOperation.FieldIndexes);
        CollectionAssert.AreEqual(
            new[] { typeof(int), typeof(string), typeof(decimal) },
            setOperation.FieldTypes);
    }

    [TestMethod]
    public void Lower_WhenCteNode_ShouldLowerDefinitionsAndQuery()
    {
        var logical = new CteNode(
            [new CteDefinition("cte", CreateScan("inner", ("Id", typeof(int))))],
            new CteRefNode("cte", "c", CreateSchema(("Id", typeof(int)))));

        var physical = CreatePhysicalBuilder().Lower(logical);

        Assert.IsInstanceOfType<PhysicalNodes.PhysicalCteNode>(physical);
        var cte = (PhysicalNodes.PhysicalCteNode)physical;
        Assert.HasCount(1, cte.Definitions);
        Assert.IsInstanceOfType<PhysicalNodes.PhysicalSchemaScanNode>(cte.Definitions[0].Plan);
        Assert.IsInstanceOfType<PhysicalNodes.PhysicalCteRefNode>(cte.Query);
    }

    [TestMethod]
    public void Lower_WhenMultiStatementIsEmpty_ShouldPreserveEmptySchema()
    {
        var physical = CreatePhysicalBuilder().Lower(new MultiStatementNode([]));

        Assert.IsInstanceOfType<PhysicalNodes.PhysicalMultiStatementNode>(physical);
        Assert.IsEmpty(physical.OutputSchema.Columns);
    }

    [TestMethod]
    public void Print_WhenLoweringSingleKeyAggregate_ShouldShowCandidate()
    {
        var logical = new AggregateNode(
            [new ColumnRef("t", "Name", typeof(string))],
            ["Name"],
            [typeof(string)],
            [CreateAggregateBinding()],
            CreateScan());

        var output = PhysicalPlanPrinter.Print(CreatePhysicalBuilder().Lower(logical));

        PlanTextAssertions.AreEqual(
            "PhysicalAggregateCandidate [keys: Name] [aggs: Count]\r\n  PhysicalSchemaScan [#test.data() as t]",
            output);
    }
}
