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
public partial class StringComparisonConsistencyTests : BasicEntityTestBase
{


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



}
