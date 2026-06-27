using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class StringComparisonConsistencyTests
{
    [TestMethod]
    public void GroupBy_StringEquality_IsOrdinal_CaseSensitive()
    {
        var query = "select Name, Count(Name) from #A.entities() group by Name";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("hello"),
                    new BasicEntity("Hello"),
                    new BasicEntity("HELLO"),
                    new BasicEntity("hello")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);


        Assert.AreEqual(3, table.Count, "GROUP BY should treat different cases as different groups");

        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "hello" && (long)row.Values[1] == 2L));
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "Hello" && (long)row.Values[1] == 1L));
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "HELLO" && (long)row.Values[1] == 1L));
    }

    [TestMethod]
    public void GroupBy_ToLower_GroupsCaseInsensitively()
    {
        var query = "select ToLower(Name), Count(Name) from #A.entities() group by ToLower(Name)";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("hello"),
                    new BasicEntity("Hello"),
                    new BasicEntity("HELLO"),
                    new BasicEntity("world")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count, "GROUP BY ToLower should merge case variants");

        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "hello" && (long)row.Values[1] == 3L));
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "world" && (long)row.Values[1] == 1L));
    }



    [TestMethod]
    public void Like_CaseInsensitive_GreekText()
    {
        var query = "select Name from #A.entities() where Name like '%αθήνα%'";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("Η Αθήνα είναι πρωτεύουσα"),
                    new BasicEntity("Η ΑΘΉΝΑ ΕΊΝΑΙ ΠΡΩΤΕΎΟΥΣΑ"),
                    new BasicEntity("Η Θεσσαλονίκη")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count, "LIKE should match Greek text case-insensitively");
    }

    [TestMethod]
    public void Like_CaseInsensitive_TurkishText()
    {
        var query = "select Name from #A.entities() where Name like '%istanbul%'";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("Istanbul is beautiful"),
                    new BasicEntity("ISTANBUL IS BEAUTIFUL"),
                    new BasicEntity("istanbul is beautiful"),
                    new BasicEntity("Ankara")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count, "LIKE should match Istanbul text case-insensitively");
    }



    [TestMethod]
    public void Like_CaseInsensitive_WithOrderBy_CombinedTest()
    {
        var query = "select Name from #A.entities() where Name like '%test%' order by Name asc";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("test_c"),
                    new BasicEntity("TEST_A"),
                    new BasicEntity("Test_B"),
                    new BasicEntity("no_match")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count, "LIKE should match all case variants");


        Assert.AreEqual("TEST_A", table[0].Values[0]);
        Assert.AreEqual("Test_B", table[1].Values[0]);
        Assert.AreEqual("test_c", table[2].Values[0]);
    }

    [TestMethod]
    public void Like_CaseInsensitive_WithGroupBy_CombinedTest()
    {
        var query = @"
            select City, Count(City)
            from #A.entities()
            where Name like '%active%'
            group by City";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("Poland", "Warsaw") { Name = "active_user1" },
                    new BasicEntity("Poland", "Warsaw") { Name = "ACTIVE_user2" },
                    new BasicEntity("Poland", "Krakow") { Name = "Active_user3" },
                    new BasicEntity("Poland", "Warsaw") { Name = "sleeping" }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count, "Should have 2 city groups");
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "Warsaw" && (long)row.Values[1] == 2L));
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "Krakow" && (long)row.Values[1] == 1L));
    }

    [TestMethod]
    public void Select_ContainsAndLike_Consistent()
    {
        var query = @"
            select Name,
                   Contains(Name, 'test') as ContainsResult
            from #A.entities()
            where Name like '%test%'";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("TestValue"),
                    new BasicEntity("TESTVALUE"),
                    new BasicEntity("testvalue"),
                    new BasicEntity("no_match")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count, "LIKE and query should match 3 rows");


        Assert.IsTrue(table.All(row => (bool?)row.Values[1] == true),
            "Contains should agree with LIKE on case-insensitive matching");
    }

    [TestMethod]
    public void Select_ToUpperAndToLower_RoundTripConsistency()
    {
        var query = "select ToLower(ToUpper(Name)) from #A.entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("hello"),
                    new BasicEntity("Hello"),
                    new BasicEntity("HELLO")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);
        Assert.IsTrue(table.All(row => (string)row.Values[0] == "hello"),
            "ToLower(ToUpper()) should normalize all case variants to lowercase");
    }



    [TestMethod]
    public void Distinct_CaseSensitive_PreservesAllCaseVariants()
    {
        var query = "select distinct Name from #A.entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("hello"),
                    new BasicEntity("Hello"),
                    new BasicEntity("HELLO"),
                    new BasicEntity("hello")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count, "DISTINCT should treat different cases as different values (ordinal)");
    }

    [TestMethod]
    public void Distinct_ToLower_MergesCaseVariants()
    {
        var query = "select distinct ToLower(Name) from #A.entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("hello"),
                    new BasicEntity("Hello"),
                    new BasicEntity("HELLO"),
                    new BasicEntity("world")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count, "DISTINCT ToLower should merge case variants");
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "hello"));
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "world"));
    }

}
