using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator.Tables;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;
using static Musoq.Evaluator.Tests.MusoqExceptionAssertions;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class Core029NonRecursiveCteTests : BasicEntityTestBase
{
    [TestMethod]
    public void CteDefinitions_ShouldResolveEarlierDeclarationsAndExportRenamedColumns()
    {
        const string query = "with base_rows as (select City, Country from #A.Entities()), " +
                             "filtered as (select City from base_rows where Country = 'POLAND'), " +
                             "renamed as (select City as Place from filtered) " +
                             "select Place from renamed";

        var table = Run(
            query,
            CreateSingleSource(
                new BasicEntity { City = "WARSAW", Country = "POLAND" },
                new BasicEntity { City = "BERLIN", Country = "GERMANY" }));

        TableMaterializationTestHelper.AssertColumns(table, ("Place", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["WARSAW"]);
    }

    [TestMethod]
    public void ForwardCteReference_ShouldReportExactTableDiagnostic()
    {
        const string query = "with later as (select Name from earlier), " +
                             "earlier as (select Name from #A.Entities()) " +
                             "select Name from later";

        var diagnostic = AssertSingleError(Analyze(query), DiagnosticCode.MQ3023_TableNotDefined);

        Assert.AreEqual(DiagnosticPhase.Bind, diagnostic.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, diagnostic.SourceKind);
        Assert.AreEqual("Table 'earlier' is not defined in query", diagnostic.Message);
        Assert.AreEqual(SpanOf(query, "earlier"), diagnostic.Span);

        var envelope = MusoqErrorEnvelope.FromDiagnostic(diagnostic, query);
        Assert.AreEqual("Core Spec - FROM Clause", envelope.DocsReference);
        Assert.IsNotEmpty(envelope.SuggestedFixes);
        Assert.HasCount(envelope.SuggestedFixes.Count, envelope.Actions);
    }

    [TestMethod]
    public void CteProjection_ShouldUseExplicitAliasesAndExpandStarsFromExportedShape()
    {
        const string query = "with places as (select a.City as LeftCity, a.Country as Nation from #A.Entities() a) " +
                             "select * from places";

        var table = Run(
            query,
            CreateSingleSource(
                new BasicEntity { City = "WARSAW", Country = "POLAND" },
                new BasicEntity { City = "BERLIN", Country = "GERMANY" }));

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("LeftCity", typeof(string)),
            ("Nation", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["WARSAW", "POLAND"],
            ["BERLIN", "GERMANY"]);
    }

    [TestMethod]
    public void CteProjection_ShouldStripLocalQualifierUnlessDottedAliasIsExplicit()
    {
        const string query = "with places as (select a.City, a.Country as [a.Country] from #A.Entities() a) " +
                             "select City, [a.Country] from places";

        var table = Run(
            query,
            CreateSingleSource(new BasicEntity { City = "WARSAW", Country = "POLAND" }));

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("City", typeof(string)),
            ("a.Country", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["WARSAW", "POLAND"]);
    }

    [TestMethod]
    public void CteReferenceAlias_ShouldHideTheCteNameInTheCurrentQueryBlock()
    {
        const string query = "with places as (select City from #A.Entities()) " +
                             "select places.City from places p";

        var exception = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachine(query, CreateSingleSource(new BasicEntity { City = "WARSAW" })));

        AssertErrorEnvelope(exception, DiagnosticCode.MQ3015_UnknownAlias, DiagnosticPhase.Bind, "places");
        AssertHasGuidance(exception);
        Assert.AreEqual(DiagnosticSourceKind.Query, exception.PrimaryEnvelope.SourceKind);
        Assert.AreEqual(new TextSpan(query.IndexOf("places.City", StringComparison.Ordinal), "places".Length),
            new TextSpan(exception.PrimaryEnvelope.Offset!.Value, exception.PrimaryEnvelope.Length!.Value));
    }

    [TestMethod]
    public void CteColumnNamedLikeTheCte_ShouldResolveAsAnUnqualifiedColumn()
    {
        const string query = "with cte as (select City as cte from #A.Entities()) select cte from cte";

        var table = Run(query, CreateSingleSource(new BasicEntity { City = "WARSAW" }));

        TableMaterializationTestHelper.AssertColumns(table, ("cte", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["WARSAW"]);
    }

    [TestMethod]
    public void CteAggregation_ShouldWorkInsideTheDefinitionAndOnTheCteReference()
    {
        var sources = CreateSingleSource(
            new BasicEntity { City = "WARSAW", Country = "POLAND", Population = 500m },
            new BasicEntity { City = "KRAKOW", Country = "POLAND", Population = 300m },
            new BasicEntity { City = "BERLIN", Country = "GERMANY", Population = 250m });

        var inside = Run(
            "with summary as (select Country, Sum(Population) as Total from #A.Entities() group by Country) " +
            "select Country, Total from summary order by Country",
            sources);
        var outside = Run(
            "with raw as (select Population, Country from #A.Entities()) " +
            "select Country, Sum(Population) as Total from raw group by Country order by Country",
            sources);

        TableMaterializationTestHelper.AssertRowsInOrder(
            inside,
            ["GERMANY", 250m],
            ["POLAND", 800m]);
        TableMaterializationTestHelper.AssertRowsInOrder(
            outside,
            ["GERMANY", 250m],
            ["POLAND", 800m]);
    }

    [TestMethod]
    public void MultipleCtes_WithSetOperationAndJoin_ShouldComposeUsingExportedColumns()
    {
        const string query = "with left_rows as (select Id, City from #A.Entities()), " +
                             "right_rows as (select Id, City from #B.Entities()), " +
                             "combined as (select Id, City from left_rows union all (Id, City) " +
                             "select Id, City from right_rows) " +
                             "select c.City, r.City as MatchedCity from combined c " +
                             "inner join right_rows r on c.Id = r.Id order by c.City";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            ["#A"] =
            [
                new BasicEntity { Id = 1, City = "ALPHA" },
                new BasicEntity { Id = 2, City = "BETA" }
            ],
            ["#B"] =
            [
                new BasicEntity { Id = 2, City = "BETA-RIGHT" },
                new BasicEntity { Id = 3, City = "GAMMA" }
            ]
        };

        var table = Run(query, sources);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("c.City", typeof(string)),
            ("MatchedCity", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["BETA", "BETA-RIGHT"],
            ["BETA-RIGHT", "BETA-RIGHT"],
            ["GAMMA", "GAMMA"]);
    }

    [TestMethod]
    public void CteBodySubquery_ShouldCorrelateOnlyToItsBodyLocalSource()
    {
        const string query = "with matched as (" +
                             "select a.City from #A.Entities() a where exists (" +
                             "select b.City from #B.Entities() b where b.Country = a.Country)) " +
                             "select City from matched order by City";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            ["#A"] =
            [
                new BasicEntity { City = "WARSAW", Country = "POLAND" },
                new BasicEntity { City = "BERLIN", Country = "GERMANY" },
                new BasicEntity { City = "PARIS", Country = "FRANCE" }
            ],
            ["#B"] =
            [new BasicEntity { City = "KRAKOW", Country = "POLAND" }]
        };

        var table = Run(query, sources);

        TableMaterializationTestHelper.AssertColumns(table, ("City", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["WARSAW"]);
    }

    [TestMethod]
    public void CteDefinitionReferencingConsumerAlias_ShouldReportExactInvalidSubqueryContract()
    {
        const string query = "with matched as (" +
                             "select b.City from #B.Entities() b where b.Country = a.Country) " +
                             "select a.City from #A.Entities() a " +
                             "where a.City in (select City from matched)";

        var exception = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachine(query, new Dictionary<string, IEnumerable<BasicEntity>>
            {
                ["#A"] = [],
                ["#B"] = []
            }));

        AssertErrorEnvelope(
            exception,
            DiagnosticCode.MQ3015_UnknownAlias,
            DiagnosticPhase.Bind,
            "Unknown alias 'a'.");
        AssertHasGuidance(exception);
        Assert.AreEqual(DiagnosticSourceKind.Query, exception.PrimaryEnvelope.SourceKind);
        Assert.AreEqual(new TextSpan(query.IndexOf("a.Country", StringComparison.Ordinal), "a".Length),
            new TextSpan(exception.PrimaryEnvelope.Offset!.Value, exception.PrimaryEnvelope.Length!.Value));
        Assert.AreEqual("Core Spec - Aliasing", exception.PrimaryEnvelope.DocsReference);
        Assert.AreEqual("a", exception.PrimaryEnvelope.Arguments["alias"]);
    }

    [TestMethod]
    public void UnreachableCteChain_ShouldReportStructuredWarningsForEachDeadDefinition()
    {
        const string query = "with dead as (select Name from #A.Entities()), " +
                             "dead_dependency as (select Name from dead), " +
                             "live as (select Name from #A.Entities()) " +
                             "select Name from live";
        var result = Analyze(query);

        Assert.IsFalse(result.HasErrors, FormatDiagnostics(result));
        var warnings = result.Warnings
            .Where(static warning => warning.Code == DiagnosticCode.MQ5022_UnusedCte)
            .ToArray();
        Assert.HasCount(2, warnings);
        Assert.AreEqual("CTE 'dead' is not reachable from the outer query", warnings[0].Message);
        Assert.AreEqual("CTE 'dead_dependency' is not reachable from the outer query", warnings[1].Message);
        Assert.IsTrue(warnings.All(static warning =>
            warning.Severity == DiagnosticSeverity.Warning &&
            warning.Phase == DiagnosticPhase.Bind &&
            warning.SourceKind == DiagnosticSourceKind.Query &&
            !string.IsNullOrWhiteSpace(warning.ContextSnippet) &&
            !string.IsNullOrWhiteSpace(warning.Explanation) &&
            warning.SuggestedFixes.Count > 0),
            string.Join(" | ", warnings.Select(static warning =>
                $"{warning.Code} severity={warning.Severity} phase={warning.Phase} source={warning.SourceKind} " +
                $"snippet={warning.ContextSnippet} explanation={warning.Explanation} fixes={warning.SuggestedFixes.Count}")));
        Assert.AreEqual(SpanOf(query, "dead"), warnings[0].Span);
        Assert.AreEqual(SpanOf(query, "dead_dependency"), warnings[1].Span);
        Assert.IsTrue(warnings.All(static warning => warning.DocsReference == "Core Spec - Common Table Expressions"));
    }

    private Table Run(string query, IDictionary<string, IEnumerable<BasicEntity>> sources)
    {
        var vm = CreateAndRunVirtualMachine(query, sources);
        return TableMaterializationTestHelper.Materialize(vm.Run(TestContext.CancellationToken));
    }

    private static QueryAnalysisResult Analyze(string query)
    {
        return new QueryAnalyzer(new BasicSchemaProvider<BasicEntity>(
            new Dictionary<string, IEnumerable<BasicEntity>>
            {
                ["#A"] = [],
                ["#B"] = []
            })).Analyze(query);
    }

    private static Diagnostic AssertSingleError(QueryAnalysisResult result, DiagnosticCode expectedCode)
    {
        Assert.IsFalse(result.IsSuccess, FormatDiagnostics(result));
        var errors = result.Errors.ToArray();
        Assert.HasCount(1, errors, FormatDiagnostics(result));
        Assert.AreEqual(expectedCode, errors[0].Code);
        return errors[0];
    }

    private static TextSpan SpanOf(string query, string text)
    {
        return new TextSpan(query.IndexOf(text, StringComparison.Ordinal), text.Length);
    }

    private static string FormatDiagnostics(QueryAnalysisResult result)
    {
        return string.Join(" | ", result.Diagnostics.Select(static diagnostic =>
            $"{diagnostic.Code}: {diagnostic.Message} at {diagnostic.Span}"));
    }
}
