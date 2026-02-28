using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

/// <summary>
///     Evaluator integration tests that verify string comparison consistency
///     across the full SQL compilation and execution pipeline.
///     Tests cover:
///     - LIKE operator case-insensitivity
///     - NOT LIKE operator case-insensitivity
///     - ORDER BY deterministic ordinal sorting
///     - Plugin string functions (Contains, Replace, ToUpper, ToLower, etc.) in queries
///     - GROUP BY with mixed-case strings
///     - UNION/EXCEPT/INTERSECT with string comparisons
///     - WHERE clause with string functions and case variations
/// </summary>
[TestClass]
public class StringComparisonConsistencyTests : BasicEntityTestBase
{
    public TestContext TestContext { get; set; }

    #region NOT LIKE - Case Insensitivity

    [TestMethod]
    public void NotLike_CaseInsensitive_ExcludesAllCases()
    {
        var query = "select Name from #A.entities() where Name not like '%hello%'";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("HELLO WORLD"),
                    new BasicEntity("Hello There"),
                    new BasicEntity("hello everyone"),
                    new BasicEntity("goodbye")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count, "NOT LIKE should exclude all case variations");
        Assert.AreEqual("goodbye", table[0].Values[0]);
    }

    #endregion

    #region LIKE - Case Insensitivity

    [TestMethod]
    public void Like_CaseInsensitive_LowercasePatternMatchesUppercaseData()
    {
        var query = "select Name from #A.entities() where Name like '%hello%'";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("HELLO WORLD"),
                    new BasicEntity("Hello There"),
                    new BasicEntity("hello everyone"),
                    new BasicEntity("goodbye")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count, "LIKE should match case-insensitively");

        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "HELLO WORLD"));
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "Hello There"));
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "hello everyone"));
    }

    [TestMethod]
    public void Like_CaseInsensitive_UppercasePatternMatchesLowercaseData()
    {
        var query = "select Name from #A.entities() where Name like '%WORLD%'";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("hello world"),
                    new BasicEntity("HELLO WORLD"),
                    new BasicEntity("Hello World"),
                    new BasicEntity("goodbye")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count, "LIKE should match case-insensitively");

        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "hello world"));
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "HELLO WORLD"));
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "Hello World"));
    }

    [TestMethod]
    public void Like_CaseInsensitive_PrefixPattern()
    {
        var query = "select Name from #A.entities() where Name like 'test%'";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("Testing"),
                    new BasicEntity("TEST123"),
                    new BasicEntity("test_value"),
                    new BasicEntity("notest")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count, "LIKE prefix should match case-insensitively");

        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "Testing"));
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "TEST123"));
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "test_value"));
    }

    [TestMethod]
    public void Like_CaseInsensitive_SuffixPattern()
    {
        var query = "select Name from #A.entities() where Name like '%ing'";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("Testing"),
                    new BasicEntity("RUNNING"),
                    new BasicEntity("coding"),
                    new BasicEntity("test")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count, "LIKE suffix should match case-insensitively");

        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "Testing"));
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "RUNNING"));
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "coding"));
    }

    [TestMethod]
    public void Like_CaseInsensitive_ExactPattern()
    {
        var query = "select Name from #A.entities() where Name like 'hello'";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("Hello"),
                    new BasicEntity("HELLO"),
                    new BasicEntity("hello"),
                    new BasicEntity("world")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count, "LIKE exact should match case-insensitively");

        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "Hello"));
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "HELLO"));
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "hello"));
    }

    [TestMethod]
    public void Like_CaseInsensitive_SingleCharWildcard()
    {
        var query = "select Name from #A.entities() where Name like 'tes_'";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("test"),
                    new BasicEntity("TEST"),
                    new BasicEntity("TesT"),
                    new BasicEntity("testing")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count, "LIKE with _ wildcard should match case-insensitively");

        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "test"));
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "TEST"));
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "TesT"));
    }

    [TestMethod]
    public void Like_CaseInsensitive_UnicodePolishText()
    {
        var query = "select Name from #A.entities() where Name like '%żółć%'";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("Zażółć gęślą jaźń"),
                    new BasicEntity("ZAŻÓŁĆ GĘŚLĄ JAŹŃ"),
                    new BasicEntity("No match here")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count, "LIKE should match Polish text case-insensitively");

        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "Zażółć gęślą jaźń"));
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "ZAŻÓŁĆ GĘŚLĄ JAŹŃ"));
    }

    [TestMethod]
    public void Like_CaseInsensitive_UnicodeGermanText()
    {
        var query = "select Name from #A.entities() where Name like '%ünch%'";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("München"),
                    new BasicEntity("MÜNCHEN"),
                    new BasicEntity("Berlin")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count, "LIKE should match German text case-insensitively");

        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "München"));
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "MÜNCHEN"));
    }

    [TestMethod]
    public void Like_CaseInsensitive_CyrillicText()
    {
        var query = "select Name from #A.entities() where Name like '%привет%'";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("Привет мир"),
                    new BasicEntity("ПРИВЕТ МИР"),
                    new BasicEntity("привет всем"),
                    new BasicEntity("Пока")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count, "LIKE should match Cyrillic text case-insensitively");

        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "Привет мир"));
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "ПРИВЕТ МИР"));
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "привет всем"));
    }

    #endregion

    #region ORDER BY - Deterministic Ordinal Sorting

    [TestMethod]
    public void OrderBy_StringsSortedByOrdinal_AscendingOrder()
    {
        var query = "select Name from #A.entities() order by Name asc";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("banana"),
                    new BasicEntity("Apple"),
                    new BasicEntity("cherry"),
                    new BasicEntity("Banana")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(4, table.Count);


        Assert.AreEqual("Apple", table[0].Values[0]);
        Assert.AreEqual("Banana", table[1].Values[0]);
        Assert.AreEqual("banana", table[2].Values[0]);
        Assert.AreEqual("cherry", table[3].Values[0]);
    }

    [TestMethod]
    public void OrderBy_StringsSortedByOrdinal_DescendingOrder()
    {
        var query = "select Name from #A.entities() order by Name desc";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("banana"),
                    new BasicEntity("Apple"),
                    new BasicEntity("cherry"),
                    new BasicEntity("Banana")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(4, table.Count);


        Assert.AreEqual("cherry", table[0].Values[0]);
        Assert.AreEqual("banana", table[1].Values[0]);
        Assert.AreEqual("Banana", table[2].Values[0]);
        Assert.AreEqual("Apple", table[3].Values[0]);
    }

    [TestMethod]
    public void OrderBy_UnicodeStrings_DeterministicSort()
    {
        var query = "select Name from #A.entities() order by Name asc";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("zzz"),
                    new BasicEntity("Ąbc"),
                    new BasicEntity("abc"),
                    new BasicEntity("Abc")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(4, table.Count);


        Assert.AreEqual("Abc", table[0].Values[0]);
        Assert.AreEqual("abc", table[1].Values[0]);
        Assert.AreEqual("zzz", table[2].Values[0]);
        Assert.AreEqual("Ąbc", table[3].Values[0]);
    }

    [TestMethod]
    public void OrderBy_Integers_NotAffectedByStringComparer()
    {
        var query = "select Population from #A.entities() order by Population asc";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("c", 300),
                    new BasicEntity("a", 100),
                    new BasicEntity("b", 200)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual(100m, table[0].Values[0]);
        Assert.AreEqual(200m, table[1].Values[0]);
        Assert.AreEqual(300m, table[2].Values[0]);
    }

    #endregion

    #region Plugin String Functions in WHERE Clause

    [TestMethod]
    public void Where_ContainsCaseInsensitive_MatchesDifferentCases()
    {
        var query = "select Name from #A.entities() where Contains(Name, 'world')";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("Hello World"),
                    new BasicEntity("HELLO WORLD"),
                    new BasicEntity("hello world"),
                    new BasicEntity("goodbye")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count, "Contains should be case-insensitive");
    }

    [TestMethod]
    public void Where_StartsWithCaseInsensitive_MatchesDifferentCases()
    {
        var query = "select Name from #A.entities() where StartsWith(Name, 'hello')";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("Hello World"),
                    new BasicEntity("HELLO WORLD"),
                    new BasicEntity("hello world"),
                    new BasicEntity("world hello")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count, "StartsWith should be case-insensitive");
    }

    [TestMethod]
    public void Where_EndsWithCaseInsensitive_MatchesDifferentCases()
    {
        var query = "select Name from #A.entities() where EndsWith(Name, 'world')";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("Hello World"),
                    new BasicEntity("HELLO WORLD"),
                    new BasicEntity("hello world"),
                    new BasicEntity("world hello")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count, "EndsWith should be case-insensitive");
    }

    #endregion

    #region SELECT with Plugin String Functions

    [TestMethod]
    public void Select_ToUpperUsesInvariantCulture()
    {
        var query = "select ToUpper(Name) from #A.entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("hello world"),
                    new BasicEntity("zażółć")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "HELLO WORLD"));
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "ZAŻÓŁĆ"));
    }

    [TestMethod]
    public void Select_ToLowerUsesInvariantCulture()
    {
        var query = "select ToLower(Name) from #A.entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("HELLO WORLD"),
                    new BasicEntity("ZAŻÓŁĆ")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "hello world"));
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "zażółć"));
    }

    [TestMethod]
    public void Select_ReplaceCaseInsensitive()
    {
        var query = "select Replace(Name, 'hello', 'Hi') from #A.entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("Hello World"),
                    new BasicEntity("HELLO World"),
                    new BasicEntity("hello World")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);
        Assert.IsTrue(table.All(row => ((string)row.Values[0]).StartsWith("Hi")),
            "Replace should be case-insensitive and replace all case variants");
    }

    [TestMethod]
    public void Select_IndexOfCaseInsensitive()
    {
        var query = "select IndexOf(Name, 'WORLD') from #A.entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("hello world"),
                    new BasicEntity("Hello World"),
                    new BasicEntity("HELLO WORLD")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);
        Assert.IsTrue(table.All(row => (int?)row.Values[0] == 6),
            "IndexOf should be case-insensitive and return 6 for all rows");
    }

    #endregion

    #region GROUP BY - String Equality

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

        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "hello" && (int)row.Values[1] == 2));
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "Hello" && (int)row.Values[1] == 1));
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "HELLO" && (int)row.Values[1] == 1));
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

        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "hello" && (int)row.Values[1] == 3));
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "world" && (int)row.Values[1] == 1));
    }

    #endregion

    #region LIKE with Multilingual + Case Insensitivity Combined

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

    #endregion

    #region Combined Scenarios

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
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "Warsaw" && (int)row.Values[1] == 2));
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "Krakow" && (int)row.Values[1] == 1));
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

    #endregion

    #region DISTINCT - Case-Sensitive (Ordinal)

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

    #endregion
}
