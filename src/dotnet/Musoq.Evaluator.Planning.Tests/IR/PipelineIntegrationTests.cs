using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Logical;
using Musoq.Evaluator.IR.Logical.Nodes;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;
using Musoq.Evaluator.Tests.Schema.Basic;
using PhysicalPlanPrinter = Musoq.Evaluator.IR.Physical.PhysicalPlanPrinter;

namespace Musoq.Evaluator.Tests.IR;

[TestClass]
public class PipelineIntegrationTests : BasicEntityTestBase
{
    private static LogicalNode RequireLogicalPlan(Musoq.Converter.Build.BuildItems buildItems)
    {
        return buildItems.LogicalPlan ?? throw new AssertFailedException("Expected a logical plan.");
    }

    private static PhysicalNode RequirePhysicalPlan(Musoq.Converter.Build.BuildItems buildItems)
    {
        return buildItems.PhysicalPlan ?? throw new AssertFailedException("Expected a physical plan.");
    }

    private static LogicalNode UnwrapMultiStatement(LogicalNode node)
    {
        if (node is MultiStatementNode { Statements.Length: 1 } multi)
            return multi.Statements[0];
        return node;
    }

    private static PhysicalNode UnwrapPhysicalMultiStatement(PhysicalNode node)
    {
        if (node is PhysicalMultiStatementNode { Statements.Length: 1 } multi)
            return multi.Statements[0];
        return node;
    }

    private static LogicalNode GetFinalLogicalStatement(LogicalNode node)
    {
        if (node is MultiStatementNode multi)
            return multi.Statements[^1];

        return node;
    }

    private static PhysicalNode GetFinalPhysicalStatement(PhysicalNode node)
    {
        if (node is PhysicalMultiStatementNode multi)
            return multi.Statements[^1];

        return node;
    }

    [TestMethod]
    public void WhenSimpleSelect_ShouldProduceBothPlans()
    {
        var buildItems = CreateBuildItems<BasicEntity>("select Name from #A.Entities()");

        Assert.IsNotNull(RequireLogicalPlan(buildItems));
        Assert.IsNotNull(RequirePhysicalPlan(buildItems));
        Assert.IsNotNull(buildItems.Compilation);
    }

    [TestMethod]
    public void WhenSimpleSelect_LogicalPlanShouldHaveProjectOverSchemaScan()
    {
        var buildItems = CreateBuildItems<BasicEntity>("select Name from #A.Entities()");

        var logical = UnwrapMultiStatement(RequireLogicalPlan(buildItems));

        Assert.IsInstanceOfType<ProjectNode>(logical);
        var project = (ProjectNode)logical;
        Assert.HasCount(1, project.Fields);
        Assert.AreEqual("Name", project.Fields[0].OutputName);
        Assert.IsInstanceOfType<SchemaScanNode>(project.Input);
    }

    [TestMethod]
    public void WhenSimpleSelect_PhysicalPlanShouldHavePhysicalProjectOverPhysicalSchemaScan()
    {
        var buildItems = CreateBuildItems<BasicEntity>("select Name from #A.Entities()");

        var physical = UnwrapPhysicalMultiStatement(RequirePhysicalPlan(buildItems));

        Assert.IsInstanceOfType<PhysicalProjectNode>(physical);
        var project = (PhysicalProjectNode)physical;
        Assert.IsInstanceOfType<PhysicalSchemaScanNode>(project.Input);
    }

    [TestMethod]
    public void WhenSelectWithWhere_ShouldProduceFilterInBothPlans()
    {
        var buildItems = CreateBuildItems<BasicEntity>("select Name from #A.Entities() where Population > 100");

        var logical = UnwrapMultiStatement(RequireLogicalPlan(buildItems));
        Assert.IsInstanceOfType<ProjectNode>(logical);
        var project = (ProjectNode)logical;
        Assert.IsInstanceOfType<FilterNode>(project.Input);

        var physical = UnwrapPhysicalMultiStatement(RequirePhysicalPlan(buildItems));
        Assert.IsInstanceOfType<PhysicalProjectNode>(physical);
        var physProject = (PhysicalProjectNode)physical;
        Assert.IsInstanceOfType<PhysicalFilterNode>(physProject.Input);
    }

    [TestMethod]
    public void WhenAsofJoinUsed_ShouldProduceAsofJoinInBothPlans()
    {
        var buildItems = CreateBuildItems<BasicEntity>(
            "select a.Name, b.Name from #A.Entities() a asof join #B.Entities() b on a.Country = b.Country and a.Population >= b.Population");

        var logicalPrinted = LogicalPlanPrinter.Print(RequireLogicalPlan(buildItems));
        StringAssert.Contains(logicalPrinted, "Join [AsofInner]");

        var logical = GetFinalLogicalStatement(RequireLogicalPlan(buildItems));
        Assert.IsInstanceOfType<ProjectNode>(logical);
        var logicalProject = (ProjectNode)logical;
        Assert.IsInstanceOfType<CteRefNode>(logicalProject.Input);

        var physicalPrinted = PhysicalPlanPrinter.Print(RequirePhysicalPlan(buildItems));
        StringAssert.Contains(physicalPrinted, "PhysicalNestedLoopJoin [AsofInner]");

        var physical = GetFinalPhysicalStatement(RequirePhysicalPlan(buildItems));
        Assert.IsInstanceOfType<PhysicalProjectNode>(physical);
        var physicalProject = (PhysicalProjectNode)physical;
        Assert.IsInstanceOfType<PhysicalCteRefNode>(physicalProject.Input);
    }

    [TestMethod]
    public void WhenAsofLeftJoinUsed_ShouldProduceAsofLeftJoinInBothPlans()
    {
        var buildItems = CreateBuildItems<BasicEntity>(
            "select a.Name, b.Name from #A.Entities() a asof left join #B.Entities() b on a.Population >= b.Population");

        var logicalPrinted = LogicalPlanPrinter.Print(RequireLogicalPlan(buildItems));
        StringAssert.Contains(logicalPrinted, "Join [AsofLeft]");

        var logical = GetFinalLogicalStatement(RequireLogicalPlan(buildItems));
        Assert.IsInstanceOfType<ProjectNode>(logical);
        var logicalProject = (ProjectNode)logical;
        Assert.IsInstanceOfType<CteRefNode>(logicalProject.Input);

        var physicalPrinted = PhysicalPlanPrinter.Print(RequirePhysicalPlan(buildItems));
        StringAssert.Contains(physicalPrinted, "PhysicalNestedLoopJoin [AsofLeft]");

        var physical = GetFinalPhysicalStatement(RequirePhysicalPlan(buildItems));
        Assert.IsInstanceOfType<PhysicalProjectNode>(physical);
        var physicalProject = (PhysicalProjectNode)physical;
        Assert.IsInstanceOfType<PhysicalCteRefNode>(physicalProject.Input);
    }

    [TestMethod]
    public void WhenRightOuterEquiJoinUsed_ShouldProduceHashJoinInPhysicalPlan()
    {
        var buildItems = CreateBuildItems<BasicEntity>(
            "select a.Name, b.Name from #A.Entities() a right outer join #B.Entities() b on a.Id = b.Id");

        var logicalPrinted = LogicalPlanPrinter.Print(RequireLogicalPlan(buildItems));
        StringAssert.Contains(logicalPrinted, "Join [RightOuter]");

        var logical = GetFinalLogicalStatement(RequireLogicalPlan(buildItems));
        Assert.IsInstanceOfType<ProjectNode>(logical);
        var logicalProject = (ProjectNode)logical;
        Assert.IsInstanceOfType<CteRefNode>(logicalProject.Input);

        var physicalPrinted = PhysicalPlanPrinter.Print(RequirePhysicalPlan(buildItems));
        StringAssert.Contains(physicalPrinted, "PhysicalHashJoin [RightOuter]");

        var physical = GetFinalPhysicalStatement(RequirePhysicalPlan(buildItems));
        Assert.IsInstanceOfType<PhysicalProjectNode>(physical);
        var physicalProject = (PhysicalProjectNode)physical;
        Assert.IsInstanceOfType<PhysicalCteRefNode>(physicalProject.Input);
    }

    [TestMethod]
    public void WhenRightOuterNonEquiJoinUsed_ShouldProduceNestedLoopJoinInPhysicalPlan()
    {
        var buildItems = CreateBuildItems<BasicEntity>(
            "select a.Name, b.Name from #A.Entities() a right outer join #B.Entities() b on a.Population > b.Population");

        var logicalPrinted = LogicalPlanPrinter.Print(RequireLogicalPlan(buildItems));
        StringAssert.Contains(logicalPrinted, "Join [RightOuter]");

        var logical = GetFinalLogicalStatement(RequireLogicalPlan(buildItems));
        Assert.IsInstanceOfType<ProjectNode>(logical);
        var logicalProject = (ProjectNode)logical;
        Assert.IsInstanceOfType<CteRefNode>(logicalProject.Input);

        var physicalPrinted = PhysicalPlanPrinter.Print(RequirePhysicalPlan(buildItems));
        StringAssert.Contains(physicalPrinted, "PhysicalNestedLoopJoin [RightOuter]");

        var physical = GetFinalPhysicalStatement(RequirePhysicalPlan(buildItems));
        Assert.IsInstanceOfType<PhysicalProjectNode>(physical);
        var physicalProject = (PhysicalProjectNode)physical;
        Assert.IsInstanceOfType<PhysicalCteRefNode>(physicalProject.Input);
    }

    [TestMethod]
    public void WhenFullOuterJoinUsed_ShouldProduceFullOuterJoinInBothPlans()
    {
        var buildItems = CreateBuildItems<BasicEntity>(
            "select a.Name, b.Name from #A.Entities() a full outer join #B.Entities() b on a.Id = b.Id");

        var logicalPrinted = LogicalPlanPrinter.Print(RequireLogicalPlan(buildItems));
        StringAssert.Contains(logicalPrinted, "Join [FullOuter]");

        var logical = GetFinalLogicalStatement(RequireLogicalPlan(buildItems));
        Assert.IsInstanceOfType<ProjectNode>(logical);
        var logicalProject = (ProjectNode)logical;
        Assert.IsInstanceOfType<CteRefNode>(logicalProject.Input);

        var physicalPrinted = PhysicalPlanPrinter.Print(RequirePhysicalPlan(buildItems));
        StringAssert.Contains(physicalPrinted, "PhysicalHashJoin [FullOuter]");

        var physical = GetFinalPhysicalStatement(RequirePhysicalPlan(buildItems));
        Assert.IsInstanceOfType<PhysicalProjectNode>(physical);
        var physicalProject = (PhysicalProjectNode)physical;
        Assert.IsInstanceOfType<PhysicalCteRefNode>(physicalProject.Input);
    }

    [TestMethod]
    public void WhenRightOuterJoinBetweenCtesUsed_ShouldProduceHashJoinAndFinalCteProjection()
    {
        var buildItems = CreateBuildItems<BasicEntity>(@"
            with cteA as (
                select Name, Population from #A.entities()
            ),
            cteB as (
                select Name, Population from #B.entities()
            )
            select a.Name, b.Name
            from cteA a
            right outer join cteB b on a.Population = b.Population");

        Assert.IsNotNull(buildItems.Compilation);
        Assert.IsInstanceOfType<CteNode>(RequireLogicalPlan(buildItems));

        var logicalPrinted = LogicalPlanPrinter.Print(RequireLogicalPlan(buildItems));
        StringAssert.Contains(logicalPrinted, "Join [RightOuter]");
        StringAssert.Contains(logicalPrinted, "CteRef");

        var logicalCte = (CteNode)RequireLogicalPlan(buildItems);
        Assert.HasCount(2, logicalCte.Definitions);

        var logicalOuterQuery = GetFinalLogicalStatement(logicalCte.Query);
        Assert.IsInstanceOfType<ProjectNode>(logicalOuterQuery);
        var logicalOuterProject = (ProjectNode)logicalOuterQuery;
        Assert.IsInstanceOfType<CteRefNode>(logicalOuterProject.Input);

        Assert.IsInstanceOfType<PhysicalCteNode>(RequirePhysicalPlan(buildItems));

        var physicalPrinted = PhysicalPlanPrinter.Print(RequirePhysicalPlan(buildItems));
        StringAssert.Contains(physicalPrinted, "PhysicalHashJoin [RightOuter]");
        StringAssert.Contains(physicalPrinted, "PhysicalCteRef");

        var physicalCte = (PhysicalCteNode)RequirePhysicalPlan(buildItems);
        Assert.HasCount(2, physicalCte.Definitions);

        var physicalOuterQuery = GetFinalPhysicalStatement(physicalCte.Query);
        Assert.IsInstanceOfType<PhysicalProjectNode>(physicalOuterQuery);
        var physicalOuterProject = (PhysicalProjectNode)physicalOuterQuery;
        Assert.IsInstanceOfType<PhysicalCteRefNode>(physicalOuterProject.Input);
    }

    [TestMethod]
    public void WhenGroupBy_ShouldProducePlans()
    {
        var buildItems = CreateBuildItems<BasicEntity>(
            "select City, Count(City) from #A.Entities() group by City");

        Assert.IsNotNull(RequireLogicalPlan(buildItems));
        Assert.IsNotNull(RequirePhysicalPlan(buildItems));
        Assert.IsNotNull(buildItems.Compilation);

        var logicalPrinted = LogicalPlanPrinter.Print(RequireLogicalPlan(buildItems));
        var physicalPrinted = PhysicalPlanPrinter.Print(RequirePhysicalPlan(buildItems));

        StringAssert.Contains(logicalPrinted, "Aggregate");
        StringAssert.Contains(physicalPrinted, "Aggregate");
    }

    [TestMethod]
    public void WhenOrderBy_ShouldProduceSortNode()
    {
        var buildItems = CreateBuildItems<BasicEntity>(
            "select Name from #A.Entities() order by Name");

        var logical = UnwrapMultiStatement(RequireLogicalPlan(buildItems));
        Assert.IsInstanceOfType<SortNode>(logical);
        var sort = (SortNode)logical;
        Assert.IsInstanceOfType<ProjectNode>(sort.Input);

        var physical = UnwrapPhysicalMultiStatement(RequirePhysicalPlan(buildItems));
        Assert.IsInstanceOfType<PhysicalSortNode>(physical);
    }

    [TestMethod]
    public void WhenOrderByTakeWithoutSkip_ShouldProduceTopNNode()
    {
        var buildItems = CreateBuildItems<BasicEntity>(
            "select Name from #A.Entities() order by Name take 2");

        var logical = UnwrapMultiStatement(RequireLogicalPlan(buildItems));
        Assert.IsInstanceOfType<TakeNode>(logical);
        var take = (TakeNode)logical;
        Assert.IsInstanceOfType<SortNode>(take.Input);

        var physical = UnwrapPhysicalMultiStatement(RequirePhysicalPlan(buildItems));
        Assert.IsInstanceOfType<PhysicalTopNNode>(physical);
    }

    [TestMethod]
    public void WhenOrderBySkipTake_ShouldProduceTopOffsetNode()
    {
        var buildItems = CreateBuildItems<BasicEntity>(
            "select Name from #A.Entities() order by Name skip 5 take 10");

        var logical = UnwrapMultiStatement(RequireLogicalPlan(buildItems));
        Assert.IsInstanceOfType<TakeNode>(logical);
        var take = (TakeNode)logical;
        Assert.IsInstanceOfType<SkipNode>(take.Input);

        var physical = UnwrapPhysicalMultiStatement(RequirePhysicalPlan(buildItems));
        Assert.IsInstanceOfType<PhysicalTopOffsetNode>(physical);
        var topOffset = (PhysicalTopOffsetNode)physical;
        Assert.AreEqual(5, topOffset.Skip);
        Assert.AreEqual(10, topOffset.Take);
    }

    [TestMethod]
    public void WhenWindowFunctionUsed_ShouldProduceWindowInBothPlans()
    {
        var buildItems = CreateBuildItems<BasicEntity>(
            "select Name, RowNumber() over (order by Name) as RowNum from #A.Entities()");

        var logical = UnwrapMultiStatement(RequireLogicalPlan(buildItems));
        Assert.IsInstanceOfType<ProjectNode>(logical);
        var logicalProject = (ProjectNode)logical;
        Assert.IsInstanceOfType<WindowNode>(logicalProject.Input);
        var logicalWindow = (WindowNode)logicalProject.Input;
        Assert.HasCount(1, logicalWindow.Registrations);

        var physical = UnwrapPhysicalMultiStatement(RequirePhysicalPlan(buildItems));
        Assert.IsInstanceOfType<PhysicalProjectNode>(physical);
        var physicalProject = (PhysicalProjectNode)physical;
        Assert.IsInstanceOfType<PhysicalWindowNode>(physicalProject.Input);
        var physicalWindow = (PhysicalWindowNode)physicalProject.Input;
        Assert.IsInstanceOfType<PhysicalMaterializeNode>(physicalWindow.Input);
    }

    [TestMethod]
    public void WhenGroupedProjectionFeedsWindow_ShouldLowerViaIntermediateSourceInPhysicalPlan()
    {
        var buildItems = CreateBuildItems<BasicEntity>(
            "select City, Count(City) as CityCount, RowNumber() over (order by City) as RowNum from #A.Entities() group by City");

        Assert.IsNotNull(buildItems.Compilation);
        Assert.IsInstanceOfType<PhysicalMultiStatementNode>(RequirePhysicalPlan(buildItems));

        var multiStatement = (PhysicalMultiStatementNode)RequirePhysicalPlan(buildItems);
        Assert.IsGreaterThanOrEqualTo(multiStatement.Statements.Length, 2);

        var finalStatement = multiStatement.Statements[^1];
        Assert.IsInstanceOfType<PhysicalProjectNode>(finalStatement);

        var project = (PhysicalProjectNode)finalStatement;
        Assert.IsInstanceOfType<PhysicalWindowNode>(project.Input);

        var window = (PhysicalWindowNode)project.Input;
        Assert.IsInstanceOfType<PhysicalMaterializeNode>(window.Input);

        var materialize = (PhysicalMaterializeNode)window.Input;
        Assert.IsTrue(
            materialize.Input is PhysicalProjectNode or PhysicalCteRefNode,
            $"Expected grouped window input to flow through an intermediate projection or CTE ref, but encountered {materialize.Input.GetType().Name}.");
    }

    [TestMethod]
    public void WhenWindowInsideCte_ShouldBuildWindowDefinitionAndOuterFilterOverCteRef()
    {
        var buildItems = CreateBuildItems<BasicEntity>(@"
            with ranked as (
                select Name, RowNumber() over (order by Name) as RowNum from #A.entities()
            )
            select Name, RowNum from ranked where RowNum <= 2");

        Assert.IsNotNull(buildItems.Compilation);
        Assert.IsInstanceOfType<CteNode>(RequireLogicalPlan(buildItems));

        var logicalCte = (CteNode)RequireLogicalPlan(buildItems);
        Assert.HasCount(1, logicalCte.Definitions);
        var logicalDefinitionPrinted = LogicalPlanPrinter.Print(logicalCte.Definitions[0].Plan);
        StringAssert.Contains(logicalDefinitionPrinted, "Window");

        var logicalOuterQuery = GetFinalLogicalStatement(logicalCte.Query);
        Assert.IsInstanceOfType<ProjectNode>(logicalOuterQuery);
        var outerProject = (ProjectNode)logicalOuterQuery;
        Assert.IsInstanceOfType<FilterNode>(outerProject.Input);

        var outerFilter = (FilterNode)outerProject.Input;
        Assert.IsInstanceOfType<CteRefNode>(outerFilter.Input);

        Assert.IsInstanceOfType<PhysicalCteNode>(RequirePhysicalPlan(buildItems));
        var physicalCte = (PhysicalCteNode)RequirePhysicalPlan(buildItems);
        Assert.HasCount(1, physicalCte.Definitions);
        var physicalDefinitionPrinted = PhysicalPlanPrinter.Print(physicalCte.Definitions[0].Plan);
        StringAssert.Contains(physicalDefinitionPrinted, "PhysicalWindow");
        StringAssert.Contains(physicalDefinitionPrinted, "PhysicalMaterialize");

        var physicalOuterQuery = GetFinalPhysicalStatement(physicalCte.Query);
        Assert.IsInstanceOfType<PhysicalProjectNode>(physicalOuterQuery);
        var physicalOuterProject = (PhysicalProjectNode)physicalOuterQuery;
        Assert.IsInstanceOfType<PhysicalFilterNode>(physicalOuterProject.Input);

        var physicalOuterFilter = (PhysicalFilterNode)physicalOuterProject.Input;
        Assert.IsInstanceOfType<PhysicalCteRefNode>(physicalOuterFilter.Input);
    }

    [TestMethod]
    public void WhenWindowOverAggregatedCte_ShouldBuildCteAggregateAndOuterWindowOverCteRef()
    {
        var buildItems = CreateBuildItems<BasicEntity>(@"
            with agg as (
                select City, Sum(Population) as CityPop from #A.entities() group by City
            )
            select City, CityPop, Sum(CityPop) over (order by City) as RunningPop from agg");

        Assert.IsNotNull(buildItems.Compilation);
        Assert.IsInstanceOfType<CteNode>(RequireLogicalPlan(buildItems));

        var logicalCte = (CteNode)RequireLogicalPlan(buildItems);
        Assert.HasCount(1, logicalCte.Definitions);
        var logicalDefinitionPrinted = LogicalPlanPrinter.Print(logicalCte.Definitions[0].Plan);
        StringAssert.Contains(logicalDefinitionPrinted, "Aggregate");
        StringAssert.Contains(logicalDefinitionPrinted, "CteRef");

        var logicalOuterQuery = GetFinalLogicalStatement(logicalCte.Query);
        Assert.IsInstanceOfType<ProjectNode>(logicalOuterQuery);
        var logicalOuterProject = (ProjectNode)logicalOuterQuery;
        Assert.IsInstanceOfType<WindowNode>(logicalOuterProject.Input);

        var logicalOuterWindow = (WindowNode)logicalOuterProject.Input;
        Assert.IsInstanceOfType<CteRefNode>(logicalOuterWindow.Input);

        Assert.IsInstanceOfType<PhysicalCteNode>(RequirePhysicalPlan(buildItems));
        var physicalCte = (PhysicalCteNode)RequirePhysicalPlan(buildItems);
        Assert.HasCount(1, physicalCte.Definitions);
        var physicalDefinitionPrinted = PhysicalPlanPrinter.Print(physicalCte.Definitions[0].Plan);
        StringAssert.Contains(physicalDefinitionPrinted, "Aggregate");
        StringAssert.Contains(physicalDefinitionPrinted, "PhysicalCteRef");

        var physicalOuterQuery = GetFinalPhysicalStatement(physicalCte.Query);
        Assert.IsInstanceOfType<PhysicalProjectNode>(physicalOuterQuery);
        var physicalOuterProject = (PhysicalProjectNode)physicalOuterQuery;
        Assert.IsInstanceOfType<PhysicalWindowNode>(physicalOuterProject.Input);

        var physicalOuterWindow = (PhysicalWindowNode)physicalOuterProject.Input;
        Assert.IsInstanceOfType<PhysicalMaterializeNode>(physicalOuterWindow.Input);

        var physicalMaterialize = (PhysicalMaterializeNode)physicalOuterWindow.Input;
        Assert.IsInstanceOfType<PhysicalCteRefNode>(physicalMaterialize.Input);
    }

    [TestMethod]
    public void WhenAggregateValuesInCteWithOuterQualify_ShouldBuildQualifyOverWindowOnCteResult()
    {
        var buildItems = CreateBuildItems<BasicEntity>(@"
            with grouped as (
                select Country, AggregateValues(Name, ', ') as Names, Count(Name) as Cnt
                from #A.entities()
                group by Country
            )
            select Country, Names, Cnt,
                   RowNumber() over (order by Country) as rn
            from grouped
            qualify RowNumber() over (order by Country) <= 1");

        Assert.IsNotNull(buildItems.Compilation);
        Assert.IsInstanceOfType<CteNode>(RequireLogicalPlan(buildItems));

        var logicalCte = (CteNode)RequireLogicalPlan(buildItems);
        Assert.HasCount(1, logicalCte.Definitions);
        var logicalDefinitionPrinted = LogicalPlanPrinter.Print(logicalCte.Definitions[0].Plan);
        StringAssert.Contains(logicalDefinitionPrinted, "Aggregate");
        StringAssert.Contains(logicalDefinitionPrinted, "CteRef");

        var logicalOuterQuery = GetFinalLogicalStatement(logicalCte.Query);
        Assert.IsInstanceOfType<ProjectNode>(logicalOuterQuery);
        var logicalOuterProject = (ProjectNode)logicalOuterQuery;
        Assert.IsInstanceOfType<QualifyFilterNode>(logicalOuterProject.Input);

        var logicalQualify = (QualifyFilterNode)logicalOuterProject.Input;
        Assert.IsInstanceOfType<WindowNode>(logicalQualify.Input);

        var logicalWindow = (WindowNode)logicalQualify.Input;
        Assert.HasCount(1, logicalWindow.Registrations);
        Assert.IsInstanceOfType<CteRefNode>(logicalWindow.Input);

        Assert.IsInstanceOfType<PhysicalCteNode>(RequirePhysicalPlan(buildItems));
        var physicalCte = (PhysicalCteNode)RequirePhysicalPlan(buildItems);
        Assert.HasCount(1, physicalCte.Definitions);
        var physicalDefinitionPrinted = PhysicalPlanPrinter.Print(physicalCte.Definitions[0].Plan);
        StringAssert.Contains(physicalDefinitionPrinted, "Aggregate");
        StringAssert.Contains(physicalDefinitionPrinted, "PhysicalCteRef");
        var physicalOuterQuery = GetFinalPhysicalStatement(physicalCte.Query);
        Assert.IsInstanceOfType<PhysicalProjectNode>(physicalOuterQuery);

        var physicalOuterProject = (PhysicalProjectNode)physicalOuterQuery;
        Assert.IsInstanceOfType<PhysicalQualifyFilterNode>(physicalOuterProject.Input);

        var physicalQualify = (PhysicalQualifyFilterNode)physicalOuterProject.Input;
        Assert.IsInstanceOfType<PhysicalWindowNode>(physicalQualify.Input);

        var physicalWindow = (PhysicalWindowNode)physicalQualify.Input;
        Assert.IsInstanceOfType<PhysicalMaterializeNode>(physicalWindow.Input);

        var physicalMaterialize = (PhysicalMaterializeNode)physicalWindow.Input;
        Assert.IsInstanceOfType<PhysicalCteRefNode>(physicalMaterialize.Input);
    }

    [TestMethod]
    public void WhenQualifyUsesWindowFunction_ShouldProduceQualifyOverWindowInBothPlans()
    {
        var buildItems = CreateBuildItems<BasicEntity>(
            "select Name, RowNumber() over (order by Name) as RowNum from #A.Entities() qualify RowNumber() over (order by Name) <= 2");

        var logical = UnwrapMultiStatement(RequireLogicalPlan(buildItems));
        Assert.IsInstanceOfType<ProjectNode>(logical);
        var logicalProject = (ProjectNode)logical;
        Assert.IsInstanceOfType<QualifyFilterNode>(logicalProject.Input);
        var logicalQualify = (QualifyFilterNode)logicalProject.Input;
        Assert.IsInstanceOfType<WindowNode>(logicalQualify.Input);

        var physical = UnwrapPhysicalMultiStatement(RequirePhysicalPlan(buildItems));
        Assert.IsInstanceOfType<PhysicalProjectNode>(physical);
        var physicalProject = (PhysicalProjectNode)physical;
        Assert.IsInstanceOfType<PhysicalQualifyFilterNode>(physicalProject.Input);
        var physicalQualify = (PhysicalQualifyFilterNode)physicalProject.Input;
        Assert.IsInstanceOfType<PhysicalWindowNode>(physicalQualify.Input);
        var physicalWindow = (PhysicalWindowNode)physicalQualify.Input;
        Assert.IsInstanceOfType<PhysicalMaterializeNode>(physicalWindow.Input);
    }

    [TestMethod]
    public void WhenSelectAndQualifyReuseSameWindowFunction_ShouldDeduplicateWindowRegistration()
    {
        var buildItems = CreateBuildItems<BasicEntity>(
            "select Name, RowNumber() over (order by Name) as RowNum from #A.Entities() qualify RowNumber() over (order by Name) <= 2");

        var logical = UnwrapMultiStatement(RequireLogicalPlan(buildItems));
        var logicalProject = (ProjectNode)logical;
        var logicalQualify = (QualifyFilterNode)logicalProject.Input;
        var logicalWindow = (WindowNode)logicalQualify.Input;

        Assert.HasCount(1, logicalWindow.Registrations);
        Assert.AreEqual(0, ((WindowFunctionRef)logicalProject.Fields[1].Expression).WindowIndex);

        Assert.IsInstanceOfType<BinaryOp>(logicalQualify.Predicate);
        var qualifyPredicate = (BinaryOp)logicalQualify.Predicate;
        Assert.IsInstanceOfType<WindowFunctionRef>(qualifyPredicate.Left);
        Assert.AreEqual(0, ((WindowFunctionRef)qualifyPredicate.Left).WindowIndex);
    }

    [TestMethod]
    public void WhenPlansPrintable_LogicalPlanShouldPrint()
    {
        var buildItems = CreateBuildItems<BasicEntity>("select Name from #A.Entities()");

        var printed = LogicalPlanPrinter.Print(RequireLogicalPlan(buildItems));

        Assert.IsFalse(string.IsNullOrWhiteSpace(printed));
        StringAssert.Contains(printed, "Project");
        StringAssert.Contains(printed, "SchemaScan");
    }

    [TestMethod]
    public void WhenPlansPrintable_PhysicalPlanShouldPrint()
    {
        var buildItems = CreateBuildItems<BasicEntity>("select Name from #A.Entities()");

        var printed = PhysicalPlanPrinter.Print(RequirePhysicalPlan(buildItems));

        Assert.IsFalse(string.IsNullOrWhiteSpace(printed));
        StringAssert.Contains(printed, "PhysicalProject");
        StringAssert.Contains(printed, "PhysicalSchemaScan");
    }

    [TestMethod]
    public void WhenGroupByUsed_ExistingPipelineShouldStillWork()
    {
        var buildItems = CreateBuildItems<BasicEntity>(
            "select City, Count(City) from #A.Entities() group by City having Count(City) > 1 order by City");

        Assert.IsNotNull(buildItems.Compilation);
        Assert.IsNotNull(buildItems.UsedColumns);
        Assert.IsNotNull(buildItems.TransformedQueryTree);
    }

    [TestMethod]
    public void WhenMultipleColumns_ShouldTrackAllProjectedFields()
    {
        var buildItems = CreateBuildItems<BasicEntity>(
            "select Name, City, Country from #A.Entities()");

        var logical = UnwrapMultiStatement(RequireLogicalPlan(buildItems));
        Assert.IsInstanceOfType<ProjectNode>(logical);
        var project = (ProjectNode)logical;
        Assert.HasCount(3, project.Fields);
        Assert.AreEqual("Name", project.Fields[0].OutputName);
        Assert.AreEqual("City", project.Fields[1].OutputName);
        Assert.AreEqual("Country", project.Fields[2].OutputName);
    }
}
