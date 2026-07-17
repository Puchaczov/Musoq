using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public partial class SubqueryTests
{
    [TestMethod]
    public void WhenCorrelatedScalarSubquery_HasSkipAndTake_ShouldApplyOffsetPerCorrelationKey()
    {
        const string query = @"
            SELECT a.City, (
                SELECT b.City FROM #B.entities() b
                WHERE b.Country = a.Country
                ORDER BY b.Population DESC
                SKIP 1 TAKE 1
            ) AS MatchCity
            FROM #A.entities() a";

        var table = CreateAndRunVirtualMachine(query, CreateScalarSources()).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["WARSAW", "KRAKOW"],
            new object?[] { "BERLIN", null },
            new object?[] { "PARIS", null });
    }

    [TestMethod]
    public void WhenCorrelatedScalarSubquery_TakesZeroRows_ShouldReturnNullPerCorrelationKey()
    {
        const string query = @"
            SELECT a.City, (
                SELECT b.City FROM #B.entities() b
                WHERE b.Country = a.Country
                ORDER BY b.Population DESC
                TAKE 0
            ) AS MatchCity
            FROM #A.entities() a";

        var table = CreateAndRunVirtualMachine(query, CreateScalarSources()).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            new object?[] { "WARSAW", null },
            new object?[] { "BERLIN", null },
            new object?[] { "PARIS", null });
    }
}
