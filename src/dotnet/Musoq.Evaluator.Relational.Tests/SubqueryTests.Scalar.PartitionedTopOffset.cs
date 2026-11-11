using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public partial class SubqueryTests
{
    [TestMethod]
    public void WhenCorrelatedScalarSubquery_HasSkipAndTake_ShouldApplyOffsetPerCorrelationKey()
    {
        const string query = (
            "\n"
            + "            SELECT a.City, (\n"
            + "                SELECT b.City FROM #B.entities() b\n"
            + "                WHERE b.Country = a.Country\n"
            + "                ORDER BY b.Population DESC\n"
            + "                SKIP 1 TAKE 1\n"
            + "            ) AS MatchCity\n"
            + "            FROM #A.entities() a"
        );

        var table = CreateAndRunVirtualMachine(query, CreateScalarSources()).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["WARSAW", "KRAKOW"],
            new object?[] { "BERLIN", null },
            new object?[] { "PARIS", null }
        );
    }

    [TestMethod]
    public void WhenCorrelatedScalarSubquery_TakesZeroRows_ShouldReturnNullPerCorrelationKey()
    {
        const string query = (
            "\n"
            + "            SELECT a.City, (\n"
            + "                SELECT b.City FROM #B.entities() b\n"
            + "                WHERE b.Country = a.Country\n"
            + "                ORDER BY b.Population DESC\n"
            + "                TAKE 0\n"
            + "            ) AS MatchCity\n"
            + "            FROM #A.entities() a"
        );

        var table = CreateAndRunVirtualMachine(query, CreateScalarSources()).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            new object?[] { "WARSAW", null },
            new object?[] { "BERLIN", null },
            new object?[] { "PARIS", null }
        );
    }
}
