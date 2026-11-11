using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Exceptions;
using Musoq.Parser.Diagnostics;
using static Musoq.Evaluator.Tests.MusoqExceptionAssertions;

namespace Musoq.Evaluator.Tests;

public partial class SubqueryTests
{
    [TestMethod]
    public void WhenCorrelatedScalarSubquery_UsesUnion_ShouldDeduplicatePerCorrelationKey()
    {
        const string query = (
            "\n"
            + "            SELECT a.City, (\n"
            + "                SELECT b.City FROM #B.entities() b\n"
            + "                WHERE b.Country = a.Country AND (b.City = 'KRAKOW' OR b.City = 'PARIS')\n"
            + "                UNION (City)\n"
            + "                SELECT c.City FROM #C.entities() c\n"
            + "                WHERE c.Country = a.Country\n"
            + "            ) AS MatchCity\n"
            + "            FROM #A.entities() a"
        );

        var table = CreateAndRunVirtualMachine(query, CreateScalarSources()).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("City", typeof(string)), ("MatchCity", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["WARSAW", "KRAKOW"],
            new object?[] { "BERLIN", null },
            ["PARIS", "PARIS"]
        );
    }

    [TestMethod]
    public void WhenCorrelatedScalarSubquery_UsesUnionAll_ShouldPreserveDuplicateCardinality()
    {
        const string query = (
            "\n"
            + "            SELECT a.City, (\n"
            + "                SELECT b.City FROM #B.entities() b\n"
            + "                WHERE b.Country = a.Country AND b.City = 'KRAKOW'\n"
            + "                UNION ALL (City)\n"
            + "                SELECT c.City FROM #C.entities() c\n"
            + "                WHERE c.Country = a.Country\n"
            + "            ) AS MatchCity\n"
            + "            FROM #A.entities() a"
        );

        var exception = Assert.Throws<QueryExecutionException>(() =>
            _ = CreateAndRunVirtualMachine(query, CreateScalarSources()).Run(TestContext.CancellationToken).Count
        );

        AssertRuntimeError(exception, DiagnosticCode.MQ7016_ScalarSubqueryCardinalityViolation);
    }

    [TestMethod]
    public void WhenCorrelatedScalarSubquery_UsesExcept_ShouldSubtractInsideEachCorrelationKey()
    {
        const string query = (
            "\n"
            + "            SELECT a.City, (\n"
            + "                SELECT b.City FROM #B.entities() b\n"
            + "                WHERE b.Country = a.Country\n"
            + "                EXCEPT (City)\n"
            + "                SELECT c.City FROM #C.entities() c\n"
            + "                WHERE c.Country = a.Country\n"
            + "            ) AS MatchCity\n"
            + "            FROM #A.entities() a"
        );

        var table = CreateAndRunVirtualMachine(query, CreateScalarSources()).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("City", typeof(string)), ("MatchCity", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["WARSAW", "GDANSK"],
            new object?[] { "BERLIN", null },
            new object?[] { "PARIS", null }
        );
    }

    [TestMethod]
    public void WhenCorrelatedScalarSubquery_UsesIntersect_ShouldIntersectInsideEachCorrelationKey()
    {
        const string query = (
            "\n"
            + "            SELECT a.City, (\n"
            + "                SELECT b.City FROM #B.entities() b\n"
            + "                WHERE b.Country = a.Country\n"
            + "                INTERSECT (City)\n"
            + "                SELECT c.City FROM #C.entities() c\n"
            + "                WHERE c.Country = a.Country\n"
            + "            ) AS MatchCity\n"
            + "            FROM #A.entities() a"
        );

        var table = CreateAndRunVirtualMachine(query, CreateScalarSources()).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("City", typeof(string)), ("MatchCity", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["WARSAW", "KRAKOW"],
            new object?[] { "BERLIN", null },
            ["PARIS", "PARIS"]
        );
    }

    [TestMethod]
    public void WhenCorrelatedScalarSetBranches_UseDifferentOuterKeys_ShouldEnforceRuntimeCardinality()
    {
        const string query = (
            "\n"
            + "            SELECT a.City, (\n"
            + "                SELECT b.City FROM #B.entities() b\n"
            + "                WHERE b.Country = a.Country\n"
            + "                UNION (City)\n"
            + "                SELECT c.City FROM #C.entities() c\n"
            + "                WHERE c.City = a.City\n"
            + "            ) AS MatchCity\n"
            + "            FROM #A.entities() a"
        );

        using var compiled = CreateAndRunVirtualMachine(query, CreateScalarSources());
        var exception = Assert.Throws<QueryExecutionException>(() =>
            _ = compiled.Run(TestContext.CancellationToken).Count
        );
        AssertRuntimeError(exception, DiagnosticCode.MQ7016_ScalarSubqueryCardinalityViolation);
    }
}
