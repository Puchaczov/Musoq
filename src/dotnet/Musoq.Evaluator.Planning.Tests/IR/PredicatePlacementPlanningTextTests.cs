using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests.IR;

[TestClass]
public sealed class PredicatePlacementPlanningTextTests : BasicEntityTestBase
{
    private readonly PlanTextBuildHarness _buildHarness = new();

    [TestMethod]
    public void PlanningText_WhenSourceLocalPredicateIsPushed_ShouldUsePushedPredicateText()
    {
        var buildItems = CreateBuildItems<UsedColumnsOrUsedWhereEntity>(
            "select e.Name from #A.Entities() e where e.Population > 100");

        Assert.Contains("pushdown: (e.Population > 100)", buildItems.RequirePlanningText());
        Assert.Contains("placement: Where -> SourcePushdown (High) aliases: e predicate: (e.Population > 100)", buildItems.RequirePlanningText());
        Assert.Contains("facts: owners=e.Population group=Where:conjunct:0:e deterministic=yes nulls=NullSensitive blocked=none", buildItems.RequirePlanningText());
    }

    [TestMethod]
    public void PlanningText_WhenWhereHasMultipleConjuncts_ShouldClassifyEachConjunct()
    {
        var buildItems = CreateBuildItems<UsedColumnsOrUsedWhereEntity>(
            "select e.Name from #A.Entities() e where e.Population > 100 and e.City = 'NYC' and 1 = 1");

        Assert.Contains("placement: Where -> SourcePushdown (High) aliases: e predicate: (e.Population > 100)", buildItems.RequirePlanningText());
        Assert.Contains("placement: Where -> SourcePushdown (High) aliases: e predicate: (e.City = 'NYC')", buildItems.RequirePlanningText());
        Assert.Contains("placement: Where -> ConstantPredicate (High) aliases: none predicate: TRUE", buildItems.RequirePlanningText());
        Assert.Contains("group=Where:conjunct:2:constant deterministic=yes nulls=NullInsensitive blocked=none", buildItems.RequirePlanningText());
    }

    [TestMethod]
    public void PlanningText_WhenOuterJoinFilterReferencesNullableSide_ShouldStayPostJoin()
    {
        var buildItems = CreateBuildItems<BasicEntity>(
            "select a.Name, b.Name from #A.Entities() a left outer join #B.Entities() b on a.Id = b.Id where b.Population > 100");

        Assert.Contains("placement: Where -> PostJoin (Medium) aliases: b predicate: (b.Population > 100)", buildItems.RequirePlanningText());
        Assert.Contains("owners=b.Population", buildItems.RequirePlanningText());
        Assert.Contains("blocked=LeftOuter join predicate remains at the post-join boundary to preserve outer join row semantics.", buildItems.RequirePlanningText());
        Assert.Contains("LeftOuter join predicate remains at the post-join boundary to preserve outer join row semantics.", buildItems.RequirePlanningText());
    }

    [TestMethod]
    public void PlanningText_WhenApplyFilterReferencesBothSides_ShouldStayRuntimeOnly()
    {
        var buildItems = _buildHarness.BuildForThreeSources(
            "select p.Name, t.Value from #schema.first() p cross apply p.Tags t where p.Name = t.Value",
            [new ExploratoryEvaluatorTestsBase.Person { Name = "vip", Tags = ["vip"] }],
            Array.Empty<ExploratoryEvaluatorTestsBase.Order>(),
            Array.Empty<ExploratoryEvaluatorTestsBase.TreeNode>());

        Assert.Contains("placement: Where -> RuntimeFilter (Medium) aliases: p, t predicate: (p.Name = t.Value)", buildItems.RequirePlanningText());
        Assert.Contains("Predicate crosses a Cross APPLY boundary, so placement remains runtime-only until APPLY correlation movement is implemented.", buildItems.RequirePlanningText());
    }

    [TestMethod]
    public void PlanningText_WhenHavingPredicateReferencesSourceColumn_ShouldStayPostAggregate()
    {
        var buildItems = CreateBuildItems<BasicEntity>(
            "select e.City, Count(e.City) as CityCount from #A.Entities() e group by e.City having e.City = 'NYC' and Count(e.City) > 1");

        Assert.Contains("placement: Having -> PostAggregate (High) aliases: e predicate: (e.City = 'NYC')", buildItems.RequirePlanningText());
        Assert.Contains("placement: Having -> PostAggregate (High) aliases: none predicate: (AggRef(e.Count(e.City)) > 1)", buildItems.RequirePlanningText());
    }

    [TestMethod]
    public void PlanningText_WhenQualifyPredicateReferencesWindow_ShouldStayPostWindow()
    {
        var buildItems = CreateBuildItems<BasicEntity>(
            "select e.Name, RowNumber() over (order by e.Name) as RowNum from #A.Entities() e qualify RowNumber() over (order by e.Name) <= 2");

        Assert.Contains("placement: Qualify -> PostWindow (High) aliases: none predicate: (WindowRef(0) <= 2)", buildItems.RequirePlanningText());
        Assert.Contains("Qualify predicates are evaluated at their logical post-window stage.", buildItems.RequirePlanningText());
    }

    [TestMethod]
    public void PlanningText_WhenAliasAppearsInMultipleSourceScopes_ShouldRemainConservative()
    {
        var buildItems = CreateBuildItems<BasicEntity>(
            "with c as (select e.Name from #A.Entities() e where e.Population > 100) select e.Name from #B.Entities() e where e.Population > 200");

        Assert.Contains("placement: Where -> RuntimeFilter (Low) aliases: e predicate: (e.Population > 100)", buildItems.RequirePlanningText());
        Assert.Contains("placement: Where -> RuntimeFilter (Low) aliases: e predicate: (e.Population > 200)", buildItems.RequirePlanningText());
        Assert.Contains("Predicate alias appears in multiple source scopes, so placement remains conservative.", buildItems.RequirePlanningText());
    }
}
