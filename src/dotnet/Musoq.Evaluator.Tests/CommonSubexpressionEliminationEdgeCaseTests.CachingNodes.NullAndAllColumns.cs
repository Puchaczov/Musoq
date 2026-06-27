using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class CommonSubexpressionEliminationEdgeCaseTests
{
    #region IsNull Node Id Tests

    [TestMethod]
    public void WhenIsNullOnDifferentColumns_ShouldReturnCorrectResults()
    {
        const string query = @"
            SELECT
                Name,
                Country,
                CASE WHEN Name IS NULL THEN 'NameNull' ELSE 'NameNotNull' END,
                CASE WHEN Country IS NULL THEN 'CountryNull' ELSE 'CountryNotNull' END
            FROM #A.Entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity { Name = null, Country = "Poland" },
                    new BasicEntity { Name = "Test", Country = null },
                    new BasicEntity { Name = "Both", Country = "USA" }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);


        var row1 = table.FirstOrDefault(r => r[1]?.ToString() == "Poland");
        Assert.IsNotNull(row1);
        Assert.IsNull(row1[0]);
        Assert.AreEqual("NameNull", row1[2]);
        Assert.AreEqual("CountryNotNull", row1[3]);


        var row2 = table.FirstOrDefault(r => r[0]?.ToString() == "Test");
        Assert.IsNotNull(row2);
        Assert.IsNull(row2[1]);
        Assert.AreEqual("NameNotNull", row2[2]);
        Assert.AreEqual("CountryNull", row2[3]);


        var row3 = table.FirstOrDefault(r => r[0]?.ToString() == "Both");
        Assert.IsNotNull(row3);
        Assert.AreEqual("NameNotNull", row3[2]);
        Assert.AreEqual("CountryNotNull", row3[3]);
    }

    [TestMethod]
    public void WhenIsNullAndIsNotNullOnSameColumn_ShouldReturnCorrectResults()
    {
        const string query = @"
            SELECT
                Name,
                CASE WHEN Name IS NULL THEN 'IsNull' ELSE 'NotNull1' END,
                CASE WHEN Name IS NOT NULL THEN 'IsNotNull' ELSE 'Null2' END
            FROM #A.Entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity { Name = null },
                    new BasicEntity { Name = "Test" }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);


        var nullRow = table.FirstOrDefault(r => r[0] == null);
        Assert.IsNotNull(nullRow);
        Assert.AreEqual("IsNull", nullRow[1]);
        Assert.AreEqual("Null2", nullRow[2]);


        var notNullRow = table.FirstOrDefault(r => r[0]?.ToString() == "Test");
        Assert.IsNotNull(notNullRow);
        Assert.AreEqual("NotNull1", notNullRow[1]);
        Assert.AreEqual("IsNotNull", notNullRow[2]);
    }

    [TestMethod]
    public void WhenIsNullUsedMultipleTimesOnSameColumn_ShouldReturnCorrectResults()
    {
        const string query = @"
            SELECT
                Name,
                CASE WHEN Name IS NULL THEN 'Null' ELSE Name END as DisplayName
            FROM #A.Entities()
            WHERE Name IS NULL OR Name = 'Test'";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity { Name = null },
                    new BasicEntity { Name = "Test" },
                    new BasicEntity { Name = "Other" }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);

        var nullRow = table.FirstOrDefault(r => r[0] == null);
        Assert.IsNotNull(nullRow);
        Assert.AreEqual("Null", nullRow[1]);

        var testRow = table.FirstOrDefault(r => r[0]?.ToString() == "Test");
        Assert.IsNotNull(testRow);
        Assert.AreEqual("Test", testRow[1]);
    }

    #endregion

    #region AllColumns Node Id Tests

    [TestMethod]
    public void WhenAllColumnsWithDifferentAliases_ShouldReturnCorrectResults()
    {
        const string query = @"
            SELECT a.Country, a.Population, b.Country as Country2, b.Population as Population2
            FROM #A.Entities() a
            INNER JOIN #B.Entities() b ON a.Country = b.Country";

        var sourcesA = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("Poland", 100),
                    new BasicEntity("USA", 200)
                ]
            }
        };

        var sourcesB = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#B",
                [
                    new BasicEntity("Poland", 38),
                    new BasicEntity("USA", 331)
                ]
            }
        };


        var allSources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", sourcesA["#A"] },
            { "#B", sourcesB["#B"] }
        };

        var vm = CreateAndRunVirtualMachine(query, allSources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);

        var polandRow = table.FirstOrDefault(r => r[0]?.ToString() == "Poland");
        Assert.IsNotNull(polandRow);
        Assert.AreEqual(100m, polandRow[1]);
        Assert.AreEqual("Poland", polandRow[2]);
        Assert.AreEqual(38m, polandRow[3]);

        var usaRow = table.FirstOrDefault(r => r[0]?.ToString() == "USA");
        Assert.IsNotNull(usaRow);
        Assert.AreEqual(200m, usaRow[1]);
        Assert.AreEqual("USA", usaRow[2]);
        Assert.AreEqual(331m, usaRow[3]);
    }

    public TestContext TestContext { get; set; }

    #endregion
}
