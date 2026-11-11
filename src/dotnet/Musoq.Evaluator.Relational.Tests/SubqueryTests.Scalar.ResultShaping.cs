using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Exceptions;
using Musoq.Parser.Diagnostics;
using static Musoq.Evaluator.Tests.MusoqExceptionAssertions;

namespace Musoq.Evaluator.Tests;

public partial class SubqueryTests
{
    [TestMethod]
    public void WhenCorrelatedScalarSubquery_IsDistinct_ShouldDeduplicateInsideEachCorrelationKey()
    {
        const string query = (
            "\n"
            + "            SELECT a.City, (\n"
            + "                SELECT DISTINCT b.Country FROM #B.entities() b\n"
            + "                WHERE b.Country = a.Country\n"
            + "            ) AS MatchCountry\n"
            + "            FROM #A.entities() a"
        );

        var table = CreateAndRunVirtualMachine(query, CreateScalarSources()).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("City", typeof(string)), ("MatchCountry", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["WARSAW", "POLAND"],
            new object?[] { "BERLIN", null },
            ["PARIS", "FRANCE"]
        );
    }

    [TestMethod]
    public void WhenCorrelatedScalarSubquery_DistinctKeepsMultipleValues_ShouldThrow()
    {
        const string query = (
            "\n"
            + "            SELECT a.City, (\n"
            + "                SELECT DISTINCT b.City FROM #B.entities() b\n"
            + "                WHERE b.Country = a.Country\n"
            + "            ) AS MatchCity\n"
            + "            FROM #A.entities() a"
        );

        var inspection = CompileSubqueryForInspection(query);
        Assert.Contains("__CorrelatedScalarSubqueryValue", inspection.PhysicalPlanText, inspection.PhysicalPlanText);

        var exception = Assert.Throws<QueryExecutionException>(() =>
            _ = CreateAndRunVirtualMachine(query, CreateScalarSources()).Run(TestContext.CancellationToken).Count
        );

        AssertRuntimeError(exception, DiagnosticCode.MQ7016_ScalarSubqueryCardinalityViolation);
    }

    [TestMethod]
    public void WhenCorrelatedScalarSubquery_HasGroupByAndHaving_ShouldShapeEachCorrelationKey()
    {
        const string query = (
            "\n"
            + "            SELECT a.City, (\n"
            + "                SELECT Max(b.Population) FROM #B.entities() b\n"
            + "                WHERE b.Country = a.Country\n"
            + "                GROUP BY b.Country\n"
            + "                HAVING Max(b.Population) > 105\n"
            + "            ) AS LargestPopulation\n"
            + "            FROM #A.entities() a"
        );

        var table = CreateAndRunVirtualMachine(query, CreateScalarSources()).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("City", typeof(string)),
            ("LargestPopulation", typeof(decimal?))
        );
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["WARSAW", 110m],
            new object?[] { "BERLIN", null },
            ["PARIS", 450m]
        );
    }

    [TestMethod]
    public void WhenCorrelatedScalarSubquery_GroupByKeepsMultipleGroups_ShouldThrow()
    {
        const string query = (
            "\n"
            + "            SELECT a.City, (\n"
            + "                SELECT Max(b.Population) FROM #B.entities() b\n"
            + "                WHERE b.Country = a.Country\n"
            + "                GROUP BY b.City\n"
            + "            ) AS PopulationByCity\n"
            + "            FROM #A.entities() a"
        );

        var exception = Assert.Throws<QueryExecutionException>(() =>
            _ = CreateAndRunVirtualMachine(query, CreateScalarSources()).Run(TestContext.CancellationToken).Count
        );

        AssertRuntimeError(exception, DiagnosticCode.MQ7016_ScalarSubqueryCardinalityViolation);
    }

    [TestMethod]
    public void WhenCorrelatedScalarSubquery_HasQualify_ShouldPartitionWindowByCorrelationKey()
    {
        const string query = (
            "\n"
            + "            SELECT a.City, (\n"
            + "                SELECT b.City FROM #B.entities() b\n"
            + "                WHERE b.Country = a.Country\n"
            + "                QUALIFY RowNumber() OVER (ORDER BY b.Population DESC) = 1\n"
            + "            ) AS MatchCity\n"
            + "            FROM #A.entities() a"
        );

        var table = CreateAndRunVirtualMachine(query, CreateScalarSources()).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("City", typeof(string)), ("MatchCity", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["WARSAW", "GDANSK"],
            new object?[] { "BERLIN", null },
            ["PARIS", "PARIS"]
        );
    }

    [TestMethod]
    public void WhenCorrelatedScalarSubquery_UsesNamedWindow_ShouldPartitionDefinitionByCorrelationKey()
    {
        const string query = (
            "\n"
            + "            SELECT a.City, (\n"
            + "                SELECT b.City FROM #B.entities() b\n"
            + "                WHERE b.Country = a.Country\n"
            + "                WINDOW ranked AS (ORDER BY b.Population DESC)\n"
            + "                QUALIFY RowNumber() OVER ranked = 1\n"
            + "            ) AS MatchCity\n"
            + "            FROM #A.entities() a"
        );

        var table = CreateAndRunVirtualMachine(query, CreateScalarSources()).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("City", typeof(string)), ("MatchCity", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["WARSAW", "GDANSK"],
            new object?[] { "BERLIN", null },
            ["PARIS", "PARIS"]
        );
    }

    [TestMethod]
    public void WhenCorrelatedScalarSubquery_SelectsWindowValue_ShouldPartitionInlineWindowByCorrelationKey()
    {
        const string query = (
            "\n"
            + "            SELECT a.City, (\n"
            + "                SELECT FirstValue(b.City) OVER (ORDER BY b.Population DESC)\n"
            + "                FROM #B.entities() b\n"
            + "                WHERE b.Country = a.Country\n"
            + "                QUALIFY RowNumber() OVER (ORDER BY b.Population DESC) = 1\n"
            + "            ) AS MatchCity\n"
            + "            FROM #A.entities() a"
        );

        var table = CreateAndRunVirtualMachine(query, CreateScalarSources()).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("City", typeof(string)), ("MatchCity", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["WARSAW", "GDANSK"],
            new object?[] { "BERLIN", null },
            ["PARIS", "PARIS"]
        );
    }

    [TestMethod]
    public void WhenCorrelatedScalarSubquery_CombinesDistinctAndTake_ShouldShapePerBinding()
    {
        const string query = (
            "\n"
            + "            SELECT a.City, (\n"
            + "                SELECT DISTINCT b.Country FROM #B.entities() b\n"
            + "                WHERE b.Country = a.Country\n"
            + "                TAKE 1\n"
            + "            ) AS MatchCountry\n"
            + "            FROM #A.entities() a"
        );

        using var compiled = CreateAndRunVirtualMachine(query, CreateScalarSources());
        using var table = compiled.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["WARSAW", "POLAND"],
            ["BERLIN", null],
            ["PARIS", "FRANCE"]
        );
    }
}
