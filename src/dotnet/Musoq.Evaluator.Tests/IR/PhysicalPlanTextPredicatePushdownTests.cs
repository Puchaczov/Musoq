using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Physical.Nodes;
using Musoq.Evaluator.Tests.Schema.Basic;
using PhysicalPlanPrinter = Musoq.Evaluator.IR.Physical.PhysicalPlanPrinter;

namespace Musoq.Evaluator.Tests.IR;

[TestClass]
public sealed class PhysicalPlanTextPredicatePushdownTests : BasicEntityTestBase
{
    [TestMethod]
    public void Print_WhenJoinWhereCanBeSplitAcrossSources_ShouldShowPushdownOnEachScan()
    {
        var buildItems = CreateBuildItems<UsedColumnsOrUsedWhereEntity>(
            "select a.Name, b.City from #A.Entities() a join #B.Entities() b on a.City = b.City where a.Population > 100 and b.City = 'NYC'");

        var planText = PhysicalPlanPrinter.Print(buildItems.RequirePhysicalPlan());

        PlanTextAssertions.AreEqual(
            PlanTextAssertions.FromLines(
                "PhysicalMultiStatement",
                "  PhysicalProject [a.Name as a.Name, a.City as a.City, a.Population as a.Population, b.City as b.City]",
                "    PhysicalHashJoin [Inner] [build: b.City] [probe: a.City]",
                "      PhysicalFilter [(a.Population > 100)]",
                "        PhysicalSchemaScan [#A.Entities() as a] [pushdown: (a.Population > 100)]",
                "      PhysicalFilter [(b.City = 'NYC')]",
                "        PhysicalSchemaScan [#B.Entities() as b] [pushdown: (b.City = 'NYC')]",
                "  PhysicalProject [a.Name as a.Name, b.City as b.City]",
                "    PhysicalFilter [((a.Population > 100) AND (b.City = 'NYC'))]",
                "      PhysicalCteRef [ab as ab]"),
            planText);
    }

    [TestMethod]
    public void Print_WhenJoinWhereCanBeSplitAcrossSources_ShouldExplainSourcePredicatePlans()
    {
        var buildItems = CreateBuildItems<UsedColumnsOrUsedWhereEntity>(
            "select a.Name, b.City from #A.Entities() a join #B.Entities() b on a.City = b.City where a.Population > 100 and b.City = 'NYC'");

        Assert.Contains("predicate where: a.Population > 100 and 1 = 1", buildItems.RequirePlanningText());
        Assert.Contains("predicate where: 1 = 1 and b.City = 'NYC'", buildItems.RequirePlanningText());
        Assert.Contains("PredicatePushdown [SourcePredicatePlan]", buildItems.RequirePlanningText());
        Assert.Contains("Pushed 1 source-local predicate(s); runtime filter remains for full predicate semantics.", buildItems.RequirePlanningText());
        Assert.Contains("movement: Where -> PreInnerJoinLeft", buildItems.RequirePlanningText());
        Assert.Contains("movement: Where -> PreInnerJoinRight", buildItems.RequirePlanningText());
        Assert.Contains("PredicateMovement [PredicateMovementPlan]", buildItems.RequirePlanningText());
    }

    [TestMethod]
    public void Print_WhenInnerJoinOnHasSourceLocalConjuncts_ShouldPrefilterJoinSides()
    {
        var buildItems = CreateBuildItems<UsedColumnsOrUsedWhereEntity>(
            "select a.Name, b.City from #A.Entities() a join #B.Entities() b on a.City = b.City and a.Population > 100 and b.City = 'NYC'");

        var planText = PhysicalPlanPrinter.Print(buildItems.RequirePhysicalPlan());

        Assert.Contains("PhysicalHashJoin [Inner] [build: b.City] [probe: a.City] [residual: ((a.Population > 100) AND (b.City = 'NYC'))]", planText);
        Assert.Contains("PhysicalFilter [(a.Population > 100)]", planText);
        Assert.Contains("PhysicalFilter [(b.City = 'NYC')]", planText);
        Assert.Contains("movement: JoinOn -> PreInnerJoinLeft", buildItems.RequirePlanningText());
        Assert.Contains("movement: JoinOn -> PreInnerJoinRight", buildItems.RequirePlanningText());
    }

    [TestMethod]
    public void Print_WhenInnerJoinOnHasDeterministicMethodConjunct_ShouldPrefilterJoinSide()
    {
        var buildItems = CreateBuildItems<BasicEntity>(
            "select a.Name, b.City from #A.Entities() a join #B.Entities() b on a.Id = b.Id and ToUpper(b.City) = 'NYC'");

        var planText = PhysicalPlanPrinter.Print(buildItems.RequirePhysicalPlan());
        var planningText = buildItems.RequirePlanningText();

        Assert.Contains("PhysicalFilter [(ToUpper(b.City) = 'NYC')]", planText);
        Assert.Contains("PhysicalHashJoin [Inner] [build: b.Id] [probe: a.Id] [residual: (ToUpper(b.City) = 'NYC')]", planText);
        Assert.Contains("movement: JoinOn -> PreInnerJoinRight", planningText);
        Assert.Contains("Predicate is deterministic, source-local, and mapped to the right side of an inner join.", planningText);
    }

    [TestMethod]
    public void Print_WhenWhereHasDeterministicMethodConjunct_ShouldPrefilterJoinSideAndKeepRuntimeGuard()
    {
        var buildItems = CreateBuildItems<BasicEntity>(
            "select a.Name, b.City from #A.Entities() a join #B.Entities() b on a.Id = b.Id where ToUpper(b.City) = 'NYC'");

        var planText = PhysicalPlanPrinter.Print(buildItems.RequirePhysicalPlan());
        var normalizedPlanText = planText.Replace("\r\n", "\n", StringComparison.Ordinal);
        var planningText = buildItems.RequirePlanningText();

        Assert.Contains("PhysicalFilter [(ToUpper(b.City) = 'NYC')]", planText);
        Assert.Contains("PhysicalFilter [(ToUpper(b.City) = 'NYC')]\n      PhysicalCteRef [ab as ab]", normalizedPlanText);
        Assert.Contains("movement: Where -> PreInnerJoinRight", planningText);
        Assert.Contains("Original predicate remains in place as a semantic safety net.", planningText);
    }

    [TestMethod]
    public void Print_WhenJoinWhereUsesCrossSourceOr_ShouldKeepRuntimeFilterOnly()
    {
        var buildItems = CreateBuildItems<UsedColumnsOrUsedWhereEntity>(
            "select a.Name, b.City from #A.Entities() a join #B.Entities() b on a.City = b.City where a.Population > 100 or b.City = 'NYC'");

        var planText = PhysicalPlanPrinter.Print(buildItems.RequirePhysicalPlan());

        PlanTextAssertions.AreEqual(
            PlanTextAssertions.FromLines(
                "PhysicalMultiStatement",
                "  PhysicalProject [a.Name as a.Name, a.City as a.City, a.Population as a.Population, b.City as b.City]",
                "    PhysicalHashJoin [Inner] [build: b.City] [probe: a.City]",
                "      PhysicalSchemaScan [#A.Entities() as a]",
                "      PhysicalSchemaScan [#B.Entities() as b]",
                "  PhysicalProject [a.Name as a.Name, b.City as b.City]",
                "    PhysicalFilter [((a.Population > 100) OR (b.City = 'NYC'))]",
                "      PhysicalCteRef [ab as ab]"),
            planText);
    }

    [TestMethod]
    public void Print_WhenJoinWhereUsesCrossSourceOr_ShouldExplainRuntimeOnlyPredicatePlan()
    {
        var buildItems = CreateBuildItems<UsedColumnsOrUsedWhereEntity>(
            "select a.Name, b.City from #A.Entities() a join #B.Entities() b on a.City = b.City where a.Population > 100 or b.City = 'NYC'");

        Assert.Contains("predicate where: 1 = 1", buildItems.RequirePlanningText());
        Assert.Contains("RetainedRuntimeOnly", buildItems.RequirePlanningText());
        Assert.Contains("Predicate was retained for runtime because source-local rewrite produced a neutral predicate.", buildItems.RequirePlanningText());
        Assert.IsFalse(buildItems.RequirePlanningText().Contains("movement: Where ->", StringComparison.Ordinal));
    }

    [TestMethod]
    public void Print_WhenJoinWhereUsesCrossSourceComparison_ShouldKeepRuntimeFilterOnly()
    {
        var buildItems = CreateBuildItems<UsedColumnsOrUsedWhereEntity>(
            "select a.Name, b.City from #A.Entities() a join #B.Entities() b on a.City = b.City where a.Population > b.Population");

        var planText = PhysicalPlanPrinter.Print(buildItems.RequirePhysicalPlan());

        PlanTextAssertions.AreEqual(
            PlanTextAssertions.FromLines(
                "PhysicalMultiStatement",
                "  PhysicalProject [a.Name as a.Name, a.City as a.City, a.Population as a.Population, b.City as b.City, b.Population as b.Population]",
                "    PhysicalHashJoin [Inner] [build: b.City] [probe: a.City]",
                "      PhysicalSchemaScan [#A.Entities() as a]",
                "      PhysicalSchemaScan [#B.Entities() as b]",
                "  PhysicalProject [a.Name as a.Name, b.City as b.City]",
                "    PhysicalFilter [(a.Population > b.Population)]",
                "      PhysicalCteRef [ab as ab]"),
            planText);
    }

    [TestMethod]
    public void Print_WhenJoinWhereUsesCrossSourceComparison_ShouldExplainRuntimeOnlyPredicatePlan()
    {
        var buildItems = CreateBuildItems<UsedColumnsOrUsedWhereEntity>(
            "select a.Name, b.City from #A.Entities() a join #B.Entities() b on a.City = b.City where a.Population > b.Population");

        Assert.Contains("predicate where: 1 = 1", buildItems.RequirePlanningText());
        Assert.Contains("RetainedRuntimeOnly", buildItems.RequirePlanningText());
        Assert.Contains("Predicate was retained for runtime because source-local rewrite produced a neutral predicate.", buildItems.RequirePlanningText());
    }

    [TestMethod]
    public void Print_WhenWhereUsesMethodCall_ShouldExplainRuntimeOnlyPredicatePlan()
    {
        var buildItems = CreateBuildItems<UsedColumnsOrUsedWhereEntity>(
            "select 1 from #A.Entities() a where a.City contains (DoNothing('abc'), 'def')");

        Assert.Contains("predicate where: 1 = 1", buildItems.RequirePlanningText());
        Assert.Contains("RetainedRuntimeOnly", buildItems.RequirePlanningText());
        Assert.Contains("Predicate was retained for runtime because source-local rewrite produced a neutral predicate.", buildItems.RequirePlanningText());
    }

    [TestMethod]
    public void Print_WhenSourceHasNoWherePredicate_ShouldExplainNeutralPredicateFallback()
    {
        var buildItems = CreateBuildItems<UsedColumnsOrUsedWhereEntity>(
            "select a.Name from #A.Entities() a");

        Assert.Contains("predicate where: 1 = 1", buildItems.RequirePlanningText());
        Assert.Contains("No non-neutral source-local predicate was available for pushdown.", buildItems.RequirePlanningText());
        Assert.Contains("RetainedRuntimeOnly", buildItems.RequirePlanningText());
    }

    [TestMethod]
    public void Print_WhenWherePrecedesHaving_ShouldKeepPushdownAndPostAggregateHaving()
    {
        var buildItems = CreateBuildItems<UsedColumnsOrUsedWhereEntity>(
            "select e.City, Count(e.City) as CityCount from #A.Entities() e where e.Population > 100 group by e.City having Count(e.City) >= 2");

        var planText = PhysicalPlanPrinter.Print(buildItems.RequirePhysicalPlan());

        PlanTextAssertions.AreEqual(
            PlanTextAssertions.FromLines(
                "PhysicalMultiStatement",
                "  PhysicalProject [e.City as e.City, AggRef(e.Count(e.City)) as e.Count(e.City)]",
                "    PhysicalHaving [(AggRef(e.Count(e.City)) >= 2)]",
                "      PhysicalSingleKeyAggregate [key: e.City (String)] [aggs: Count(City)]",
                "        PhysicalFilter [(e.Population > 100)]",
                "          PhysicalSchemaScan [#A.Entities() as e] [pushdown: (e.Population > 100)]",
                "  PhysicalProject [e.City as e.City, e.Count(e.City) as CityCount]",
                "    PhysicalCteRef [eScore as eScore]"),
            planText);
    }

    [TestMethod]
    public void Print_WhenSchemaScanHasMultiplePushedPredicates_ShouldRenderReadableSuffix()
    {
        var schema = new OutputSchema(
            [new ColumnSchema("Id", typeof(int), 0)]);
        IrExpression[] pushedPredicates =
        [
            new BinaryOp(
                BinaryOpKind.GreaterThan,
                new ColumnRef("a", "Population", typeof(decimal)),
                new Literal(100, typeof(int)),
                typeof(bool)),
            new BinaryOp(
                BinaryOpKind.Equal,
                new ColumnRef("a", "City", typeof(string)),
                new Literal("NYC", typeof(string)),
                typeof(bool))
        ];
        var scan = new PhysicalSchemaScanNode("A", "Entities", [], "a", pushedPredicates, [], schema);

        var planText = PhysicalPlanPrinter.Print(scan);

        PlanTextAssertions.AreEqual(
            "PhysicalSchemaScan [#A.Entities() as a] [pushdown: (a.Population > 100), (a.City = 'NYC')]",
            planText);
    }
}
