using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class SubqueryTests
{
    [TestMethod]
    public void WhenEqualAnySubquery_ShouldUseExistingInSemiJoin()
    {
        const string query = @"
            SELECT a.City FROM #A.entities() a
            WHERE a.Country = ANY (
                SELECT b.Country FROM #B.entities() b
            )";

        var table = CreateAndRunVirtualMachine(query, CreateQuantifiedSources()).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("a.City", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["WARSAW"], ["PARIS"]);

        var inspection = CompileSubqueryForInspection(query);
        Assert.Contains("PhysicalHashJoin [LeftSemi]", inspection.PhysicalPlanText);
    }

    [TestMethod]
    public void WhenEqualSomeSubquery_ShouldBehaveLikeAny()
    {
        const string query = @"
            SELECT a.City FROM #A.entities() a
            WHERE a.Country = SOME (
                SELECT b.Country FROM #B.entities() b
            )";

        var table = CreateAndRunVirtualMachine(query, CreateQuantifiedSources()).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("a.City", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["WARSAW"], ["PARIS"]);
    }

    [TestMethod]
    public void WhenGreaterThanAnySubquery_ShouldReturnRowsWithAtLeastOneTrueComparison()
    {
        const string query = @"
            SELECT a.City FROM #A.entities() a
            WHERE a.Population > ANY (
                SELECT b.Population FROM #B.entities() b
            )";

        var table = CreateAndRunVirtualMachine(query, CreateQuantifiedSources()).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("a.City", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["WARSAW"], ["BERLIN"], ["PARIS"]);
    }

    [TestMethod]
    public void WhenGreaterThanAllSubquery_ShouldReturnRowsWhereEveryComparisonIsTrue()
    {
        const string query = @"
            SELECT a.City FROM #A.entities() a
            WHERE a.Population > ALL (
                SELECT b.Population FROM #B.entities() b
            )";

        var table = CreateAndRunVirtualMachine(query, CreateQuantifiedSources()).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("a.City", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["WARSAW"]);
    }

    [TestMethod]
    public void WhenAnySubquery_IsEmpty_ShouldReturnNoRows()
    {
        const string query = @"
            SELECT a.City FROM #A.entities() a
            WHERE a.Population > ANY (
                SELECT b.Population FROM #B.entities() b
                WHERE b.Country = 'SPAIN'
            )";

        var table = CreateAndRunVirtualMachine(query, CreateQuantifiedSources()).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("a.City", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table);
    }

    [TestMethod]
    public void WhenAllSubquery_IsEmpty_ShouldReturnAllRows()
    {
        const string query = @"
            SELECT a.City FROM #A.entities() a
            WHERE a.Population > ALL (
                SELECT b.Population FROM #B.entities() b
                WHERE b.Country = 'SPAIN'
            )";

        var table = CreateAndRunVirtualMachine(query, CreateQuantifiedSources()).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("a.City", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["WARSAW"], ["BERLIN"], ["PARIS"]);
    }

    [TestMethod]
    public void WhenAllSubquery_ContainsNullComparison_ShouldNotTreatUnknownAsTrue()
    {
        const string query = @"
            SELECT a.Name FROM #A.entities() a
            WHERE a.NullableValue > ALL (
                SELECT b.NullableValue FROM #B.entities() b
            )";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "HAS_VALUE", NullableValue = 10 },
                    new BasicEntity { Name = "IS_NULL", NullableValue = null }
                ]
            },
            {
                "#B", [
                    new BasicEntity { Name = "LOW", NullableValue = 1 },
                    new BasicEntity { Name = "UNKNOWN", NullableValue = null }
                ]
            }
        };

        var table = CreateAndRunVirtualMachine(query, sources).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("a.Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table);
    }

    [TestMethod]
    public void WhenAnySubquery_ContainsNulls_ShouldReturnOnlyRowsWithTrueComparison()
    {
        const string query = @"
            SELECT a.Name FROM #A.entities() a
            WHERE a.NullableValue = ANY (
                SELECT b.NullableValue FROM #B.entities() b
            )";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "HAS_VALUE", NullableValue = 10 },
                    new BasicEntity { Name = "IS_NULL", NullableValue = null },
                    new BasicEntity { Name = "NO_MATCH", NullableValue = 30 }
                ]
            },
            {
                "#B", [
                    new BasicEntity { Name = "MATCH", NullableValue = 10 },
                    new BasicEntity { Name = "UNKNOWN", NullableValue = null }
                ]
            }
        };

        var table = CreateAndRunVirtualMachine(query, sources).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("a.Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["HAS_VALUE"]);
    }

    [TestMethod]
    public void WhenAllSubquery_HasCorrelation_ShouldUseAntiSemiJoin()
    {
        const string query = @"
            SELECT a.City FROM #A.entities() a
            WHERE a.Population > ALL (
                SELECT b.Population FROM #B.entities() b
                WHERE b.Country = a.Country
            )";

        var table = CreateAndRunVirtualMachine(query, CreateQuantifiedSources()).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("a.City", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["WARSAW"], ["BERLIN"], ["PARIS"]);

        var inspection = CompileSubqueryForInspection(query);
        Assert.Contains("PhysicalHashJoin [LeftAntiSemi]", inspection.PhysicalPlanText);
    }

    private static Dictionary<string, IEnumerable<BasicEntity>> CreateQuantifiedSources()
    {
        return new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("BERLIN", "GERMANY", 250),
                    new BasicEntity("PARIS", "FRANCE", 300)
                ]
            },
            {
                "#B", [
                    new BasicEntity("KRAKOW", "POLAND", 100),
                    new BasicEntity("GDANSK", "POLAND", 400),
                    new BasicEntity("LYON", "FRANCE", 200)
                ]
            },
            {
                "#C", []
            }
        };
    }
}
