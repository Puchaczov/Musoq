#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

/// <summary>
///     Single schema tests: NULL handling, WHERE, strings, and complex types.
/// </summary>
[TestClass]
public class SingleSchema_OperationsTests : BasicEntityTestBase
{
    public TestContext TestContext { get; set; }
    [TestMethod]
    public void NullColumnTest()
    {
        var query =
            "select null from #A.Entities()";

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
        Assert.AreEqual("null", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(object), table.Columns.ElementAt(0).ColumnType);

        Assert.IsNull(table[0][0]);
    }

    [TestMethod]
    public void CaseWhenWithEmptyStringTest()
    {
        var query =
            "select (case when 1 = 2 then 'test' else '' end) from #A.Entities()";

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
        Assert.AreEqual("case when 1 = 2 then test else  end", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);

        Assert.AreEqual(string.Empty, table[0][0]);
    }

    [TestMethod]
    public void CaseWhenWithNullTest()
    {
        var query =
            "select (case when 1 = 2 then 'test' else null end) from #A.Entities()";

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
        Assert.AreEqual("case when 1 = 2 then test else null end", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);

        Assert.IsNull(table[0][0]);
    }

    [TestMethod]
    public void ComplexWhere1Test()
    {
        var query =
            "select Population from #A.Entities() where Population > 0 and Population - 100 > -1.5d and Population - 100 < 1.5d";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("CZESTOCHOWA", "POLAND", 99),
                    new BasicEntity("KATOWICE", "POLAND", 101),
                    new BasicEntity("BERLIN", "GERMANY", 50)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Columns.Count());
        Assert.AreEqual("Population", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(decimal), table.Columns.ElementAt(0).ColumnType);

        Assert.AreEqual(2, table.Count, "Table should have 2 entries");

        Assert.IsTrue(table.Any(entry => (decimal)entry.Values[0] == 99m), "First entry should be 99m");
        Assert.IsTrue(table.Any(entry => (decimal)entry.Values[0] == 101m), "Second entry should be 101m");
    }

    [TestMethod]
    public void MultipleAndOperatorTest()
    {
        var query =
            "select Name from #A.Entities() where IndexOf(Name, 'A') = 0 and IndexOf(Name, 'B') = 1 and IndexOf(Name, 'C') = 2";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [new BasicEntity("A"), new BasicEntity("AB"), new BasicEntity("ABC"), new BasicEntity("ABCD")]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Columns.Count());
        Assert.AreEqual("Name", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);

        Assert.AreEqual(2, table.Count, "Table should have 2 entries");

        Assert.IsTrue(table.Any(entry => (string)entry.Values[0] == "ABC"), "First entry should be 'ABC'");
        Assert.IsTrue(table.Any(entry => (string)entry.Values[0] == "ABCD"), "Second entry should be 'ABCD'");
    }

    [TestMethod]
    public void MultipleOrOperatorTest()
    {
        var query = "select Name from #A.Entities() where Name = 'ABC' or Name = 'ABCD' or Name = 'A'";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [new BasicEntity("A"), new BasicEntity("AB"), new BasicEntity("ABC"), new BasicEntity("ABCD")]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Columns.Count());
        Assert.AreEqual("Name", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);

        Assert.AreEqual(3, table.Count, "Table should have 3 entries");

        Assert.IsTrue(table.Any(entry => (string)entry.Values[0] == "A"), "First entry should be 'A'");
        Assert.IsTrue(table.Any(entry => (string)entry.Values[0] == "ABC"), "Second entry should be 'ABC'");
        Assert.IsTrue(table.Any(entry => (string)entry.Values[0] == "ABCD"), "Third entry should be 'ABCD'");
    }

    [TestMethod]
    public void AddOperatorWithStringsTurnsIntoConcatTest()
    {
        var query = "select 'abc' + 'cda' from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("ABCAACBA")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Columns.Count());
        Assert.AreEqual("abc + cda", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("abccda", table[0].Values[0]);
    }

    [TestMethod]
    public void ContainsStringsTest()
    {
        var query = "select Name from #A.Entities() where Name contains ('ABC', 'CdA', 'CDA', 'DDABC')";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("ABC"),
                    new BasicEntity("XXX"),
                    new BasicEntity("CDA"),
                    new BasicEntity("DDABC")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Columns.Count());
        Assert.AreEqual("Name", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);

        Assert.AreEqual(3, table.Count, "Table should have 3 entries");

        Assert.IsTrue(table.Any(entry => (string)entry.Values[0] == "ABC"), "First entry should be 'ABC'");
        Assert.IsTrue(table.Any(entry => (string)entry.Values[0] == "CDA"), "Second entry should be 'CDA'");
        Assert.IsTrue(table.Any(entry => (string)entry.Values[0] == "DDABC"), "Third entry should be 'DDABC'");
    }

    [TestMethod]
    public void CanPassComplexArgumentToFunctionTest()
    {
        var query = "select NothingToDo(Self) from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("001")
                    {
                        Name = "ABBA",
                        Country = "POLAND",
                        City = "CRACOV",
                        Money = 1.23m,
                        Month = "JANUARY",
                        Population = 250
                    }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Columns.Count());
        Assert.AreEqual("NothingToDo(Self)", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(BasicEntity), table.Columns.ElementAt(0).ColumnType);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(typeof(BasicEntity), table[0].Values[0].GetType());
    }

    [TestMethod]
    public void TableShouldReturnComplexTypeTest()
    {
        var query = "select Self from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("001")
                    {
                        Name = "ABBA",
                        Country = "POLAND",
                        City = "CRACOV",
                        Money = 1.23m,
                        Month = "JANUARY",
                        Population = 250
                    }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Columns.Count());
        Assert.AreEqual("Self", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(BasicEntity), table.Columns.ElementAt(0).ColumnType);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(typeof(BasicEntity), table[0].Values[0].GetType());
    }

    [TestMethod]
    public void SimpleShowAllColumnsTest()
    {
        var entity = new BasicEntity("001")
        {
            Name = "ABBA",
            Country = "POLAND",
            City = "CRACOV",
            Money = 1.23m,
            Month = "JANUARY",
            Population = 250,
            Time = DateTime.MaxValue,
            Id = 5,
            NullableValue = null
        };
        var query = "select 1, *, Name as Name2, ToString(Self) as SelfString from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [entity] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual("1", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(int), table.Columns.ElementAt(0).ColumnType);

        Assert.AreEqual("Name", table.Columns.ElementAt(1).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(1).ColumnType);

        Assert.AreEqual("City", table.Columns.ElementAt(2).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(2).ColumnType);

        Assert.AreEqual("Country", table.Columns.ElementAt(3).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(3).ColumnType);

        Assert.AreEqual("Population", table.Columns.ElementAt(4).ColumnName);
        Assert.AreEqual(typeof(decimal), table.Columns.ElementAt(4).ColumnType);

        Assert.AreEqual("Money", table.Columns.ElementAt(5).ColumnName);
        Assert.AreEqual(typeof(decimal), table.Columns.ElementAt(5).ColumnType);

        Assert.AreEqual("Month", table.Columns.ElementAt(6).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(6).ColumnType);

        Assert.AreEqual("Time", table.Columns.ElementAt(7).ColumnName);
        Assert.AreEqual(typeof(DateTime), table.Columns.ElementAt(7).ColumnType);

        Assert.AreEqual("Id", table.Columns.ElementAt(8).ColumnName);
        Assert.AreEqual(typeof(int), table.Columns.ElementAt(8).ColumnType);

        Assert.AreEqual("NullableValue", table.Columns.ElementAt(9).ColumnName);
        Assert.AreEqual(typeof(int?), table.Columns.ElementAt(9).ColumnType);

        Assert.AreEqual("Name2", table.Columns.ElementAt(10).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(10).ColumnType);

        Assert.AreEqual("SelfString", table.Columns.ElementAt(11).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(11).ColumnType);

        Assert.AreEqual(1, table.Count, "Table should have 1 entry");

        Assert.IsTrue(table.Any(entry =>
            (int)entry.Values[0] == Convert.ToInt32(1) &&
            (string)entry.Values[1] == "ABBA" &&
            (string)entry.Values[2] == "CRACOV" &&
            (string)entry.Values[3] == "POLAND" &&
            (decimal)entry.Values[4] == 250m &&
            (decimal)entry.Values[5] == 1.23m &&
            (string)entry.Values[6] == "JANUARY" &&
            (DateTime)entry.Values[7] == DateTime.MaxValue &&
            (int)entry.Values[8] == 5 &&
            entry.Values[9] == null &&
            (string)entry.Values[10] == "ABBA" &&
            (string)entry.Values[11] == "TEST STRING"
        ), "Entry should match all specified values");
    }

    [TestMethod]
    public void SimpleAccessArrayTest()
    {
        var query = @"select Self.Array[2] from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001"), new BasicEntity("002")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual("Self.Array[2]", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(int), table.Columns.ElementAt(0).ColumnType);

        Assert.AreEqual(2, table.Count, "Table should have 2 entries");

        Assert.IsTrue(table.All(entry => (int)entry.Values[0] == 2), "Both entries should have value 2");
    }

    [TestMethod]
    public void SimpleAccessObjectTest()
    {
        var query = @"select Self.Array from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual("Self.Array", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(int[]), table.Columns.ElementAt(0).ColumnType);

        Assert.AreEqual(1, table.Count);
    }

    [TestMethod]
    public void AccessObjectTest()
    {
        var query = @"select Self.Self.Array from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual("Self.Self.Array", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(int[]), table.Columns.ElementAt(0).ColumnType);

        Assert.AreEqual(1, table.Count);
    }

    [TestMethod]
    public void SimpleAccessObjectIncrementTest()
    {
        var query = @"select Inc(Self.Array[2]) from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001"), new BasicEntity("002")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual("Inc(Self.Array[2])", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(int), table.Columns.ElementAt(0).ColumnType);

        Assert.AreEqual(2, table.Count, "Table should have 2 entries");

        Assert.IsTrue(table.All(entry => (int)entry.Values[0] == 3), "Both entries should have value 3 (as int)");
    }

    [TestMethod]
    public void WhereWithOrTest()
    {
        var query = @"select Name from #A.Entities() where Name = '001' or Name = '005'";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001"), new BasicEntity("002"), new BasicEntity("005")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count, "Table should have 2 entries");

        Assert.IsTrue(table.Any(entry => (string)entry.Values[0] == "001"), "First entry should be '001'");
        Assert.IsTrue(table.Any(entry => (string)entry.Values[0] == "005"), "Second entry should be '005'");
    }

    [TestMethod]
    public void SimpleQueryTest()
    {
        var query = @"select Name as 'x1' from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001"), new BasicEntity("002")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count, "Table should have 2 entries");

        Assert.IsTrue(table.Any(entry =>
                (string)entry.Values[0] == "001"),
            "First entry should be '001'");

        Assert.IsTrue(table.Any(entry =>
                (string)entry.Values[0] == "002"),
            "Second entry should be '002'");
    }

    [TestMethod]
    public void SimpleSkipTest()
    {
        var query = @"select Name from #A.Entities() skip 2";
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

        Assert.AreEqual(4, table.Count, "Table should have 4 entries");

        Assert.IsTrue(table.Any(entry => (string)entry.Values[0] == "003"), "First entry should be '003'");
        Assert.IsTrue(table.Any(entry => (string)entry.Values[0] == "004"), "Second entry should be '004'");
        Assert.IsTrue(table.Any(entry => (string)entry.Values[0] == "005"), "Third entry should be '005'");
        Assert.IsTrue(table.Any(entry => (string)entry.Values[0] == "006"), "Fourth entry should be '006'");
    }

}
