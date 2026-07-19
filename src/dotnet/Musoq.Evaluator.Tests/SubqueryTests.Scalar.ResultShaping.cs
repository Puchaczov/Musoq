using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

public partial class SubqueryTests
{
    [TestMethod]
    public void WhenCorrelatedScalarSubquery_IsDistinct_ShouldDeduplicateInsideEachCorrelationKey()
    {
        const string query = @"
            SELECT a.City, (
                SELECT DISTINCT b.Country FROM #B.entities() b
                WHERE b.Country = a.Country
            ) AS MatchCountry
            FROM #A.entities() a";

        var table = CreateAndRunVirtualMachine(query, CreateScalarSources()).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.City", typeof(string)),
            ("MatchCountry", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["WARSAW", "POLAND"],
            new object?[] { "BERLIN", null },
            ["PARIS", "FRANCE"]);
    }

    [TestMethod]
    public void WhenCorrelatedScalarSubquery_DistinctKeepsMultipleValues_ShouldThrow()
    {
        const string query = @"
            SELECT a.City, (
                SELECT DISTINCT b.City FROM #B.entities() b
                WHERE b.Country = a.Country
            ) AS MatchCity
            FROM #A.entities() a";

        var inspection = CompileSubqueryForInspection(query);
        Assert.Contains("__CorrelatedScalarSubqueryValue", inspection.PhysicalPlanText, inspection.PhysicalPlanText);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            _ = CreateAndRunVirtualMachine(query, CreateScalarSources())
                .Run(TestContext.CancellationToken)
                .Count);

        Assert.AreEqual("Scalar subquery returned more than one row.", exception.Message);
    }

    [TestMethod]
    public void WhenCorrelatedScalarSubquery_HasGroupByAndHaving_ShouldShapeEachCorrelationKey()
    {
        const string query = @"
            SELECT a.City, (
                SELECT Max(b.Population) FROM #B.entities() b
                WHERE b.Country = a.Country
                GROUP BY b.Country
                HAVING Max(b.Population) > 105
            ) AS LargestPopulation
            FROM #A.entities() a";

        var table = CreateAndRunVirtualMachine(query, CreateScalarSources()).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.City", typeof(string)),
            ("LargestPopulation", typeof(decimal?)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["WARSAW", 110m],
            new object?[] { "BERLIN", null },
            ["PARIS", 450m]);
    }

    [TestMethod]
    public void WhenCorrelatedScalarSubquery_GroupByKeepsMultipleGroups_ShouldThrow()
    {
        const string query = @"
            SELECT a.City, (
                SELECT Max(b.Population) FROM #B.entities() b
                WHERE b.Country = a.Country
                GROUP BY b.City
            ) AS PopulationByCity
            FROM #A.entities() a";

        var exception = Assert.Throws<InvalidOperationException>(() =>
            _ = CreateAndRunVirtualMachine(query, CreateScalarSources())
                .Run(TestContext.CancellationToken)
                .Count);

        Assert.AreEqual("Scalar subquery returned more than one row.", exception.Message);
    }

    [TestMethod]
    public void WhenCorrelatedScalarSubquery_HasQualify_ShouldPartitionWindowByCorrelationKey()
    {
        const string query = @"
            SELECT a.City, (
                SELECT b.City FROM #B.entities() b
                WHERE b.Country = a.Country
                QUALIFY RowNumber() OVER (ORDER BY b.Population DESC) = 1
            ) AS MatchCity
            FROM #A.entities() a";

        var table = CreateAndRunVirtualMachine(query, CreateScalarSources()).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.City", typeof(string)),
            ("MatchCity", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["WARSAW", "GDANSK"],
            new object?[] { "BERLIN", null },
            ["PARIS", "PARIS"]);
    }

    [TestMethod]
    public void WhenCorrelatedScalarSubquery_UsesNamedWindow_ShouldPartitionDefinitionByCorrelationKey()
    {
        const string query = @"
            SELECT a.City, (
                SELECT b.City FROM #B.entities() b
                WHERE b.Country = a.Country
                WINDOW ranked AS (ORDER BY b.Population DESC)
                QUALIFY RowNumber() OVER ranked = 1
            ) AS MatchCity
            FROM #A.entities() a";

        var table = CreateAndRunVirtualMachine(query, CreateScalarSources()).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.City", typeof(string)),
            ("MatchCity", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["WARSAW", "GDANSK"],
            new object?[] { "BERLIN", null },
            ["PARIS", "PARIS"]);
    }

    [TestMethod]
    public void WhenCorrelatedScalarSubquery_SelectsWindowValue_ShouldPartitionInlineWindowByCorrelationKey()
    {
        const string query = @"
            SELECT a.City, (
                SELECT FirstValue(b.City) OVER (ORDER BY b.Population DESC)
                FROM #B.entities() b
                WHERE b.Country = a.Country
                QUALIFY RowNumber() OVER (ORDER BY b.Population DESC) = 1
            ) AS MatchCity
            FROM #A.entities() a";

        var table = CreateAndRunVirtualMachine(query, CreateScalarSources()).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.City", typeof(string)),
            ("MatchCity", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["WARSAW", "GDANSK"],
            new object?[] { "BERLIN", null },
            ["PARIS", "PARIS"]);
    }

    [TestMethod]
    public void WhenCorrelatedScalarSubquery_CombinesDistinctAndTake_ShouldKeepExplicitDiagnostic()
    {
        const string query = @"
            SELECT a.City, (
                SELECT DISTINCT b.Country FROM #B.entities() b
                WHERE b.Country = a.Country
                TAKE 1
            ) AS MatchCountry
            FROM #A.entities() a";

        var exception = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachine(query, CreateScalarSources()));

        Assert.IsTrue(exception.Envelopes.Any(envelope => envelope.Code == DiagnosticCode.MQ2024_InvalidSubquery));
        Assert.Contains("post-shaping partition stage", exception.Message);
    }
}
