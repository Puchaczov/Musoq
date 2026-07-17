using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Optimization.Logical;
using Musoq.Evaluator.IR.Optimization.Logical.Subqueries;
using Musoq.Evaluator.Visitors.Helpers.Subqueries;
using Musoq.Parser.Lexing;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Tests.IR;

[TestClass]
public sealed class CorrelatedSubqueryStrategyPlannerTests
{
    [TestMethod]
    public void Plan_DirectPredicateFilters_ShouldSelectSemiAndAntiHashStrategies()
    {
        var decisions = Plan("""
            select a.City
            from #A.entities() a
            where exists (
                select b.City from #B.entities() b
                where b.Country = a.Country
            ) and not exists (
                select c.City from #C.entities() c
                where c.Country = a.Country
            )
            """);

        Assert.HasCount(2, decisions);
        Assert.AreEqual(CorrelatedSubqueryStrategyKind.HashSemiJoin, decisions[0].Strategy);
        Assert.AreEqual(CorrelatedSubqueryStrategyKind.HashAntiJoin, decisions[1].Strategy);
        Assert.IsTrue(decisions.All(static decision => decision.Request.EvaluationPhase == SubqueryEvaluationPhase.Filter));
        Assert.IsTrue(decisions.All(static decision => decision.Request.IsDirectFilter));
    }

    [TestMethod]
    public void Plan_PredicateUsedAsProjection_ShouldSelectHashMarkAtProjectionPhase()
    {
        var decision = Plan("""
            select exists (
                select b.City from #B.entities() b
                where b.Country = a.Country
            ) as HasMatch
            from #A.entities() a
            """).Single();

        Assert.AreEqual(CorrelatedSubqueryStrategyKind.HashMarkJoin, decision.Strategy);
        Assert.AreEqual(SubqueryEvaluationPhase.Projection, decision.Request.EvaluationPhase);
        Assert.IsFalse(decision.Request.IsDirectFilter);
    }

    [TestMethod]
    public void Plan_ScalarSubquery_ShouldSelectHashSingle()
    {
        var decision = Plan("""
            select (
                select b.City from #B.entities() b
                where b.Country = a.Country
            ) as MatchCity
            from #A.entities() a
            """).Single();

        Assert.AreEqual(CorrelatedSubqueryStrategyKind.HashSingleJoin, decision.Strategy);
        Assert.AreEqual(SubqueryEvaluationPhase.Projection, decision.Request.EvaluationPhase);
    }

    [TestMethod]
    public void Plan_SlicedScalarSubquery_ShouldSelectPartitionedTopOffset()
    {
        var decision = Plan("""
            select (
                select b.City from #B.entities() b
                where b.Country = a.Country
                order by b.City
                skip 1 take 1
            ) as MatchCity
            from #A.entities() a
            """).Single();

        Assert.AreEqual(CorrelatedSubqueryStrategyKind.PartitionedTopOffset, decision.Strategy);
    }

    [TestMethod]
    public void Plan_ResidualOnlyCorrelation_ShouldNotChooseImplicitPerRowApply()
    {
        var decision = Plan("""
            select (
                select b.City from #B.entities() b
                where b.Score > a.Score
            ) as MatchCity
            from #A.entities() a
            """).Single();

        Assert.AreEqual(CorrelatedSubqueryStrategyKind.Unsupported, decision.Strategy);
        Assert.Contains("not selected implicitly", decision.Reason);
    }

    [TestMethod]
    public void Plan_NestedCorrelations_ShouldUseTheNearestEvaluationPhase()
    {
        var decisions = Plan("""
            select (
                select b.City from #B.entities() b
                where b.Country = a.Country and exists (
                    select c.City from #C.entities() c
                    where c.City = b.City
                )
            ) as MatchCity
            from #A.entities() a
            """);

        var scalar = decisions.Single(static decision => decision.Request.IsScalar);
        var predicate = decisions.Single(static decision => decision.Request.IsPredicate);

        Assert.AreEqual(SubqueryEvaluationPhase.Projection, scalar.Request.EvaluationPhase);
        Assert.AreEqual(CorrelatedSubqueryStrategyKind.HashSingleJoin, scalar.Strategy);
        Assert.AreEqual(SubqueryEvaluationPhase.Filter, predicate.Request.EvaluationPhase);
        Assert.AreEqual(CorrelatedSubqueryStrategyKind.HashSemiJoin, predicate.Strategy);
    }

    [TestMethod]
    public void Normalize_ShouldPublishPhaseAwareDecisionsAsSidecarFacts()
    {
        var root = Parse("""
            select a.City
            from #A.entities() a
            where exists (
                select b.City from #B.entities() b
                where b.Country = a.Country
            )
            """);

        var result = new PreLogicalNormalizer().Normalize(root);
        var decision = result.CorrelatedSubqueryDecisions.Single();

        Assert.AreEqual(CorrelatedSubqueryStrategyKind.HashSemiJoin, decision.Strategy);
        Assert.AreEqual(SubqueryEvaluationPhase.Filter, decision.Request.EvaluationPhase);
        Assert.IsTrue(result.Trace.Entries.Single(static entry =>
            entry.PassName == "SubqueryToCteNormalization").Reason.Contains("recomputed 2"));
    }

    private static IReadOnlyList<CorrelatedSubqueryDecision> Plan(string query)
    {
        var root = Parse(query);
        var analysis = SubqueryCorrelationAnalyzer.Analyze(root);
        var requests = CorrelatedSubqueryRewriteRequestBuilder.Build(root, analysis);
        return CorrelatedSubqueryStrategyPlanner.Plan(requests);
    }

    private static RootNode Parse(string query)
    {
        var lexer = new Lexer(query, true);
        var parser = new Musoq.Parser.Parser(lexer);
        return parser.ComposeAll();
    }
}
