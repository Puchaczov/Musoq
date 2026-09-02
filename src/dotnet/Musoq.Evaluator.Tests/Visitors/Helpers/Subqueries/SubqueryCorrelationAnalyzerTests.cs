using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Visitors.Helpers.Subqueries;
using Musoq.Parser;
using Musoq.Parser.Lexing;

namespace Musoq.Evaluator.Tests.Visitors.Helpers.Subqueries;

[TestClass]
public sealed class SubqueryCorrelationAnalyzerTests
{
    [TestMethod]
    public void Analyze_UncorrelatedInSubquery_ShouldReportNoCorrelation()
    {
        var analysis = Analyze(
            "select a.City from #A.entities() a where a.City in (select b.City from #B.entities() b)");

        var subquery = AssertSingleSubquery(analysis);
        Assert.IsFalse(subquery.IsCorrelated);
        Assert.Contains("a", subquery.OuterAliases);
        Assert.Contains("b", subquery.LocalAliases);
        Assert.Contains("a", subquery.Facts.OuterAliases);
        Assert.Contains("b", subquery.Facts.LocalAliases);
        Assert.IsEmpty(subquery.CorrelatedAliases);
        Assert.AreEqual(SubqueryCorrelationNullSemantics.NotCorrelated, subquery.Facts.NullSemantics);
    }

    [TestMethod]
    public void Analyze_CorrelatedInSubquery_ShouldReportOuterAlias()
    {
        var analysis = Analyze(@"
            select a.City from #A.entities() a
            where a.City in (
                select b.City from #B.entities() b
                where b.Country = a.Country)");

        var subquery = AssertSingleSubquery(analysis);
        Assert.IsTrue(subquery.IsCorrelated);
        Assert.Contains("a", subquery.CorrelatedAliases);
        Assert.Contains("b", subquery.LocalAliases);
        Assert.AreEqual(SubqueryCorrelationNullSemantics.EqualityComparison, subquery.Facts.NullSemantics);

        var key = subquery.Facts.EqualityKeys.Single();
        Assert.AreEqual("b", key.LocalAlias);
        Assert.AreEqual("Country", key.LocalColumn);
        Assert.AreEqual("a", key.OuterAlias);
        Assert.AreEqual("Country", key.OuterColumn);
    }

    [TestMethod]
    public void Analyze_NonEqualityCorrelation_ShouldReportResidualNullSemantics()
    {
        var analysis = Analyze(@"
            select a.City from #A.entities() a
            where a.City in (
                select b.City from #B.entities() b
                where b.Population < a.Population)");

        var subquery = AssertSingleSubquery(analysis);
        Assert.IsTrue(subquery.IsCorrelated);
        Assert.IsEmpty(subquery.Facts.EqualityKeys);
        Assert.AreEqual(SubqueryCorrelationNullSemantics.ResidualOrUnknown, subquery.Facts.NullSemantics);
    }

    [TestMethod]
    public void Analyze_CorrelatedScalarWithOrderingAndTake_ShouldReportCardinalitySensitiveContexts()
    {
        var analysis = Analyze(@"
            select a.City, (
                select b.City from #B.entities() b
                where b.Country = a.Country
                order by b.Population desc
                take 1
            ) as MatchCity
            from #A.entities() a");

        var subquery = AssertSingleSubquery(analysis);
        var contexts = subquery.Facts.CardinalitySensitiveContexts
            .Select(static context => context.Kind)
            .ToArray();

        CollectionAssert.Contains(contexts, SubqueryCardinalityContextKind.ScalarSubquery);
        CollectionAssert.Contains(contexts, SubqueryCardinalityContextKind.OrderBy);
        CollectionAssert.Contains(contexts, SubqueryCardinalityContextKind.Take);
        Assert.IsTrue(subquery.Facts.IsCardinalitySensitive);
    }

    [TestMethod]
    public void Analyze_CteBodySubquery_ShouldCorrelateToCteBodyLocalQuery()
    {
        var analysis = Analyze(@"
            with p as (
                select a.City from #A.entities() a
                where a.City in (
                    select b.City from #B.entities() b
                    where b.Country = a.Country)
            )
            select City from p");

        var subquery = AssertSingleSubquery(analysis);
        Assert.IsTrue(subquery.IsInsideCteDefinition);
        Assert.Contains("a", subquery.CorrelatedAliases);
        Assert.IsFalse(subquery.HasIllegalOuterConsumingCteReferences);
        Assert.IsFalse(analysis.HasIllegalOuterConsumingCteReferences);
    }

    [TestMethod]
    public void Analyze_CteDefinitionReferencingConsumerAlias_ShouldReportIllegalOuterConsumingReference()
    {
        const string query = @"
            with p as (
                select b.City from #B.entities() b
                where b.Country = a.Country
            )
            select a.City from #A.entities() a
            where a.City in (select City from p)";
        var analysis = Analyze(query);

        Assert.IsTrue(analysis.HasIllegalOuterConsumingCteReferences);
        CollectionAssert.AreEquivalent(new[] { "a" }, analysis.IllegalOuterConsumingCteAliases.ToArray());
        Assert.Contains("a", analysis.IllegalOuterConsumingCteAliases);
        var referenceStart = query.Trim().IndexOf("a.Country", StringComparison.Ordinal);
        Assert.AreEqual(new TextSpan(referenceStart, "a.Country".Length),
            analysis.IllegalOuterConsumingCteReferenceSpan);
    }

    [TestMethod]
    public void Analyze_SubqueryAliasShadowingOuterAlias_ShouldNotMarkCorrelation()
    {
        var analysis = Analyze(@"
            select a.City from #A.entities() a
            where a.City in (
                select a.City from #B.entities() a
                where a.Country = 'POLAND')");

        var subquery = AssertSingleSubquery(analysis);
        Assert.Contains("a", subquery.OuterAliases);
        Assert.Contains("a", subquery.LocalAliases);
        Assert.IsFalse(subquery.IsCorrelated);
    }

    [TestMethod]
    public void Analyze_NestedSubqueryCorrelation_ShouldNotLeakInnerCorrelationToParent()
    {
        var analysis = Analyze(@"
            select a.City from #A.entities() a
            where a.City in (
                select b.City from #B.entities() b
                where b.Country in (
                    select c.Country from #C.entities() c
                    where c.City = b.City))");

        Assert.HasCount(2, analysis.Subqueries);

        var inner = analysis.Subqueries.Single(item => item.CorrelatedAliases.Contains("b"));
        var outer = analysis.Subqueries.Single(item => item.OuterAliases.Contains("a") && !item.OuterAliases.Contains("b"));

        Assert.Contains("b", inner.CorrelatedAliases);
        Assert.IsFalse(outer.IsCorrelated);
    }

    private static SubqueryCorrelationInfo AssertSingleSubquery(SubqueryCorrelationAnalysis analysis)
    {
        Assert.HasCount(1, analysis.Subqueries);
        return analysis.Subqueries[0];
    }

    private static SubqueryCorrelationAnalysis Analyze(string query)
    {
        var lexer = new Lexer(query, true);
        var parser = new Musoq.Parser.Parser(lexer);
        return SubqueryCorrelationAnalyzer.Analyze(parser.ComposeAll());
    }
}
