#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Parser.Diagnostics;
using static Musoq.Evaluator.Tests.MusoqExceptionAssertions;

namespace Musoq.Evaluator.Tests;

/// <summary>
///     Single schema tests: Pattern matching and validation.
/// </summary>
[TestClass]
public class SingleSchema_PatternMatchingTests : BasicEntityTestBase
{
    public TestContext TestContext { get; set; }

    [TestMethod]
    public void WhenMissingSchema_ShouldFail()
    {
        var query = "select Name from #B.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", []
            }
        };

        var ex = Assert.Throws<MusoqQueryException>(() => CreateAndRunVirtualMachine(query, sources));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3010_UnknownSchema, DiagnosticPhase.Bind, "#B");
        AssertHasGuidance(ex);
    }

    [TestMethod]
    public void LikeOperatorTest()
    {
        var query = "select Name from #A.Entities() where Name like '%AA%'";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("ABCAACBA"),
                    new BasicEntity("AAeqwgQEW"),
                    new BasicEntity("XXX"),
                    new BasicEntity("dadsqqAA")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Columns.Count());
        Assert.AreEqual("Name", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);

        Assert.AreEqual(3, table.Count, "Table should contain 3 rows");

        Assert.IsTrue(table.Any(row =>
                (string)row.Values[0] == "ABCAACBA"),
            "Row with ABCAACBA not found");

        Assert.IsTrue(table.Any(row =>
                (string)row.Values[0] == "AAeqwgQEW"),
            "Row with AAeqwgQEW not found");

        Assert.IsTrue(table.Any(row =>
                (string)row.Values[0] == "dadsqqAA"),
            "Row with dadsqqAA not found");
    }

    [TestMethod]
    public void NotLikeOperatorTest()
    {
        var query = "select Name from #A.Entities() where Name not like '%AA%'";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("ABCAACBA"), new BasicEntity("AAeqwgQEW"), new BasicEntity("XXX"),
                    new BasicEntity("dadsqqAA")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Columns.Count());
        Assert.AreEqual("Name", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("XXX", table[0].Values[0]);
    }

    [TestMethod]
    public void WrongColumnNameWithHintTest()
    {
        var query = "select Namre from #A.Entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("ABCAACBA"), new BasicEntity("AAeqwgQEW"), new BasicEntity("XXX"),
                    new BasicEntity("dadsqqAA")
                ]
            }
        };

        var ex = Assert.Throws<MusoqQueryException>(() => CreateAndRunVirtualMachine(query, sources));

        AssertSingleError(ex, DiagnosticCode.MQ3001_UnknownColumn, DiagnosticPhase.Bind, "Namre");
    }

    [TestMethod]
    public void RLikeOperatorTest()
    {
        var query =
            @"select Name from #A.Entities() where Name rlike '^([\w-\.]+)@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([\w-]+\.)+))([a-zA-Z]{2,4}|[0-9]{1,3})(\]?)$'";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("12@hostname.com"),
                    new BasicEntity("ma@hostname.comcom"),
                    new BasicEntity("david.jones@proseware.com"),
                    new BasicEntity("ma@hostname.com")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Columns.Count());
        Assert.AreEqual("Name", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);

        Assert.AreEqual(3, table.Count, "Table should contain 3 rows");

        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "12@hostname.com"), "Missing 12@hostname.com");
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "david.jones@proseware.com"),
            "Missing david.jones@proseware.com");
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "ma@hostname.com"), "Missing ma@hostname.com");
    }

    [TestMethod]
    public void NotRLikeOperatorTest()
    {
        var query =
            @"select Name from #A.Entities() where Name not rlike '^([\w-\.]+)@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([\w-]+\.)+))([a-zA-Z]{2,4}|[0-9]{1,3})(\]?)$'";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("12@hostname.com"),
                    new BasicEntity("ma@hostname.comcom"),
                    new BasicEntity("david.jones@proseware.com"),
                    new BasicEntity("ma@hostname.com")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Columns.Count());
        Assert.AreEqual("Name", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("ma@hostname.comcom", table[0].Values[0]);
    }

    [TestMethod]
    public void LikeOperator_WhenLeftSideIsNull_ShouldReturnFalse()
    {
        var query = "select Name, City from #A.Entities() where Name like '%test%'";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity { Name = null, City = "CityA" },
                    new BasicEntity { Name = "test123", City = "CityB" },
                    new BasicEntity { Name = null, City = "CityC" },
                    new BasicEntity { Name = "testValue", City = "CityD" }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Columns.Count());
        Assert.AreEqual(2, table.Count);
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "test123"));
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "testValue"));
    }

    [TestMethod]
    public void LikeOperator_WhenRightSideIsNull_ShouldReturnFalse()
    {
        var query = "select Name from #A.Entities() where Name like City";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity { Name = "test", City = null },
                    new BasicEntity { Name = "match", City = "match" },
                    new BasicEntity { Name = "other", City = null }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Columns.Count());
        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("match", table[0].Values[0]);
    }

    [TestMethod]
    public void LikeOperator_WhenBothSidesAreNull_ShouldReturnFalse()
    {
        var query = "select Name from #A.Entities() where Name like City";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity { Name = null, City = null },
                    new BasicEntity { Name = "test", City = "test" },
                    new BasicEntity { Name = null, City = null }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Columns.Count());
        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("test", table[0].Values[0]);
    }

    [TestMethod]
    public void NotLikeOperator_WhenLeftSideIsNull_ShouldTreatAsNotFalse()
    {
        var query = "select Name from #A.Entities() where Name not like '%test%'";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity { Name = null },
                    new BasicEntity { Name = "test123" },
                    new BasicEntity { Name = "other" },
                    new BasicEntity { Name = null }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Columns.Count());
        Assert.AreEqual(3, table.Count);
        Assert.AreEqual(2, table.Count(row => row.Values[0] == null));
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "other"));
    }

    [TestMethod]
    public void RLikeOperator_WhenLeftSideIsNull_ShouldReturnFalse()
    {
        var query = @"select Name from #A.Entities() where Name rlike '^test.*$'";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity { Name = null },
                    new BasicEntity { Name = "test123" },
                    new BasicEntity { Name = null },
                    new BasicEntity { Name = "testValue" }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Columns.Count());
        Assert.AreEqual(2, table.Count);
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "test123"));
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "testValue"));
    }

    [TestMethod]
    public void RLikeOperator_WhenRightSideIsNull_ShouldReturnFalse()
    {
        var query = "select Name from #A.Entities() where Name rlike City";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity { Name = "test", City = null },
                    new BasicEntity { Name = "abc", City = "a.*" },
                    new BasicEntity { Name = "xyz", City = null }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Columns.Count());
        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("abc", table[0].Values[0]);
    }

    [TestMethod]
    public void RLikeOperator_WhenBothSidesAreNull_ShouldReturnFalse()
    {
        var query = "select Name from #A.Entities() where Name rlike City";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity { Name = null, City = null },
                    new BasicEntity { Name = "test", City = "test" },
                    new BasicEntity { Name = null, City = null }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Columns.Count());
        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("test", table[0].Values[0]);
    }

    [TestMethod]
    public void NotRLikeOperator_WhenLeftSideIsNull_ShouldTreatAsNotFalse()
    {
        var query = @"select Name from #A.Entities() where Name not rlike '^test.*$'";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity { Name = null },
                    new BasicEntity { Name = "test123" },
                    new BasicEntity { Name = "other" },
                    new BasicEntity { Name = null }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Columns.Count());
        Assert.AreEqual(3, table.Count);
        Assert.AreEqual(2, table.Count(row => row.Values[0] == null));
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "other"));
    }

    [TestMethod]
    public void FirstLetterOfColumnTest()
    {
        var query = @"select Name from #A.Entities() where Name[0] = 'd'";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("12@hostname.com"),
                    new BasicEntity("ma@hostname.comcom"),
                    new BasicEntity("david.jones@proseware.com"),
                    new BasicEntity("ma@hostname.com")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Columns.Count());
        Assert.AreEqual("Name", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("david.jones@proseware.com", table[0].Values[0]);
    }

    [TestMethod]
    public void FirstLetterOfColumnTest2()
    {
        var query = @"select Name from #A.Entities() f where f.Name[0] = 'd'";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("12@hostname.com"),
                    new BasicEntity("ma@hostname.comcom"),
                    new BasicEntity("david.jones@proseware.com"),
                    new BasicEntity("ma@hostname.com")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Columns.Count());
        Assert.AreEqual("Name", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("david.jones@proseware.com", table[0].Values[0]);
    }

    [TestMethod]
    public void WrongColumnNameTest()
    {
        var query =
            "select Populationr from #A.Entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("WARSAW", "POLAND", 500)
                ]
            }
        };

        var ex = Assert.Throws<MusoqQueryException>(() => CreateAndRunVirtualMachine(query, sources));

        AssertSingleError(ex, DiagnosticCode.MQ3001_UnknownColumn, DiagnosticPhase.Bind, "Populationr");
    }

    [TestMethod]
    public void EmptyStringTest()
    {
        var query =
            "select '' from #A.Entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("WARSAW", "POLAND", 500)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Columns.Count());
        Assert.AreEqual("''", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);

        Assert.AreEqual(string.Empty, table[0][0]);
    }

}
