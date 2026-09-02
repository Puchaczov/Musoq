using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;
using static Musoq.Evaluator.Tests.MusoqExceptionAssertions;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class Core014PredicateSubqueryTests : BasicEntityTestBase
{
    [TestMethod]
    public void LiteralIn_WithNullItem_ShouldTreatUnknownAsFalseInWhere()
    {
        const string query = "select a.City from #A.Entities() a where a.City in ('WARSAW', null)";

        var table = CreateAndRunVirtualMachine(query, CreateSources()).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("a.City", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["WARSAW"]);
    }

    [TestMethod]
    public void LiteralNotIn_WithNullItem_ShouldTreatUnknownAsFalseInWhere()
    {
        const string query = "select a.City from #A.Entities() a where a.City not in ('WARSAW', null)";

        var table = CreateAndRunVirtualMachine(query, CreateSources()).Run(TestContext.CancellationToken);

        Assert.AreEqual(0, table.Count);
    }

    [TestMethod]
    public void CollectionIn_WithNullItem_ShouldTreatUnknownAsFalseInWhere()
    {
        const string query = "param(keys: string[]) select a.City from #A.Entities() a where a.City in $keys";
        var vm = CreateAndRunVirtualMachine(query, CreateSources());
        vm.Parameters["keys"] = new string?[] { "WARSAW", null };

        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("a.City", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["WARSAW"]);
    }

    [TestMethod]
    public void CollectionNotIn_WithNullItem_ShouldTreatUnknownAsFalseInWhere()
    {
        const string query = "param(keys: string[]) select a.City from #A.Entities() a where a.City not in $keys";
        var vm = CreateAndRunVirtualMachine(query, CreateSources());
        vm.Parameters["keys"] = new string?[] { "WARSAW", null };

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(0, table.Count);
    }

    [TestMethod]
    public void ExistsInOrderBy_ShouldOrderByPerOuterRow()
    {
        const string query = """
            select a.City
            from #A.Entities() a
            order by exists (
                select b.City from #B.Entities() b where b.City = a.City
            ) desc, a.City
            """;

        var sources = CreateSources();
        sources["#A"] = sources["#A"].Where(entity => entity.City != null).ToArray();
        var table = CreateAndRunVirtualMachine(query, sources).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("a.City", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["PARIS"], ["WARSAW"], ["BERLIN"]);
    }

    [TestMethod]
    public void InSubqueryWithMultipleColumns_ShouldReportExactDiagnosticContract()
    {
        const string query = "select a.City from #A.Entities() a where a.City in (select b.City, b.Country from #B.Entities() b)";
        var exception = Assert.Throws<MusoqQueryException>(() => CreateAndRunVirtualMachine(query, CreateSources()));
        var envelope = exception.PrimaryEnvelope;
        var expectedStart = query.IndexOf("City", query.IndexOf("a.City in", StringComparison.Ordinal), StringComparison.Ordinal);
        var expectedEnd = query.LastIndexOf(')');

        Assert.AreEqual(DiagnosticCode.MQ3049_InSubqueryMultipleColumns, envelope.Code);
        Assert.AreEqual(DiagnosticPhase.Bind, envelope.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, envelope.SourceKind);
        Assert.AreEqual(expectedStart, envelope.Offset);
        Assert.AreEqual(expectedEnd - expectedStart + 1, envelope.Length);
        StringAssert.Contains(envelope.Message, "Subquery used with IN must return exactly one column.");
        Assert.IsFalse(string.IsNullOrWhiteSpace(envelope.Snippet));
        Assert.AreEqual(
            "An IN subquery must return exactly one column so Musoq can match it against the left-hand expression.",
            envelope.Explanation);
        Assert.AreEqual("Core Spec - IN Subqueries", envelope.DocsReference);
        CollectionAssert.AreEqual(
            new[]
            {
                "Remove extra columns from the subquery SELECT list so it returns a single column.",
                "Use separate IN conditions combined with AND when filtering multiple columns."
            },
            envelope.SuggestedFixes.ToArray());
        Assert.HasCount(2, envelope.Actions);
        Assert.IsTrue(envelope.Actions.All(static action =>
            action.Kind == DiagnosticActionKind.Suggestion && action.TextEdit is null));
        Assert.HasCount(0, envelope.Arguments);
    }

    [TestMethod]
    public void CorrelatedNonEqualityInInSelectExpression_ShouldReportUnsupportedFallback()
    {
        const string query = """
            select a.City,
                   a.City in (
                       select b.City from #B.Entities() b
                       where b.Population < a.Population
                   ) as HasMatch
            from #A.Entities() a
            """;

        var exception = Assert.Throws<MusoqQueryException>(() => CreateAndRunVirtualMachine(query, CreateSources()));
        var envelope = exception.PrimaryEnvelope;
        var expectedStart = query.IndexOf("City", query.IndexOf("a.City in", StringComparison.Ordinal), StringComparison.Ordinal);
        var correlatedReferenceStart = query.IndexOf("a.Population", query.IndexOf("where b.Population", StringComparison.Ordinal), StringComparison.Ordinal);
        var expectedEnd = query.IndexOf(')', correlatedReferenceStart);

        AssertErrorEnvelope(exception, DiagnosticCode.MQ2024_InvalidSubquery, DiagnosticPhase.Parse);
        Assert.AreEqual(DiagnosticSourceKind.Query, envelope.SourceKind);
        Assert.AreEqual(expectedStart, envelope.Offset);
        Assert.AreEqual(expectedEnd - expectedStart + 1, envelope.Length);
        StringAssert.Contains(envelope.Message, "requires an equality correlation key");
        Assert.AreEqual(
            "A subquery is malformed or appears in a location where this parser path cannot accept it.",
            envelope.Explanation);
        Assert.AreEqual("Core Spec - Subqueries", envelope.DocsReference);
        CollectionAssert.AreEqual(
            new[]
            {
                "Ensure the subquery starts with SELECT and is enclosed in parentheses.",
                "Use the subquery only in a supported expression or source position."
            },
            envelope.SuggestedFixes.ToArray());
        Assert.HasCount(2, envelope.Actions);
        Assert.IsTrue(envelope.Actions.All(static action =>
            action.Kind == DiagnosticActionKind.Suggestion && action.TextEdit is null));
        Assert.HasCount(0, envelope.Arguments);
        AssertHasGuidance(exception);
    }

    [TestMethod]
    public void HavingCorrelatedPredicateOnNonGroupedRow_ShouldReportExactInvalidSubqueryContract()
    {
        const string query = """
            select a.Country, Count(a.City) as CityCount
            from #A.Entities() a
            group by a.Country
            having exists (
                select b.City from #B.Entities() b
                where b.City = a.City
            )
            """;

        var exception = Assert.Throws<MusoqQueryException>(() => CreateAndRunVirtualMachine(query, CreateSources()));
        var envelope = exception.PrimaryEnvelope;
        var expectedStart = query.IndexOf("City", query.IndexOf("select b.City", StringComparison.Ordinal), StringComparison.Ordinal);
        var correlatedReferenceStart = query.IndexOf("a.City", query.IndexOf("where b.City", StringComparison.Ordinal), StringComparison.Ordinal);
        var expectedEnd = correlatedReferenceStart + "a.City".Length - 1;

        Assert.AreEqual(DiagnosticCode.MQ2024_InvalidSubquery, envelope.Code);
        Assert.AreEqual(DiagnosticPhase.Parse, envelope.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, envelope.SourceKind);
        Assert.AreEqual(expectedStart, envelope.Offset);
        Assert.AreEqual(expectedEnd - expectedStart + 1, envelope.Length);
        StringAssert.Contains(envelope.Message, "HAVING");
        StringAssert.Contains(envelope.Message, "grouping keys");
        StringAssert.Contains(envelope.Message, "non-grouped row values");
        Assert.AreEqual(
            "A subquery is malformed or appears in a location where this parser path cannot accept it.",
            envelope.Explanation);
        Assert.AreEqual("Core Spec - Subqueries", envelope.DocsReference);
        CollectionAssert.AreEqual(
            new[]
            {
                "Ensure the subquery starts with SELECT and is enclosed in parentheses.",
                "Use the subquery only in a supported expression or source position."
            },
            envelope.SuggestedFixes.ToArray());
        Assert.HasCount(2, envelope.Actions);
        Assert.IsTrue(envelope.Actions.All(static action =>
            action.Kind == DiagnosticActionKind.Suggestion && action.TextEdit is null));
        Assert.HasCount(0, envelope.Arguments);
        AssertHasGuidance(exception);
        Assert.IsFalse(envelope.Actions.Any(action => action.TextEdit != null));
    }

    private static Dictionary<string, IEnumerable<BasicEntity>> CreateSources()
    {
        return new Dictionary<string, IEnumerable<BasicEntity>>
        {
            ["#A"] =
            [
                new BasicEntity { City = "WARSAW", Country = "POLAND", Population = 500m },
                new BasicEntity { City = "BERLIN", Country = "GERMANY", Population = 250m },
                new BasicEntity { City = "PARIS", Country = "FRANCE", Population = 300m },
                new BasicEntity { City = null, Country = null, Population = 100m }
            ],
            ["#B"] =
            [
                new BasicEntity { City = "WARSAW", Country = "POLAND", Population = 100m },
                new BasicEntity { City = "PARIS", Country = "FRANCE", Population = 450m }
            ]
        };
    }
}
