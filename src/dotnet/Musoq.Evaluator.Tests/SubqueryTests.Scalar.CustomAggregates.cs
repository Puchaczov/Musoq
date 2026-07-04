using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public partial class SubqueryTests
{
    [TestMethod]
    public void WhenScalarSubquery_HasCorrelatedCustomAggregate_ShouldGroupByCorrelationKey()
    {
        const string query = @"
            SELECT a.City, (
                SELECT CustomRowCount() FROM #B.entities() b
                WHERE b.Country = a.Country
            ) AS CountryRows
            FROM #A.entities() a";

        var table = CreateAndRunVirtualMachine(query, CreateScalarSources()).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.City", typeof(string)),
            ("CountryRows", typeof(long?)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["WARSAW", 2L],
            new object?[] { "BERLIN", null },
            ["PARIS", 1L]);

        var inspection = CompileSubqueryForInspection(query);
        Assert.Contains("PhysicalHashJoin [LeftOuter]", inspection.PhysicalPlanText);
        Assert.Contains("_sq_1_corr_0", inspection.PhysicalPlanText);
    }
}
