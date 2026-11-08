using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class Core024PivotUnpivotTests : BasicEntityTestBase
{
    [TestMethod]
    public void Pivot_MultiColumnKeyWithNullComponent_ShouldMatchTupleBucket()
    {
        const string query = "pivot #A.Entities() on Id, Country in ((0, null) as Missing, (1, 'PL') as Known) using Count(*) as Orders group by City";

        var table = CreateAndRunVirtualMachine(query, CreateSingleSource(
            new BasicEntity { City = "GDA", Id = 0, Country = null },
            new BasicEntity { City = "GDA", Id = 1, Country = "PL" },
            new BasicEntity { City = "GDA", Id = 0, Country = "US" })).Run();

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("City", typeof(string)),
            ("Missing", typeof(long)),
            ("Known", typeof(long)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["GDA", 1L, 1L]);
    }

    [TestMethod]
    public void Pivot_UnmatchedStaticBucket_ShouldRemainNull()
    {
        const string query = "pivot #A.Entities() on Month in ('Jan' as Jan, 'Missing' as Missing) using Sum(Money) as Sales group by City";

        var table = CreateAndRunVirtualMachine(query, CreateSingleSource(
            new BasicEntity { City = "GDA", Month = "Jan", Money = 10m })).Run();

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("City", typeof(string)),
            ("Jan", typeof(decimal?)),
            ("Missing", typeof(decimal?)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            new object?[] { "GDA", 10m, null });
    }

    [TestMethod]
    public void Unpivot_EntriesShouldPreserveWrittenExpansionOrder()
    {
        const string query = "unpivot #A.Entities() s on Metric in (s.Population as Population, s.Money as Money) using Amount";

        var table = CreateAndRunVirtualMachine(query, CreateSingleSource(
            new BasicEntity { Population = 10m, Money = 1.5m })).Run();

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Metric", typeof(string)),
            ("Amount", typeof(decimal)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["Population", 10m],
            ["Money", 1.5m]);
    }

    [TestMethod]
    public void Unpivot_WithoutKeep_ShouldProjectOnlyGeneratedColumns()
    {
        const string query = "unpivot #A.Entities() s on Metric in (s.Name as Name, s.City as City) using Value";

        var table = CreateAndRunVirtualMachine(query, CreateSingleSource(
            new BasicEntity { Name = "Alice", City = "GDA" })).Run();

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Metric", typeof(string)),
            ("Value", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["Name", "Alice"],
            ["City", "GDA"]);
    }

    [TestMethod]
    public void Unpivot_MixedNumericEntriesWithNull_ShouldWidenToNullableDecimal()
    {
        const string query = "unpivot #A.Entities() s on Metric in (s.Id as Id, s.Population as Population, null as Missing) using Value";

        var table = CreateAndRunVirtualMachine(query, CreateSingleSource(
            new BasicEntity { Id = 7, Population = 2.5m })).Run();

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Metric", typeof(string)),
            ("Value", typeof(decimal?)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            new object?[] { "Id", 7m },
            new object?[] { "Population", 2.5m },
            new object?[] { "Missing", null });
    }

    [TestMethod]
    public void PivotUsing_NonAggregate_ShouldReportExactDiagnosticEnvelope()
    {
        const string query = "pivot #A.Entities() on Month in ('Jan' as Jan) using ToUpper(Name) as Name";
        var diagnostic = AssertSingleError(Analyze(query), DiagnosticCode.MQ3051_FilterOnNonAggregate);
        var functionStart = query.IndexOf("ToUpper", StringComparison.Ordinal);

        Assert.AreEqual(DiagnosticPhase.Bind, diagnostic.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, diagnostic.SourceKind);
        Assert.AreEqual(
            "PIVOT USING accepts aggregate function calls only, but 'ToUpper' is not an aggregate function.",
            diagnostic.Message);
        Assert.AreEqual(new TextSpan(functionStart, "ToUpper".Length), diagnostic.Span);
        Assert.IsFalse(string.IsNullOrWhiteSpace(diagnostic.ContextSnippet));

        var envelope = MusoqErrorEnvelope.FromDiagnostic(diagnostic, query);
        Assert.AreEqual(DiagnosticCode.MQ3051_FilterOnNonAggregate, envelope.Code);
        Assert.AreEqual("The FILTER clause can only be applied to aggregate functions.", envelope.Explanation);
        Assert.AreEqual("Core Spec - FILTER", envelope.DocsReference);
        Assert.IsNotEmpty(envelope.SuggestedFixes);
    }

    [TestMethod]
    public void Unpivot_IncompatibleValueTypes_ShouldReportExactDiagnostic()
    {
        const string query = "unpivot #A.Entities() s on Metric in (s.Id as Id, s.Name as Name) using Value";
        var diagnostic = AssertSingleError(Analyze(query), DiagnosticCode.MQ3055_InvalidValuesSource);

        Assert.AreEqual(DiagnosticPhase.Bind, diagnostic.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, diagnostic.SourceKind);
        Assert.AreEqual(
            "UNPIVOT value column 'Value' mixes incompatible types: Int32, String. Use consistent expression types or explicit conversion functions.",
            diagnostic.Message);
        Assert.AreEqual(
            new TextSpan(query.IndexOf("Name", StringComparison.Ordinal), "Name".Length),
            diagnostic.Span);
        Assert.IsFalse(string.IsNullOrWhiteSpace(diagnostic.ContextSnippet));
    }

    private static QueryAnalysisResult Analyze(string query)
    {
        return new QueryAnalyzer(new BasicSchemaProvider<BasicEntity>(
            new Dictionary<string, IEnumerable<BasicEntity>> { ["#A"] = [] })).Analyze(query);
    }

    private static Diagnostic AssertSingleError(QueryAnalysisResult result, DiagnosticCode expectedCode)
    {
        Assert.IsFalse(result.IsSuccess, FormatDiagnostics(result));
        var errors = result.Errors.ToArray();
        Assert.HasCount(1, errors, FormatDiagnostics(result));
        Assert.AreEqual(expectedCode, errors[0].Code);
        return errors[0];
    }

    private static string FormatDiagnostics(QueryAnalysisResult result)
    {
        return string.Join(" | ", result.Diagnostics.Select(static diagnostic =>
            $"{diagnostic.Code}: {diagnostic.Message}"));
    }
}
