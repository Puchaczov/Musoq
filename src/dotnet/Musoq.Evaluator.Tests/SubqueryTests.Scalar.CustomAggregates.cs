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
            ["BERLIN", 0L],
            ["PARIS", 1L]);

        var inspection = CompileSubqueryForInspection(query);
        Assert.Contains("PhysicalHashJoin [LeftSingle]", inspection.PhysicalPlanText);
        Assert.Contains("-> ScalarHashSingle", inspection.PlanningText);
        Assert.Contains("_sq_1_corr_0", inspection.PhysicalPlanText);
    }

    [TestMethod]
    public void WhenScalarSubquery_HasCorrelatedCustomEmptyAggregate_ShouldUseKernelEmptyResult()
    {
        const string query = @"
            SELECT a.City, (
                SELECT AggregateValues(b.City) FROM #B.entities() b
                WHERE b.Country = a.Country
            ) AS CountryCities
            FROM #A.entities() a";

        var table = CreateAndRunVirtualMachine(query, CreateScalarSources()).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.City", typeof(string)),
            ("CountryCities", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["WARSAW", "KRAKOW,GDANSK"],
            ["BERLIN", string.Empty],
            ["PARIS", "PARIS"]);

        var inspection = CompileSubqueryForInspection(query);
        Assert.Contains("PhysicalHashJoin [LeftSingle]", inspection.PhysicalPlanText);
        Assert.Contains(
            "Musoq.Plugins.AggregateValuesStringKernel.Get(_sq_1HashEmptyState)",
            inspection.GeneratedCSharpCode);
    }

    [TestMethod]
    public void WhenScalarSubquery_HasCorrelatedCount_ShouldReturnZeroForMissingGroup()
    {
        const string query = @"
            SELECT a.City, (
                SELECT Count(b.City) FROM #B.entities() b
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
            ["BERLIN", 0L],
            ["PARIS", 1L]);
    }
}
