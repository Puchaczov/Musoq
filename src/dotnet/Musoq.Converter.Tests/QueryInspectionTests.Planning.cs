using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Diagnostics;

namespace Musoq.Converter.Tests;

public partial class QueryInspectionTests
{
    [TestMethod]
    public void CompileForInspection_WhenQueryIsValid_ShouldReturnLogicalPlanText()
    {
        var result = CreateInspection();

        AssertTextEquals(
            string.Join("\n",
                "MultiStatement",
                "  Project [d.Dummy as d.Dummy]",
                "    SchemaScan [#system.dual() as d]"),
            result.LogicalPlanText);
    }

    [TestMethod]
    public void CompileForInspection_WhenQueryIsValid_ShouldReturnPhysicalPlanText()
    {
        var result = CreateInspection();

        AssertTextEquals(
            string.Join("\n",
                "PhysicalMultiStatement",
                "  PhysicalProject [d.Dummy as d.Dummy]",
                "    PhysicalSchemaScan [#system.dual() as d]"),
            result.PhysicalPlanText);
    }

    [TestMethod]
    public void CompileForInspection_WhenQueryIsValid_ShouldReturnPlanningText()
    {
        var result = CreateInspection();

        Assert.Contains("Planning", result.PlanningText);
        Assert.Contains("PlannerBoundary", result.PlanningText);
        Assert.Contains("SourceProjection", result.PlanningText);
    }

    [TestMethod]
    public void CompileForInspection_WhenQueryOrdersRows_ShouldExplainOrderingMaterialization()
    {
        var result = Inspect("select d.Dummy from #system.dual() d order by d.Dummy");

        Assert.Contains("Materialization [OrderingBoundary] PhysicalSortNode -> Required", result.PlanningText);
        Assert.Contains("Sort materializes rows before applying", result.PlanningText);
    }

    [TestMethod]
    public void CompileForInspection_WhenQueryGroupsRows_ShouldExplainAggregateMaterialization()
    {
        var result = Inspect("select d.Dummy as Dummy, Count(1) as Count from #system.dual() d group by d.Dummy");

        Assert.Contains("Materialization [AggregateBoundary] PhysicalSingleKeyAggregateNode -> Required", result.PlanningText);
        Assert.Contains("Aggregate planning materializes group state before final projection.", result.PlanningText);
    }

    [TestMethod]
    public void CompileForInspection_WhenQueryUsesSetOperation_ShouldExplainSetMaterialization()
    {
        var result = Inspect("select d.Dummy as Dummy from #system.dual() d union all (Dummy) select e.Dummy as Dummy from #system.dual() e");

        Assert.Contains("Materialization [SetOperationBoundary] UnionAll -> Required", result.PlanningText);
        Assert.Contains("Set operation materializes row identity while combining input tables.", result.PlanningText);
        Assert.Contains("SetOperationStrategy [SetOperationStrategy] UnionAll -> StreamingUnionAll", result.PlanningText);
        Assert.Contains("Execution IR can append both arms directly into the result table.", result.PlanningText);
    }

    [TestMethod]
    public void CompileForInspection_WhenUnionAllUsesFilteredDirectSources_ShouldExplainStreamingStrategy()
    {
        var result = Inspect(CreateFilteredUnionAllQuery());

        Assert.Contains("SetOperationStrategy [SetOperationStrategy] UnionAll -> StreamingUnionAll", result.PlanningText);
        Assert.Contains("UnionAll arms use directly streamable row sources with optional filters", result.PlanningText);
    }

    [TestMethod]
    public void CompileForInspection_WhenQueryUsesWindow_ShouldExplainWindowMaterialization()
    {
        var result = Inspect("select d.Dummy as Dummy, RowNumber() over (order by d.Dummy) as RowNo from #system.dual() d");

        Assert.Contains("Materialization [WindowBoundary] PhysicalWindowNode -> Required", result.PlanningText);
        Assert.Contains("Window planning materializes input rows", result.PlanningText);
    }

    [TestMethod]
    public void CompileForInspection_WhenCteIsReused_ShouldExplainCteMaterialization()
    {
        var result = Inspect("with p as (select d.Dummy as Dummy from #system.dual() d), q as (select Dummy from p), r as (select Dummy from p) select q.Dummy, r.Dummy from q inner join r on q.Dummy = r.Dummy");

        Assert.Contains("Materialization [CteReuseBoundary] cte:p -> Required", result.PlanningText);
        Assert.Contains("CteStrategy [CteReuseStrategy] cte:p -> MaterializeReuse", result.PlanningText);
        Assert.Contains("reuse requires a materialized table", result.PlanningText);
    }

    [TestMethod]
    public void CompileForInspection_WhenInspectionIsRequested_ShouldReturnAllPlanArtifacts()
    {
        var result = CreateInspection();

        Assert.IsFalse(string.IsNullOrWhiteSpace(result.LogicalPlanText));
        Assert.IsFalse(string.IsNullOrWhiteSpace(result.PlanningText));
        Assert.IsFalse(string.IsNullOrWhiteSpace(result.PhysicalPlanText));
        Assert.IsFalse(string.IsNullOrWhiteSpace(result.ExecutionPlanText));
        Assert.IsFalse(string.IsNullOrWhiteSpace(result.GeneratedCSharpCode));
    }

    [TestMethod]
    public void CompileForInspection_WhenCompilationProducesWarning_ShouldExposeDiagnosticsAndWarnings()
    {
        var result = Inspect("select d.Dummy from #system.dual() d where true");

        Assert.IsTrue(result.Diagnostics.Any(static diagnostic => diagnostic.Code == DiagnosticCode.MQ5010_TautologicalCondition));
        Assert.IsTrue(result.Warnings.Any(static diagnostic => diagnostic.Code == DiagnosticCode.MQ5010_TautologicalCondition));
    }

    [TestMethod]
    public void CompileForInspection_WhenSourceShapeIsKnown_ShouldPruneProjectedColumns()
    {
        var result = Inspect("select i.Name from #apply.items() i", CreateApplyCandidateSchemaProvider());

        var scan = FindFirstSchemaScan(result.PhysicalPlan);

        CollectionAssert.AreEqual(new[] { "Name" }, scan.ProjectedColumns);
        Assert.Contains("projection: Name", result.PlanningText);
        Assert.Contains("ProjectionPruning [SourceProjection]", result.PlanningText);
        Assert.Contains("Applied", result.PlanningText);
    }

    [TestMethod]
    public void CompileForInspection_WhenSourceShapeIsDynamic_ShouldNotPruneProjectedColumns()
    {
        var result = Inspect("select d.Name from #dynamic.all() d", CreateDynamicRowsSchemaProvider());

        var scan = FindFirstSchemaScan(result.PhysicalPlan);

        Assert.IsEmpty(scan.ProjectedColumns);
        Assert.Contains("Source entity type IReadOnlyDictionary", result.PlanningText);
        Assert.Contains("Skipped", result.PlanningText);
    }

    [TestMethod]
    public void CompileForInspection_WhenSourceInteractionIsKnown_ShouldExplainRuntimeContract()
    {
        var result = CreateApplyInspection(
            "select i.Name from #apply.items() i where i.Line = 'INFO ready'");

        Assert.Contains("interaction shape: KnownClr", result.PlanningText);
        Assert.Contains("interaction columns: ProjectedColumns [Name, Line]", result.PlanningText);
        Assert.Contains("interaction predicate: PushedSourcePredicate", result.PlanningText);
        Assert.Contains("interaction arguments: ConstantArguments", result.PlanningText);
    }

    [TestMethod]
    public void CompileForInspection_WhenSourceInteractionIsDynamic_ShouldExplainDynamicShape()
    {
        var result = Inspect("select d.Name from #dynamic.all() d", CreateDynamicRowsSchemaProvider());

        Assert.Contains("interaction shape: Dynamic", result.PlanningText);
        Assert.Contains("Source entity type IReadOnlyDictionary", result.PlanningText);
    }

    [TestMethod]
    public void CompileForInspection_WhenSourceInteractionHasPlanRequest_ShouldExplainSourceRequest()
    {
        var result = CreateApplyInspection(
            "select i.Name from #apply.items() i skip 1 take 1");

        Assert.Contains("interaction source request: orderBy=0, skip=1, take=1", result.PlanningText);
        Assert.Contains("SourceInteraction [SourceInteractionPlan]", result.PlanningText);
    }

    [TestMethod]
    public void CompileForInspection_WhenWherePredicateIsSourceLocal_ShouldExplainSourcePushdownPlacement()
    {
        var result = CreateApplyInspection(
            "select i.Name from #apply.items() i where i.Line = 'INFO ready'");

        Assert.Contains("placement: Where -> SourcePushdown", result.PlanningText);
        Assert.Contains("PredicatePlacement [PredicatePlacementPlan]", result.PlanningText);
    }

    [TestMethod]
    public void CompileForInspection_WhenWherePredicateReferencesJoinedSources_ShouldExplainPostJoinPlacement()
    {
        var result = CreateApplyInspection(
            "select l.Name from #apply.items() l inner join #apply.items() r on l.Name = r.Name where l.Line = r.Line");

        Assert.Contains("placement: Where -> PostJoin", result.PlanningText);
        Assert.Contains("aliases: l, r", result.PlanningText);
    }

    [TestMethod]
    public void CompileForInspection_WhenInnerJoinOnPredicateIsSideLocal_ShouldExplainPreInnerJoinSidePlacement()
    {
        var result = CreateApplyInspection(
            "select l.Name from #apply.items() l inner join #apply.items() r on l.Line = 'INFO ready' and r.Line = 'WARN retry'");

        Assert.Contains("placement: JoinOn -> PreInnerJoinLeft", result.PlanningText);
        Assert.Contains("placement: JoinOn -> PreInnerJoinRight", result.PlanningText);
    }

    [TestMethod]
    public void CompileForInspection_WhenOuterJoinPredicateCouldChangePreservedRows_ShouldExplainPostJoinPlacement()
    {
        var result = CreateApplyInspection(
            "select l.Name from #apply.items() l left outer join #apply.items() r on l.Name = r.Name where r.Line = 'INFO ready'");

        Assert.Contains("placement: Where -> PostJoin", result.PlanningText);
        Assert.Contains("preserve outer join row semantics", result.PlanningText);
    }

    [TestMethod]
    public void CompileForInspection_WhenHavingPredicateExists_ShouldExplainHavingPlacement()
    {
        var result = CreateApplyInspection(
            "select i.Name, Count(i.Line) as Count from #apply.items() i group by i.Name having Count(i.Line) > 0");

        Assert.Contains("placement: Having -> PostAggregate", result.PlanningText);
    }

    [TestMethod]
    public void CompileForInspection_WhenQualifyPredicateExists_ShouldExplainQualifyPlacement()
    {
        var result = CreateApplyInspection(
            "select i.Name from #apply.items() i qualify RowNumber() over (order by i.Name) <= 1");

        Assert.Contains("placement: Qualify -> PostWindow", result.PlanningText);
    }

    [TestMethod]
    public void CompileForInspection_WhenWhereUsesNonProjectedColumn_ShouldExplainRequiredColumnUsage()
    {
        var result = CreateApplyInspection(
            "select i.Name from #apply.items() i where i.Line = 'INFO ready'");

        var scan = FindSchemaScanByAlias(result.PhysicalPlan, "i");

        AssertProjectedColumnsInclude(scan, "Name", "Line");
        Assert.Contains("usage: Name <- projection (High)", result.PlanningText);
        Assert.Contains("usage: Line <- where (High)", result.PlanningText);
        Assert.Contains("RequiredColumnMappings", result.PlanningText);
        Assert.Contains("alias: i required: Line, Name retained: Line, Name blocked: none origins: i.Line->Line, i.Name->Name", result.PlanningText);
        Assert.Contains("RequiredColumns [RequiredColumnUsage]", result.PlanningText);
        Assert.Contains("RequiredColumns [RequiredColumnMapping]", result.PlanningText);
    }

    [TestMethod]
    public void CompileForInspection_WhenJoinUsesNonProjectedColumns_ShouldExplainJoinPredicateUsage()
    {
        var result = CreateApplyInspection(
            "select l.Name from #apply.items() l inner join #apply.items() r on l.Line = r.Line");

        var leftScan = FindSchemaScanByAlias(result.PhysicalPlan, "l");
        var rightScan = FindSchemaScanByAlias(result.PhysicalPlan, "r");

        AssertProjectedColumnsInclude(leftScan, "Name", "Line");
        AssertProjectedColumnsInclude(rightScan, "Line");
        Assert.Contains("usage: Line <- join predicate (High)", result.PlanningText);
    }

    [TestMethod]
    public void CompileForInspection_WhenOrderByUsesNonProjectedColumn_ShouldExplainOrderByUsage()
    {
        var result = CreateApplyInspection(
            "select i.Name from #apply.items() i order by i.Line");

        var scan = FindSchemaScanByAlias(result.PhysicalPlan, "i");

        AssertProjectedColumnsInclude(scan, "Name", "Line");
        Assert.Contains("usage: Line <- order by (High)", result.PlanningText);
    }

    [TestMethod]
    public void CompileForInspection_WhenOrderByUsesNonProjectedColumn_ShouldApplySortRowWidthPruning()
    {
        var result = CreateApplyInspection(
            "select i.Name from #apply.items() i order by i.Line");

        Assert.Contains("BoundaryRowShapes", result.PlanningText);
        Assert.Contains("row-shape: sort:0 Sort", result.PlanningText);
        Assert.Contains("semantic: i.Name", result.PlanningText);
        Assert.Contains("retained: i.Name", result.PlanningText);
        Assert.Contains("boundary-only: i.Line", result.PlanningText);
        Assert.Contains("candidates: i.Line", result.PlanningText);
        Assert.Contains("blocked: none", result.PlanningText);
        Assert.Contains("droppable-later: i.Line", result.PlanningText);
        Assert.Contains("RowWidthPruning", result.PlanningText);
        Assert.Contains("pruning: sort:0 Sort -> Applied", result.PlanningText);
        Assert.Contains("pruned: i.Line", result.PlanningText);
        Assert.Contains("RowWidthPruning [RowWidthPruningPlan] sort:0 -> Applied", result.PlanningText);
        Assert.DoesNotContain("BoundaryRowShapePlan", result.GeneratedCSharpCode);
        Assert.DoesNotContain("RowWidthPruningPlan", result.GeneratedCSharpCode);
    }

    [TestMethod]
    public void CompileForInspection_WhenJoinKeyIsNotProjected_ShouldDiagnoseBuildSideJoinOnlyColumn()
    {
        var result = CreateApplyInspection(
            "select l.Name, r.Name as RightName from #apply.items() l inner join #apply.items() r on l.Line = r.Line");

        Assert.Contains("row-shape: hash-join-build:0 HashJoinBuild", result.PlanningText);
        Assert.Contains("boundary-only: r.Line", result.PlanningText);
        Assert.Contains("droppable-later: r.Line", result.PlanningText);
        Assert.Contains("Hash join build boundary uses build-key columns", result.PlanningText);
        Assert.DoesNotContain("BoundaryRowShapePlan", result.GeneratedCSharpCode);
    }

    [TestMethod]
    public void CompileForInspection_WhenGroupingUsesColumns_ShouldExplainGroupAggregateAndHavingUsage()
    {
        var result = CreateApplyInspection(
            "select i.Name, Count(i.Line) from #apply.items() i group by i.Name having i.Name != ''");

        Assert.Contains("usage: Name <- group by (High)", result.PlanningText);
        Assert.Contains("usage: Name <- having (High)", result.PlanningText);
        Assert.Contains("usage: Line <- aggregate set argument (High)", result.PlanningText);
    }

    [TestMethod]
    public void CompileForInspection_WhenGroupingUsesAggregateInput_ShouldDiagnoseInputAndOutputRowShapes()
    {
        var result = CreateApplyInspection(
            "select i.Name, Count(i.Line) as Count from #apply.items() i group by i.Name");

        Assert.Contains("row-shape: aggregate:0 Aggregate", result.PlanningText);
        Assert.Contains("input: i.Line, i.Name", result.PlanningText);
        Assert.Contains("boundary-only: i.Line", result.PlanningText);
        Assert.Contains("droppable-later: i.Line", result.PlanningText);
        Assert.Contains("Aggregate boundary separates input columns from aggregate output columns", result.PlanningText);
        Assert.Contains("pruning: aggregate:0 Aggregate -> Applied", result.PlanningText);
        Assert.Contains("Aggregate row-width pruning drops aggregate input-only columns", result.PlanningText);
        Assert.DoesNotContain("BoundaryRowShapePlan", result.GeneratedCSharpCode);
        Assert.DoesNotContain("RowWidthPruningPlan", result.GeneratedCSharpCode);
    }

    [TestMethod]
    public void CompileForInspection_WhenWindowUsesColumns_ShouldExplainWindowPartitionAndOrderUsage()
    {
        var result = CreateApplyInspection(
            "select i.Line, RowNumber() over (partition by i.Line order by i.Name) as RowNo from #apply.items() i");

        var scan = FindSchemaScanByAlias(result.PhysicalPlan, "i");

        AssertProjectedColumnsInclude(scan, "Name", "Line");
        Assert.Contains("usage: Line <- window partition (High)", result.PlanningText);
        Assert.Contains("usage: Name <- window order (High)", result.PlanningText);
    }

    [TestMethod]
    public void CompileForInspection_WhenWindowUsesPartitionOrderAndValueColumns_ShouldDiagnoseWindowBoundaryColumns()
    {
        var result = CreateApplyInspection(
            "select i.Name, Lag(i.Line, 1) over (partition by i.Name order by i.Line) as PreviousLine from #apply.items() i");

        Assert.Contains("row-shape: window:0 Window", result.PlanningText);
        Assert.Contains("input: i.Line, i.Name", result.PlanningText);
        Assert.Contains("boundary-only: i.Line", result.PlanningText);
        Assert.Contains("droppable-later: i.Line", result.PlanningText);
        Assert.Contains("Window boundary uses partition/order/value columns", result.PlanningText);
        Assert.DoesNotContain("BoundaryRowShapePlan", result.GeneratedCSharpCode);
    }

    [TestMethod]
    public void CompileForInspection_WhenQualifyUsesColumn_ShouldExplainQualifyUsage()
    {
        var result = CreateApplyInspection(
            "select i.Name from #apply.items() i qualify RowNumber() over (order by i.Name) <= 1 and i.Line != ''");

        var scan = FindSchemaScanByAlias(result.PhysicalPlan, "i");

        AssertProjectedColumnsInclude(scan, "Name", "Line");
        Assert.Contains("usage: Line <- qualify (High)", result.PlanningText);
    }

    [TestMethod]
    public void CompileForInspection_WhenSetOperationUsesKey_ShouldExplainSetOperationKeyUsage()
    {
        var result = CreateApplyInspection(
            "select i.Name as Key from #apply.items() i union (Key) select j.Line as Key from #apply.items() j");

        var leftScan = FindSchemaScanByAlias(result.PhysicalPlan, "i");
        var rightScan = FindSchemaScanByAlias(result.PhysicalPlan, "j");

        AssertProjectedColumnsInclude(leftScan, "Name");
        AssertProjectedColumnsInclude(rightScan, "Line");
        Assert.Contains("usage: Name <- set-operation key (High)", result.PlanningText);
        Assert.Contains("usage: Line <- set-operation key (High)", result.PlanningText);
    }

    [TestMethod]
    public void CompileForInspection_WhenSourceArgumentUsesColumn_ShouldExplainSourceArgumentUsage()
    {
        var result = CreateApplyInspection(
            "select r.Line from #apply.items() i cross apply #apply.related(i.Name) r");

        var leftScan = FindSchemaScanByAlias(result.PhysicalPlan, "i");
        var rightScan = FindSchemaScanByAlias(result.PhysicalPlan, "r");

        AssertProjectedColumnsInclude(leftScan, "Name");
        AssertProjectedColumnsInclude(rightScan, "Line");
        Assert.Contains("usage: Name <- source argument (High)", result.PlanningText);
    }

    [TestMethod]
    public void CompileForInspection_WhenPropertyApplyUsesColumn_ShouldExplainApplyCorrelationUsage()
    {
        var result = CreateApplyInspection(
            "select n.Value from #apply.items() i cross apply i.Numbers n");

        var scan = FindSchemaScanByAlias(result.PhysicalPlan, "i");

        AssertProjectedColumnsInclude(scan, "Numbers");
        Assert.Contains("usage: Numbers <- apply correlation (High)", result.PlanningText);
    }

    [TestMethod]
    public void CompileForInspection_WhenSchemaApplyUsesOuterArgument_ShouldExplainApplyBoundary()
    {
        var result = CreateApplyInspection(
            "select r.Line from #apply.items() i cross apply #apply.related(i.Name) r");

        Assert.Contains("boundary: apply:0 Apply Cross Correlated target: i -> r inputs: i outputs: r call: PerRow rows: RowMultiplying result: Declared cache: NotCacheable (High)", result.PlanningText);
        Assert.Contains("strategy: apply:0 Apply Cross Correlated -> PerRowRequired cache: NotApplied (High)", result.PlanningText);
        Assert.Contains("SourceInteraction [SourceBoundaryPlan] apply:0 -> Apply/Cross/Correlated", result.PlanningText);
        Assert.Contains("SourceBoundaryStrategy [SourceBoundaryStrategyPlan] apply:0 -> PerRowRequired", result.PlanningText);
    }

    [TestMethod]
    public void CompileForInspection_WhenInterpretApplyUsesOuterColumn_ShouldExplainInterpretBoundary()
    {
        var result = CreateApplyInspection(
            @"
                text LogLine {
                    Level: until ' ',
                    Message: rest
                };
                select l.Level from #apply.items() i cross apply Parse<LogLine>(i.Line) l");

        Assert.Contains("boundary: interpret:l InterpretSource Cross Correlated target: LogLine.Parse as l inputs: i outputs: l call: PerRow rows: RowMultiplying result: Declared cache: NotCacheable (High)", result.PlanningText);
        Assert.Contains("strategy: interpret:l InterpretSource Cross Correlated -> PerRowRequired cache: NotApplied (High)", result.PlanningText);
        Assert.Contains("Parse source l reads argument alias(es): i", result.PlanningText);
        Assert.Contains("interpretation caching/pruning is not applied", result.PlanningText);
    }

    [TestMethod]
    public void CompileForInspection_WhenInterpretApplyUsesConstants_ShouldExplainPerQueryCandidateWithoutCaching()
    {
        var result = CreateApplyInspection(
            @"
                text LogLine {
                    Level: until ' ',
                    Message: rest
                };
                select l.Level from #apply.items() i cross apply Parse<LogLine>('INFO ready') l");

        Assert.Contains("boundary: interpret:l InterpretSource Cross Independent target: LogLine.Parse as l inputs: none outputs: l call: PerQuery rows: RowMultiplying result: Declared cache: CacheCandidate (Medium)", result.PlanningText);
        Assert.Contains("strategy: interpret:l InterpretSource Cross Independent -> PerQueryCandidateNotApplied cache: NotApplied (Medium)", result.PlanningText);
        Assert.Contains("source/plugin caching is not applied without a capability design", result.PlanningText);
    }

    [TestMethod]
    public void CompileForInspection_WhenOuterPropertyApplyUsesColumn_ShouldExplainPropertyBoundary()
    {
        var result = CreateApplyInspection(
            "select n.Value from #apply.items() i outer apply i.Numbers n");

        Assert.Contains("boundary: property:n PropertySource Outer Correlated target: i.Numbers as n inputs: i outputs: n call: PerRow rows: RowPreserving result: Declared cache: NotCacheable (High)", result.PlanningText);
        Assert.Contains("strategy: property:n PropertySource Outer Correlated -> PerRowRequired cache: NotApplied (High)", result.PlanningText);
        Assert.Contains("Property source n expands i.Numbers with Outer APPLY semantics.", result.PlanningText);
    }

    [TestMethod]
    public void CompileForInspection_WhenAccessMethodApplyUsesOuterColumn_ShouldExplainAccessMethodBoundary()
    {
        var result = CreateApplyInspection(
            "select s.Value from #apply.items() i cross apply i.Split(i.Line, ' ') s");

        Assert.Contains("boundary: access:s AccessMethodSource Cross Correlated", result.PlanningText);
        Assert.Contains("strategy: access:s AccessMethodSource Cross Correlated -> PerRowRequired cache: NotApplied (High)", result.PlanningText);
        Assert.Contains("call: PerRow rows: RowMultiplying result: Declared cache: NotCacheable (High)", result.PlanningText);
        Assert.Contains("Access method source s evaluates", result.PlanningText);
    }

    [TestMethod]
    public void CompileForInspection_WhenSourceAwarePlanningCrossesApplyBoundary_ShouldExposeDiagnosticsWithoutLeakingPlannerRecords()
    {
        var result = CreateApplyInspection(
            "select r.Line from #apply.items() i cross apply #apply.related(i.Name) r");

        Assert.Contains("usage: Name <- source argument (High)", result.PlanningText);
        Assert.Contains("interaction arguments: CorrelatedArguments", result.PlanningText);
        Assert.Contains("Source arguments reference outer alias(es): i.", result.PlanningText);
        Assert.Contains("boundary: apply:0 Apply Cross Correlated target: i -> r inputs: i outputs: r call: PerRow rows: RowMultiplying result: Declared cache: NotCacheable (High)", result.PlanningText);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("SourceBoundaryPlan", StringComparison.Ordinal));
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("SourceBoundaryStrategyPlan", StringComparison.Ordinal));
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("SourceInteractionPlan", StringComparison.Ordinal));
    }

    [TestMethod]
    public void CompileForInspection_WhenPlannerStrategyFactsArePresent_ShouldKeepPlanningTextSectionsVisible()
    {
        var predicateMovement = CreateApplyInspection(
            "select l.Name from #apply.items() l inner join #apply.items() r on l.Name = r.Name where l.Line = 'INFO ready'");
        var sourceBoundary = CreateApplyInspection(
            "select r.Line from #apply.items() i cross apply #apply.related(i.Name) r");
        var boundaryRowShape = CreateApplyInspection(
            "select i.Name from #apply.items() i order by i.Line");
        var setOperation = Inspect("select d.Dummy as Dummy from #system.dual() d union all (Dummy) select e.Dummy as Dummy from #system.dual() e");
        var cte = Inspect("with p as (select d.Dummy as Dummy from #system.dual() d) select p.Dummy from p");

        Assert.Contains("PredicateMovements", predicateMovement.PlanningText);
        Assert.Contains("PredicateMovement [PredicateMovementPlan]", predicateMovement.PlanningText);
        Assert.Contains("SourceBoundaryStrategies", sourceBoundary.PlanningText);
        Assert.Contains("SourceBoundaryStrategy [SourceBoundaryStrategyPlan]", sourceBoundary.PlanningText);
        Assert.Contains("BoundaryRowShapes", boundaryRowShape.PlanningText);
        Assert.Contains("BoundaryRowShape [BoundaryRowShapePlan]", boundaryRowShape.PlanningText);
        Assert.Contains("RowWidthPruning", boundaryRowShape.PlanningText);
        Assert.Contains("RowWidthPruning [RowWidthPruningPlan]", boundaryRowShape.PlanningText);
        Assert.Contains("SetOperationStrategy [SetOperationStrategy]", setOperation.PlanningText);
        Assert.Contains("CteStrategy [CteReuseStrategy]", cte.PlanningText);
    }

}
