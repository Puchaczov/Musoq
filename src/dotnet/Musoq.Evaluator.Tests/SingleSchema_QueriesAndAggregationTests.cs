#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

/// <summary>
///     Single schema tests: Pagination, columns, aggregation, and schema introspection.
/// </summary>
[TestClass]
public class SingleSchema_QueriesAndAggregationTests : BasicEntityTestBase
{
    public TestContext TestContext { get; set; }
    [TestMethod]
    public void SimpleTakeTest()
    {
        var query = @"select Name from #A.Entities() take 2";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("001"),
                    new BasicEntity("002"),
                    new BasicEntity("003"),
                    new BasicEntity("004"),
                    new BasicEntity("005"),
                    new BasicEntity("006")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count, "Table should have 2 entries");

        Assert.IsTrue(table.Any(entry => (string)entry.Values[0] == "001"), "First entry should be '001'");
        Assert.IsTrue(table.Any(entry => (string)entry.Values[0] == "002"), "Second entry should be '002'");
    }

    [TestMethod]
    public void GetHexTest()
    {
        var query = @"select ToHex(GetBytes(5), '|') as hexValue from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("001")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("05|00", table[0][0]);
    }

    [TestMethod]
    public void SimpleSkipTakeTest()
    {
        var query = @"select Name from #A.Entities() skip 1 take 2";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("001"), new BasicEntity("002"), new BasicEntity("003"), new BasicEntity("004"),
                    new BasicEntity("005"), new BasicEntity("006")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count, "Table should have 2 entries");

        Assert.IsTrue(table.Any(entry => (string)entry.Values[0] == "002"), "First entry should be '002'");
        Assert.IsTrue(table.Any(entry => (string)entry.Values[0] == "003"), "Second entry should be '003'");
    }

    [TestMethod]
    public void SimpleSkipAboveTableAmountTest()
    {
        var query = @"select Name from #A.Entities() skip 100";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("001"), new BasicEntity("002"), new BasicEntity("003"), new BasicEntity("004"),
                    new BasicEntity("005"), new BasicEntity("006")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(0, table.Count);
    }

    [TestMethod]
    public void SimpleTakeAboveTableAmountTest()
    {
        var query = @"select Name from #A.Entities() take 100";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("001"), new BasicEntity("002"), new BasicEntity("003"), new BasicEntity("004"),
                    new BasicEntity("005"), new BasicEntity("006")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(6, table.Count);
        Assert.AreEqual("001", table[0].Values[0]);
        Assert.AreEqual("002", table[1].Values[0]);
        Assert.AreEqual("003", table[2].Values[0]);
        Assert.AreEqual("004", table[3].Values[0]);
        Assert.AreEqual("005", table[4].Values[0]);
        Assert.AreEqual("006", table[5].Values[0]);
    }

    [TestMethod]
    public void SimpleSkipTakeAboveTableAmountTest()
    {
        var query = @"select Name from #A.Entities() skip 100 take 100";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("001"), new BasicEntity("002"), new BasicEntity("003"), new BasicEntity("004"),
                    new BasicEntity("005"), new BasicEntity("006")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(0, table.Count);
    }

    [TestMethod]
    public void ColumnNamesSimpleTest()
    {
        var query =
            @"select Name as TestName, GetOne(), GetOne() as TestColumn, GetTwo(4d, 'test') from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(4, table.Columns.Count());
        Assert.AreEqual("TestName", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);

        Assert.AreEqual("GetOne()", table.Columns.ElementAt(1).ColumnName);
        Assert.AreEqual(typeof(decimal), table.Columns.ElementAt(1).ColumnType);

        Assert.AreEqual("TestColumn", table.Columns.ElementAt(2).ColumnName);
        Assert.AreEqual(typeof(decimal), table.Columns.ElementAt(2).ColumnType);

        Assert.AreEqual("GetTwo(4, test)", table.Columns.ElementAt(3).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(3).ColumnType);
    }

    [TestMethod]
    public void CallMethodWithTwoParametersTest()
    {
        var query = @"select Concat(Country, ToString(Population)) from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("ABBA", 200)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Columns.Count());
        Assert.AreEqual("Concat(Country, ToString(Population))", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("ABBA200", table[0].Values[0]);
    }

    [TestMethod]
    public void ColumnTypeDateTimeTest()
    {
        var query = "select Time from #A.entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity(DateTime.MinValue)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Columns.Count());
        Assert.AreEqual("Time", table.Columns.ElementAt(0).ColumnName);

        Assert.AreEqual(1, table.Count());
        Assert.AreEqual(DateTime.MinValue, table[0].Values[0]);
    }

    [TestMethod]
    public void SimpleRowNumberStatTest()
    {
        var query = @"select RowNumber() from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("001"),
                    new BasicEntity("002"),
                    new BasicEntity("003"),
                    new BasicEntity("004"),
                    new BasicEntity("005"),
                    new BasicEntity("006")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(6, table.Count, "Table should have 6 entries");

        Assert.IsTrue(table.Any(entry => (int)entry.Values[0] == 1), "First entry should be 1");
        Assert.IsTrue(table.Any(entry => (int)entry.Values[0] == 2), "Second entry should be 2");
        Assert.IsTrue(table.Any(entry => (int)entry.Values[0] == 3), "Third entry should be 3");
        Assert.IsTrue(table.Any(entry => (int)entry.Values[0] == 4), "Fourth entry should be 4");
        Assert.IsTrue(table.Any(entry => (int)entry.Values[0] == 5), "Fifth entry should be 5");
        Assert.IsTrue(table.Any(entry => (int)entry.Values[0] == 6), "Sixth entry should be 6");
    }

    [TestMethod]
    public void SelectDecimalWithoutMarkingNumberExplicitlyTest()
    {
        var query = "select 1.0, -1.0 from #A.entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("xX")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Columns.Count());
        Assert.AreEqual("1.0", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(decimal), table.Columns.ElementAt(0).ColumnType);
        Assert.AreEqual("-1.0", table.Columns.ElementAt(1).ColumnName);
        Assert.AreEqual(typeof(decimal), table.Columns.ElementAt(1).ColumnType);

        Assert.AreEqual(1, table.Count());
        Assert.AreEqual(1.0m, table[0].Values[0]);
        Assert.AreEqual(-1.0m, table[0].Values[1]);
    }

    [TestMethod]
    public void DescEntityTest()
    {
        var query = "desc #A.entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("xX")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Columns.Count());

        Assert.AreEqual("Name", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);

        Assert.AreEqual("Index", table.Columns.ElementAt(1).ColumnName);
        Assert.AreEqual(typeof(int), table.Columns.ElementAt(1).ColumnType);

        Assert.AreEqual("Type", table.Columns.ElementAt(2).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(2).ColumnType);

        Assert.IsTrue(table.Any(row => (string)row[0] == "Name" && (string)row[2] == "System.String"));
        Assert.IsTrue(table.Any(row => (string)row[0] == "City" && (string)row[2] == "System.String"));
        Assert.IsTrue(table.Any(row => (string)row[0] == "Country" && (string)row[2] == "System.String"));
        Assert.IsTrue(table.Any(row =>
            (string)row[0] == "Self" && (string)row[2] == "Musoq.Evaluator.Tests.Schema.Basic.BasicEntity"));
        Assert.IsTrue(table.Any(row => (string)row[0] == "Money" && (string)row[2] == "System.Decimal"));
        Assert.IsTrue(table.Any(row => (string)row[0] == "Month" && (string)row[2] == "System.String"));
        Assert.IsTrue(table.Any(row => (string)row[0] == "Time" && (string)row[2] == "System.DateTime"));
        Assert.IsTrue(table.Any(row => (string)row[0] == "Id" && (string)row[2] == "System.Int32"));
        Assert.IsTrue(table.Any(row => (string)row[0] == "NullableValue" && (string)row[2] ==
            "System.Nullable`1[[System.Int32, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]"));
    }

    [TestMethod]
    public void DescMethodTest()
    {
        var query = "desc #A.entities";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("xX")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Columns.Count());
        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("entities", table[0][0]);
    }

    [TestMethod]
    public void DescSchemaTest()
    {
        var query = "desc #A";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("xX")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Columns.Count());
        Assert.AreEqual(2, table.Count);

        Assert.AreEqual("empty", table[0][0]);
        Assert.AreEqual("entities", table[1][0]);
    }

    [TestMethod]
    public void AggregateValuesTest()
    {
        var query = @"select AggregateValues(Name) from #A.entities() a group by Name";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("A"),
                    new BasicEntity("B")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count, "Table should contain 2 rows");

        Assert.IsTrue(table.Any(row => (string)row[0] == "A") &&
                      table.Any(row => (string)row[0] == "B"),
            "Expected rows with values A and B");
    }

    [TestMethod]
    public void AggregateValuesParentTest()
    {
        var query = @"select AggregateValues(Name, 1) from #A.entities() a group by Name";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("A"),
                    new BasicEntity("B")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual("A,B", table[0][0]);
    }

    [TestMethod]
    public void CoalesceTest()
    {
        var query = @"select Coalesce('a', 'b', 'c', 'e', 'f') from #A.entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("A")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual("a", table[0][0]);
    }

    [TestMethod]
    public void ChooseTest()
    {
        var query = @"select Choose(2, 'a', 'b', 'c', 'e', 'f') from #A.entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("A")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual("c", table[0][0]);
    }

}
