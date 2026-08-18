using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Logical;
using Musoq.Evaluator.IR.Logical.Nodes;
using Musoq.Evaluator.IR.Optimization;
using Musoq.Evaluator.IR.Planning;
using Musoq.Evaluator.IR.Planning.SourcePlanning;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;
using Musoq.Parser.Nodes;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Optimization;
using LogicalSkipNode = Musoq.Evaluator.IR.Logical.Nodes.SkipNode;
using LogicalTakeNode = Musoq.Evaluator.IR.Logical.Nodes.TakeNode;
using LogicalWindowNode = Musoq.Evaluator.IR.Logical.Nodes.WindowNode;
using ParserSchemaFromNode = Musoq.Parser.Nodes.From.SchemaFromNode;

namespace Musoq.Evaluator.Tests.IR;

[TestClass]
public class SourceTransferPlannerTests
{
    [TestMethod]
    public void Plan_WhenSourceAdvertisesCapabilityAndShapeIsExact_SelectsReadonlyStruct()
    {
        var source = CreateSource(
            "source:0",
            [new SchemaColumn("Id", 0, typeof(int)), new SchemaColumn("Name", 1, typeof(string))],
            SourceTransferCapabilities.QueryScopedRows);

        var result = SourceTransferPlanner.Plan(
            CreateContext(),
            CreateFacts(source, SourceTransferCapabilities.QueryScopedRows));

        var plan = result.PlansBySourceId[source.SourceContextId];
        Assert.AreEqual(SourceTransferMode.QueryScopedRows, plan.Mode);
        Assert.AreEqual(SourceQueryRowCarrier.ReadonlyStruct, plan.Carrier);
        Assert.IsNotNull(plan.Shape);
        Assert.AreEqual(2, plan.Shape!.Fields.Count);
        Assert.AreEqual("Id", plan.Shape.Fields[0].Name);
        Assert.AreEqual("Name", plan.Shape.Fields[1].Name);
        Assert.AreEqual(64, plan.Shape.Fingerprint.Length);
        Assert.IsTrue(result.Decisions.Any(static decision => decision.Outcome == "Selected"));
    }

    [TestMethod]
    public void Plan_WhenSourceDoesNotAdvertiseCapability_FallsBackToDeclaredRows()
    {
        var source = CreateSource(
            "source:0",
            [new SchemaColumn("Id", 0, typeof(int))],
            SourceTransferCapabilities.None);

        var plan = SourceTransferPlanner.Plan(CreateContext(), CreateFacts(source, SourceTransferCapabilities.None)).PlansBySourceId[source.SourceContextId];

        Assert.AreEqual(SourceTransferMode.DeclaredRows, plan.Mode);
        Assert.IsNull(plan.Shape);
        Assert.Contains("did not advertise", plan.Reason);
    }

    [TestMethod]
    public void Plan_WhenTargetDoesNotSupportCapability_FallsBackToDeclaredRows()
    {
        var source = CreateSource(
            "source:0",
            [new SchemaColumn("Id", 0, typeof(int))],
            SourceTransferCapabilities.QueryScopedRows);

        var plan = SourceTransferPlanner.Plan(
            CreateContext(SourceTransferCapabilities.None),
            CreateFacts(source, SourceTransferCapabilities.QueryScopedRows)).PlansBySourceId[source.SourceContextId];

        Assert.AreEqual(SourceTransferMode.DeclaredRows, plan.Mode);
        Assert.Contains("target does not support", plan.Reason);
    }

    [TestMethod]
    public void Plan_WhenShapeContainsObjectType_FallsBackWithDiagnosticReason()
    {
        var source = CreateSource(
            "source:0",
            [new SchemaColumn("Value", 0, typeof(object))],
            SourceTransferCapabilities.QueryScopedRows);

        var plan = SourceTransferPlanner.Plan(CreateContext(), CreateFacts(source, SourceTransferCapabilities.QueryScopedRows)).PlansBySourceId[source.SourceContextId];

        Assert.AreEqual(SourceTransferMode.DeclaredRows, plan.Mode);
        Assert.Contains("object runtime type", plan.Reason);
    }

    [TestMethod]
    [DynamicData(nameof(UnsupportedQueryRowFieldTypes))]
    public void Plan_WhenShapeContainsUnsupportedClrType_FallsBackWithDiagnosticReason(Type fieldType)
    {
        var plan = PlanSingleColumn(fieldType);

        Assert.AreEqual(SourceTransferMode.DeclaredRows, plan.Mode);
        Assert.Contains("unusable CLR type", plan.Reason);
    }

    [TestMethod]
    public void Plan_WhenShapeContainsVisibleArrayType_SelectsQueryScopedRows()
    {
        var plan = PlanSingleColumn(typeof(int[]));

        Assert.AreEqual(SourceTransferMode.QueryScopedRows, plan.Mode);
    }

    [TestMethod]
    public void Plan_WhenShapeContainsVisibleClosedGenericType_SelectsQueryScopedRows()
    {
        var plan = PlanSingleColumn(typeof(IReadOnlyDictionary<string, int>));

        Assert.AreEqual(SourceTransferMode.QueryScopedRows, plan.Mode);
    }

    [TestMethod]
    public void Plan_WhenEstimatedPayloadIsWide_SelectsSealedClass()
    {
        var columns = Enumerable.Range(0, 5)
            .Select(index => (ISchemaColumn)new SchemaColumn($"Value{index}", index, typeof(Guid)))
            .ToArray();
        var source = CreateSource("source:0", columns, SourceTransferCapabilities.QueryScopedRows);

        var plan = SourceTransferPlanner.Plan(CreateContext(), CreateFacts(source, SourceTransferCapabilities.QueryScopedRows)).PlansBySourceId[source.SourceContextId];

        Assert.AreEqual(SourceTransferMode.QueryScopedRows, plan.Mode);
        Assert.AreEqual(SourceQueryRowCarrier.SealedClass, plan.Carrier);
        Assert.Contains("sealed class", plan.Reason);
    }

    [TestMethod]
    public void Plan_WhenProjectionIsExactlyEmpty_SelectsZeroFieldShape()
    {
        var source = CreateSource(
            "source:0",
            [new SchemaColumn("Id", 0, typeof(int))],
            SourceTransferCapabilities.QueryScopedRows) with
        {
            RequiredColumns = [],
            ProjectedColumns = [],
            ProjectedSchemaColumns = [],
            QueryRowProjection = SourceQueryRowProjection.Exact([], "exact empty projection")
        };

        var plan = SourceTransferPlanner.Plan(
            CreateContext(),
            CreateFacts(source, SourceTransferCapabilities.QueryScopedRows)).PlansBySourceId[source.SourceContextId];

        Assert.AreEqual(SourceTransferMode.QueryScopedRows, plan.Mode);
        Assert.IsEmpty(plan.Shape!.Fields);
        Assert.AreEqual(new QueryRowShape([]).Fingerprint, plan.Shape.Fingerprint);
    }

    [TestMethod]
    public void Plan_WhenDescriptorContainsDuplicateNames_FallsBackDeterministically()
    {
        var columns = new ISchemaColumn[]
        {
            new SchemaColumn("Value", 0, typeof(int)),
            new SchemaColumn("Value", 1, typeof(int))
        };
        var source = CreateSource(
            "source:0",
            columns,
            SourceTransferCapabilities.QueryScopedRows) with
        {
            QueryRowProjection = SourceQueryRowProjection.Unavailable("descriptor validation required"),
            ProjectedColumns = [],
            ProjectedSchemaColumns = []
        };

        var plan = SourceTransferPlanner.Plan(
            CreateContext(),
            CreateFacts(source, SourceTransferCapabilities.QueryScopedRows, columns)).PlansBySourceId[source.SourceContextId];

        Assert.AreEqual(SourceTransferMode.DeclaredRows, plan.Mode);
        Assert.Contains("duplicate name 'Value'", plan.Reason);
    }

    [TestMethod]
    public void Plan_WhenDescriptorContainsDuplicateOrdinals_FallsBackDeterministically()
    {
        var columns = new ISchemaColumn[]
        {
            new SchemaColumn("First", 0, typeof(int)),
            new SchemaColumn("Second", 0, typeof(int))
        };
        var source = CreateSource(
            "source:0",
            columns,
            SourceTransferCapabilities.QueryScopedRows) with
        {
            QueryRowProjection = SourceQueryRowProjection.Unavailable("descriptor validation required"),
            ProjectedColumns = [],
            ProjectedSchemaColumns = []
        };

        var plan = SourceTransferPlanner.Plan(
            CreateContext(),
            CreateFacts(source, SourceTransferCapabilities.QueryScopedRows, columns)).PlansBySourceId[source.SourceContextId];

        Assert.AreEqual(SourceTransferMode.DeclaredRows, plan.Mode);
        Assert.Contains("duplicate ordinal 0", plan.Reason);
    }

    [TestMethod]
    public void Plan_WhenRequiredDescriptorColumnIsMissing_FallsBackDeterministically()
    {
        var descriptorColumns = new ISchemaColumn[] { new SchemaColumn("Id", 0, typeof(int)) };
        var source = CreateSource(
            "source:0",
            descriptorColumns,
            SourceTransferCapabilities.QueryScopedRows) with
        {
            RequiredColumns = ["Missing"],
            ProjectedColumns = [],
            ProjectedSchemaColumns = [],
            QueryRowProjection = SourceQueryRowProjection.Unavailable("required field was not inferred")
        };

        var plan = SourceTransferPlanner.Plan(
            CreateContext(),
            CreateFacts(source, SourceTransferCapabilities.QueryScopedRows, descriptorColumns)).PlansBySourceId[source.SourceContextId];

        Assert.AreEqual(SourceTransferMode.DeclaredRows, plan.Mode);
        Assert.Contains("required source columns were unresolved: Missing", plan.Reason);
    }

    [TestMethod]
    public void Plan_WhenNarrowSourceCrossesJoinBeforeProjection_SelectsSealedClass()
    {
        var source = CreateLogicalScan("source:0", "source");
        var other = CreateLogicalScan("source:1", "other");
        var join = new JoinNode(
            JoinKind.Inner,
            new BinaryOp(
                BinaryOpKind.Equal,
                new ColumnRef("source", "Id", typeof(int)),
                new ColumnRef("other", "Id", typeof(int)),
                typeof(bool)),
            source,
            other);
        var plan = PlanSingleColumn(typeof(int), CreateProjection(join));

        Assert.AreEqual(SourceQueryRowCarrier.SealedClass, plan.Carrier);
        Assert.AreEqual(SourceQueryRowLifetime.EscapesScan, plan.Lifetime);
    }

    [TestMethod]
    public void Plan_WhenSortConsumesSourceBeforeProjection_SelectsSealedClass()
    {
        var source = CreateLogicalScan("source:0", "source");
        var sort = new SortNode(
            [new OrderField(new ColumnRef("source", "Id", typeof(int)), false)],
            source);
        var plan = PlanSingleColumn(typeof(int), CreateProjection(sort));

        Assert.AreEqual(SourceQueryRowCarrier.SealedClass, plan.Carrier);
        Assert.AreEqual(SourceQueryRowLifetime.EscapesScan, plan.Lifetime);
    }

    [TestMethod]
    public void Plan_WhenProjectionPrecedesSort_KeepsReadonlyStruct()
    {
        var source = CreateLogicalScan("source:0", "source");
        var projection = CreateProjection(source);
        var sort = new SortNode(
            [new OrderField(new ColumnRef(string.Empty, "Id", typeof(int)), false)],
            projection);
        var plan = PlanSingleColumn(typeof(int), sort);

        Assert.AreEqual(SourceQueryRowCarrier.ReadonlyStruct, plan.Carrier);
        Assert.AreEqual(SourceQueryRowLifetime.ScanLocal, plan.Lifetime);
    }

    [TestMethod]
    public void Plan_WhenStreamingOperatorsPrecedeProjection_KeepsReadonlyStruct()
    {
        var source = CreateLogicalScan("source:0", "source");
        var filter = new FilterNode(new Literal(true, typeof(bool)), source);
        var skip = new LogicalSkipNode(1, filter);
        var take = new LogicalTakeNode(2, skip);
        var projection = CreateProjection(take) with { IsDistinct = true };
        var plan = PlanSingleColumn(typeof(int), projection);

        Assert.AreEqual(SourceQueryRowCarrier.ReadonlyStruct, plan.Carrier);
        Assert.AreEqual(SourceQueryRowLifetime.ScanLocal, plan.Lifetime);
    }

    [TestMethod]
    [DynamicData(nameof(AdditionalRetainingBoundaryPlans))]
    public void Plan_WhenRetainingBoundaryConsumesSource_SelectsSealedClass(LogicalNode logicalPlan)
    {
        var plan = PlanSingleColumn(typeof(int), logicalPlan);

        Assert.AreEqual(SourceQueryRowCarrier.SealedClass, plan.Carrier);
        Assert.AreEqual(SourceQueryRowLifetime.EscapesScan, plan.Lifetime);
    }

    [TestMethod]
    public void Plan_WhenAggregateReplacesSourceShape_KeepsReadonlyStruct()
    {
        var source = CreateLogicalScan("source:0", "source");
        var aggregate = new AggregateNode(
            [new ColumnRef("source", "Id", typeof(int))],
            ["Id"],
            [typeof(int)],
            [],
            source);
        var plan = PlanSingleColumn(typeof(int), aggregate);

        Assert.AreEqual(SourceQueryRowCarrier.ReadonlyStruct, plan.Carrier);
        Assert.AreEqual(SourceQueryRowLifetime.ScanLocal, plan.Lifetime);
    }

    [TestMethod]
    public void Plan_WhenSourceHasMultipleConsumers_SelectsSealedClass()
    {
        var source = CreateLogicalScan("source:0", "source");
        var join = new JoinNode(
            JoinKind.Cross,
            new Literal(true, typeof(bool)),
            source,
            source);
        var plan = PlanSingleColumn(typeof(int), CreateProjection(join));

        Assert.AreEqual(SourceQueryRowCarrier.SealedClass, plan.Carrier);
        Assert.Contains("multiple logical consumers", plan.Reason);
    }

    [TestMethod]
    public void Plan_WhenUnknownOperatorConsumesSource_SelectsSealedClass()
    {
        var source = CreateLogicalScan("source:0", "source");
        var plan = PlanSingleColumn(typeof(int), CreateProjection(new UnknownBoundaryNode(source)));

        Assert.AreEqual(SourceQueryRowCarrier.SealedClass, plan.Carrier);
        Assert.Contains(nameof(UnknownBoundaryNode), plan.Reason);
    }

    [TestMethod]
    public void Plan_WhenCteProjectionReplacesSourceShape_KeepsReadonlyStruct()
    {
        var source = CreateLogicalScan("source:0", "source");
        var definition = CreateProjection(source);
        var reference = new CteRefNode("items", "items", definition.OutputSchema);
        var cte = new CteNode([new CteDefinition("items", definition)], CreateProjection(reference));
        var plan = PlanSingleColumn(typeof(int), cte);

        Assert.AreEqual(SourceQueryRowCarrier.ReadonlyStruct, plan.Carrier);
        Assert.AreEqual(SourceQueryRowLifetime.ScanLocal, plan.Lifetime);
    }

    [TestMethod]
    public void Plan_WhenCteRetainsUnprojectedSource_SelectsSealedClass()
    {
        var source = CreateLogicalScan("source:0", "source");
        var reference = new CteRefNode("items", "items", source.OutputSchema);
        var cte = new CteNode([new CteDefinition("items", source)], CreateProjection(reference));
        var plan = PlanSingleColumn(typeof(int), cte);

        Assert.AreEqual(SourceQueryRowCarrier.SealedClass, plan.Carrier);
        Assert.Contains(nameof(CteNode), plan.Reason);
    }

    [TestMethod]
    public void PhysicalLowering_WhenTransferPlanIsSelected_CarriesItToSchemaScan()
    {
        var source = CreateSource(
            "source:0",
            [new SchemaColumn("Id", 0, typeof(int))],
            SourceTransferCapabilities.QueryScopedRows);
        var transferPlan = SourceTransferPlanner.Plan(
            CreateContext(),
            CreateFacts(source, SourceTransferCapabilities.QueryScopedRows)).PlansBySourceId[source.SourceContextId];
        var logical = new SchemaScanNode(
            "test",
            "rows",
            [],
            "source",
            new OutputSchema([new ColumnSchema("Id", typeof(int), 0)]),
            source.SourceContextId);

        var physical = new PhysicalPlanBuilder(
            predicateMovementPlans: null,
            strategyPlan: new PhysicalStrategyPlan(),
            sourceTransferPlans: new Dictionary<string, SourceTransferStrategyPlan>
            {
                [source.SourceContextId] = transferPlan
            }).Lower(logical);

        var scan = (PhysicalSchemaScanNode)physical;
        Assert.AreEqual(SourceTransferMode.QueryScopedRows, scan.SourceTransferStrategy!.Mode);
        Assert.AreEqual(transferPlan.Shape!.Fingerprint, scan.SourceTransferStrategy.Shape!.Fingerprint);
    }

    private static SourcePlanProperties CreateSource(
        string sourceContextId,
        ISchemaColumn[] columns,
        SourceTransferCapabilities capabilities)
    {
        return new SourcePlanProperties(
            sourceContextId,
            "source",
            "test",
            "rows",
            columns.Select(static column => column.ColumnName).ToArray(),
            [],
            columns.Select(static column => column.ColumnName).ToArray(),
            columns,
            SourceQueryRowProjection.Exact(columns, "exact metadata"),
            PlanningConfidence.High,
            "exact metadata");
    }

    private static SourceTransferStrategyPlan PlanSingleColumn(
        Type columnType,
        LogicalNode? logicalPlan = null)
    {
        var source = CreateSource(
            "source:0",
            [new SchemaColumn("Value", 0, columnType)],
            SourceTransferCapabilities.QueryScopedRows);

        return SourceTransferPlanner.Plan(
            CreateContext(logicalPlan: logicalPlan),
            CreateFacts(source, SourceTransferCapabilities.QueryScopedRows)).PlansBySourceId[source.SourceContextId];
    }

    private static SchemaScanNode CreateLogicalScan(string sourceContextId, string alias)
    {
        return new SchemaScanNode(
            "test",
            "rows",
            [],
            alias,
            new OutputSchema([new ColumnSchema("Id", typeof(int), 0)]),
            sourceContextId);
    }

    private static ProjectNode CreateProjection(LogicalNode input)
    {
        return new ProjectNode(
            [new ProjectedField("Id", new ColumnRef("source", "Id", typeof(int)), 0)],
            input);
    }

    private static IEnumerable<object[]> AdditionalRetainingBoundaryPlans
    {
        get
        {
            yield return [CreateOuterJoinPlan()];
            yield return [CreateApplyPlan()];
            yield return [CreateWindowPlan()];
            yield return [CreateSetOperationPlan()];
            yield return [CreateRecursiveCtePlan()];
            yield return [CreateUnpivotPlan()];
        }
    }

    private static LogicalNode CreateOuterJoinPlan()
    {
        var source = CreateLogicalScan("source:0", "source");
        var other = CreateLogicalScan("source:1", "other");
        return CreateProjection(new JoinNode(
            JoinKind.LeftOuter,
            new Literal(true, typeof(bool)),
            source,
            other));
    }

    private static LogicalNode CreateApplyPlan()
    {
        var source = CreateLogicalScan("source:0", "source");
        var values = new ValuesScanNode(
            "values",
            [new ValuesScanRow([new ValuesScanField("Value", new Literal(1, typeof(int)))])],
            new OutputSchema([new ColumnSchema("Value", typeof(int), 0)]));
        return CreateProjection(new ApplyNode(ApplyKind.Cross, source, values));
    }

    private static LogicalNode CreateWindowPlan()
    {
        var source = CreateLogicalScan("source:0", "source");
        var registration = new WindowRegistration(
            null,
            "RowNumber",
            [],
            [],
            [],
            0,
            typeof(long));
        return CreateProjection(new LogicalWindowNode([registration], source));
    }

    private static LogicalNode CreateSetOperationPlan()
    {
        var source = CreateLogicalScan("source:0", "source");
        var other = CreateLogicalScan("source:1", "other");
        return CreateProjection(new SetOperationNode(SetOpKind.UnionAll, source, other, ["Id"]));
    }

    private static LogicalNode CreateRecursiveCtePlan()
    {
        var source = CreateLogicalScan("source:0", "source");
        var recursiveMember = CreateLogicalScan("source:1", "other");
        return CreateProjection(new RecursiveCteNode(
            "items",
            source,
            recursiveMember,
            RecursiveCteUnionKind.All,
            ["Id"],
            [0]));
    }

    private static LogicalNode CreateUnpivotPlan()
    {
        var source = CreateLogicalScan("source:0", "source");
        var unpivot = new UnpivotNode(
            "unpivoted",
            "Name",
            "Value",
            [new UnpivotEntry("Id", new ColumnRef("source", "Id", typeof(int)))],
            [],
            source,
            source.OutputSchema);
        return CreateProjection(unpivot);
    }

    private static unsafe IEnumerable<object[]> UnsupportedQueryRowFieldTypes =>
    [
        new object[] { typeof(void) },
        new object[] { typeof(delegate*<void>) },
        new object[] { typeof(int).MakeByRefType() },
        new object[] { typeof(int).MakePointerType() },
        new object[] { typeof(Span<int>) },
        new object[] { typeof(List<>) },
        new object[] { typeof(PrivateFieldType) }
    ];

    private static SourcePlanningFacts CreateFacts(
        SourcePlanProperties source,
        SourceTransferCapabilities capabilities,
        ISchemaColumn[]? descriptorColumns = null)
    {
        var descriptor = new SourceDescriptor
        {
            Identity = new SourceIdentity("test", "rows", source.SourceContextId, source.Alias),
            Columns = descriptorColumns ?? source.ProjectedSchemaColumns,
            TransferCapabilities = capabilities
        };

        return new SourcePlanningFacts(
            new Dictionary<string, SourcePlanProperties>(StringComparer.Ordinal) { [source.SourceContextId] = source },
            new Dictionary<string, IrExpression[]>(StringComparer.Ordinal),
            new Dictionary<string, string[]>(StringComparer.Ordinal),
            new Dictionary<string, ISchemaColumn[]>(StringComparer.Ordinal),
            new Dictionary<string, SourcePredicatePlan>(StringComparer.Ordinal),
            new Dictionary<string, SourceInteractionPlan>(StringComparer.Ordinal),
            new Dictionary<string, SourcePlanRequest>(StringComparer.Ordinal),
            new Dictionary<string, SourcePlanResult>(StringComparer.Ordinal),
            new Dictionary<string, SourceDescriptor>(StringComparer.Ordinal) { [source.SourceContextId] = descriptor },
            [],
            [],
            new Dictionary<string, SourceContractDiagnosticLocationMap>(StringComparer.Ordinal));
    }

    private static PlanningContext CreateContext(
        SourceTransferCapabilities capabilities = SourceTransferCapabilities.QueryScopedRows,
        LogicalNode? logicalPlan = null)
    {
        logicalPlan ??= CreateProjection(CreateLogicalScan("source:0", "source"));
        var sourceNode = new ParserSchemaFromNode("test", "rows", ArgsListNode.Empty, "source", typeof(object), 0);

        return new PlanningContext(
            new LogicalPlanningArtifacts(logicalPlan, logicalPlan, new OptimizationTrace()),
            new CompilationOptions(),
            new ThrowingSchemaProvider(),
            new Dictionary<ParserSchemaFromNode, ISchemaColumn[]> { [sourceNode] = [] },
            new Dictionary<ParserSchemaFromNode, WhereNode>(),
            new Dictionary<ParserSchemaFromNode, SourcePlanRequest>(),
            new Dictionary<string, ISchemaColumn[]>(StringComparer.OrdinalIgnoreCase),
            new Dictionary<string, IReadOnlySet<string>>(StringComparer.OrdinalIgnoreCase),
            null,
            null,
            ExecutionPlanningShapeResolverAdapter.Create(),
            null,
            capabilities);
    }

    private sealed class ThrowingSchemaProvider : ISchemaProvider
    {
        public ISchema GetSchema(string schema) =>
            throw new NotSupportedException("Schema access is not expected in this planner unit test.");
    }

    private sealed class PrivateFieldType;

    private sealed record UnknownBoundaryNode(LogicalNode Input) : LogicalNode(Input.OutputSchema)
    {
        public override IReadOnlyList<LogicalNode> Children { get; } = [Input];
    }
}
