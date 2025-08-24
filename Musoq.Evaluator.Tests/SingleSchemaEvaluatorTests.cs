#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Tests.Exceptions;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class SingleSchemaEvaluatorTests : BasicEntityTestBase
{
    [TestMethod]
    public void WhenMissingSchema_ShouldFail()
    {
        var query = "select Name from @B.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "@A", []
            }
        };

        Assert.ThrowsException<SchemaNotFoundException>(() => CreateAndRunVirtualMachine(query, sources));
    }
    
    [TestMethod]
    public void LikeOperatorTest()
    {
        var query = "select Name from @A.Entities() where Name like '%AA%'";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "@A",
                [
                    new BasicEntity("ABCAACBA"), 
                    new BasicEntity("AAeqwgQEW"), 
                    new BasicEntity("XXX"),
                    new BasicEntity("dadsqqAA")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(1, table.Columns.Count());
        Assert.AreEqual("Name", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);

        Assert.IsTrue(table.Count == 3, "Table should contain 3 rows");

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
        var query = "select Name from @A.Entities() where Name not like '%AA%'";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "@A",
                [
                    new BasicEntity("ABCAACBA"), new BasicEntity("AAeqwgQEW"), new BasicEntity("XXX"),
                    new BasicEntity("dadsqqAA")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(1, table.Columns.Count());
        Assert.AreEqual("Name", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("XXX", table[0].Values[0]);
    }

    [TestMethod]
    public void WrongColumnNameWithHintTest()
    {
        var query = "select Namre from @A.Entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "@A",
                [
                    new BasicEntity("ABCAACBA"), new BasicEntity("AAeqwgQEW"), new BasicEntity("XXX"),
                    new BasicEntity("dadsqqAA")
                ]
            }
        };

        Assert.ThrowsException<UnknownColumnOrAliasException>(() => CreateAndRunVirtualMachine(query, sources));
    }

    [TestMethod]
    public void RLikeOperatorTest()
    {
        var query = @"select Name from @A.Entities() where Name rlike '^([\w-\.]+)@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([\w-]+\.)+))([a-zA-Z]{2,4}|[0-9]{1,3})(\]?)$'";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "@A",
                [
                    new BasicEntity("12@hostname.com"),
                    new BasicEntity("ma@hostname.comcom"),
                    new BasicEntity("david.jones@proseware.com"),
                    new BasicEntity("ma@hostname.com")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(1, table.Columns.Count());
        Assert.AreEqual("Name", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);
        
        Assert.IsTrue(table.Count == 3, "Table should contain 3 rows");

        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "12@hostname.com"), "Missing 12@hostname.com");
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "david.jones@proseware.com"), "Missing david.jones@proseware.com");
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "ma@hostname.com"), "Missing ma@hostname.com");
    }

    [TestMethod]
    public void NotRLikeOperatorTest()
    {
        var query = @"select Name from @A.Entities() where Name not rlike '^([\w-\.]+)@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([\w-]+\.)+))([a-zA-Z]{2,4}|[0-9]{1,3})(\]?)$'";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "@A",
                [
                    new BasicEntity("12@hostname.com"),
                    new BasicEntity("ma@hostname.comcom"),
                    new BasicEntity("david.jones@proseware.com"),
                    new BasicEntity("ma@hostname.com")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(1, table.Columns.Count());
        Assert.AreEqual("Name", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("ma@hostname.comcom", table[0].Values[0]);
    }

    [TestMethod]
    public void FirstLetterOfColumnTest()
    {
        var query = @"select Name from @A.Entities() where Name[0] = 'd'";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "@A",
                [
                    new BasicEntity("12@hostname.com"),
                    new BasicEntity("ma@hostname.comcom"),
                    new BasicEntity("david.jones@proseware.com"),
                    new BasicEntity("ma@hostname.com")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(1, table.Columns.Count());
        Assert.AreEqual("Name", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("david.jones@proseware.com", table[0].Values[0]);
    }

    [TestMethod]
    public void FirstLetterOfColumnTest2()
    {
        var query = @"select Name from @A.Entities() f where f.Name[0] = 'd'";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "@A",
                [
                    new BasicEntity("12@hostname.com"),
                    new BasicEntity("ma@hostname.comcom"),
                    new BasicEntity("david.jones@proseware.com"),
                    new BasicEntity("ma@hostname.com")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

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
            $"select Populationr from @A.Entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "@A", [
                    new BasicEntity("WARSAW", "POLAND", 500)
                ]
            }
        };

        Assert.ThrowsException<UnknownColumnOrAliasException>(() => CreateAndRunVirtualMachine(query, sources));
    }

    [TestMethod]
    public void EmptyStringTest()
    {
        var query =
            $"select '' from @A.Entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "@A", [
                    new BasicEntity("WARSAW", "POLAND", 500)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(1, table.Columns.Count());
        Assert.AreEqual("''", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);

        Assert.AreEqual(string.Empty, table[0][0]);
    }

    [TestMethod]
    public void NullColumnTest()
    {
        var query =
            "select null from @A.Entities()";
            
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "@A", [
                    new BasicEntity("WARSAW", "POLAND", 500)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(1, table.Columns.Count());
        Assert.AreEqual("null", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(object), table.Columns.ElementAt(0).ColumnType);

        Assert.AreEqual(null, table[0][0]);
    }

    [TestMethod]
    public void CaseWhenWithEmptyStringTest()
    {
        var query =
            $"select (case when 1 = 2 then 'test' else '' end) from @A.Entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "@A", [
                    new BasicEntity("WARSAW", "POLAND", 500)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(1, table.Columns.Count());
        Assert.AreEqual("case when 1 = 2 then test else  end", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);

        Assert.AreEqual(string.Empty, table[0][0]);
    }

    [TestMethod]
    public void CaseWhenWithNullTest()
    {
        var query =
            $"select (case when 1 = 2 then 'test' else null end) from @A.Entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "@A", [
                    new BasicEntity("WARSAW", "POLAND", 500)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(1, table.Columns.Count());
        Assert.AreEqual("case when 1 = 2 then test else null end", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);

        Assert.AreEqual(null, table[0][0]);
    }

    [TestMethod]
    public void ComplexWhere1Test()
    {
        var query =
            $"select Population from @A.Entities() where Population > 0 and Population - 100 > -1.5d and Population - 100 < 1.5d";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "@A", [
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("CZESTOCHOWA", "POLAND", 99),
                    new BasicEntity("KATOWICE", "POLAND", 101),
                    new BasicEntity("BERLIN", "GERMANY", 50)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(1, table.Columns.Count());
        Assert.AreEqual("Population", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(decimal), table.Columns.ElementAt(0).ColumnType);
        
        Assert.IsTrue(table.Count == 2, "Table should have 2 entries");

        Assert.IsTrue(table.Any(entry => (decimal)entry.Values[0] == 99m), "First entry should be 99m");
        Assert.IsTrue(table.Any(entry => (decimal)entry.Values[0] == 101m), "Second entry should be 101m");
    }

    [TestMethod]
    public void MultipleAndOperatorTest()
    {
        var query =
            "select Name from @A.Entities() where IndexOf(Name, 'A') = 0 and IndexOf(Name, 'B') = 1 and IndexOf(Name, 'C') = 2";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "@A",
                [new BasicEntity("A"), new BasicEntity("AB"), new BasicEntity("ABC"), new BasicEntity("ABCD")]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(1, table.Columns.Count());
        Assert.AreEqual("Name", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);
        
        Assert.IsTrue(table.Count == 2, "Table should have 2 entries");

        Assert.IsTrue(table.Any(entry => (string)entry.Values[0] == "ABC"), "First entry should be 'ABC'");
        Assert.IsTrue(table.Any(entry => (string)entry.Values[0] == "ABCD"), "Second entry should be 'ABCD'");
    }

    [TestMethod]
    public void MultipleOrOperatorTest()
    {
        var query = "select Name from @A.Entities() where Name = 'ABC' or Name = 'ABCD' or Name = 'A'";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "@A",
                [new BasicEntity("A"), new BasicEntity("AB"), new BasicEntity("ABC"), new BasicEntity("ABCD")]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(1, table.Columns.Count());
        Assert.AreEqual("Name", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);
        
        Assert.IsTrue(table.Count == 3, "Table should have 3 entries");

        Assert.IsTrue(table.Any(entry => (string)entry.Values[0] == "A"), "First entry should be 'A'");
        Assert.IsTrue(table.Any(entry => (string)entry.Values[0] == "ABC"), "Second entry should be 'ABC'");
        Assert.IsTrue(table.Any(entry => (string)entry.Values[0] == "ABCD"), "Third entry should be 'ABCD'");
    }

    [TestMethod]
    public void AddOperatorWithStringsTurnsIntoConcatTest()
    {
        var query = "select 'abc' + 'cda' from @A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {"@A", [new BasicEntity("ABCAACBA")]}
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(1, table.Columns.Count());
        Assert.AreEqual("abc + cda", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("abccda", table[0].Values[0]);
    }

    [TestMethod]
    public void ContainsStringsTest()
    {
        var query = "select Name from @A.Entities() where Name contains ('ABC', 'CdA', 'CDA', 'DDABC')";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "@A",
                [
                    new BasicEntity("ABC"),
                    new BasicEntity("XXX"),
                    new BasicEntity("CDA"),
                    new BasicEntity("DDABC")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(1, table.Columns.Count());
        Assert.AreEqual("Name", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);

        Assert.IsTrue(table.Count == 3, "Table should have 3 entries");

        Assert.IsTrue(table.Any(entry => (string)entry.Values[0] == "ABC"), "First entry should be 'ABC'");
        Assert.IsTrue(table.Any(entry => (string)entry.Values[0] == "CDA"), "Second entry should be 'CDA'");
        Assert.IsTrue(table.Any(entry => (string)entry.Values[0] == "DDABC"), "Third entry should be 'DDABC'");
    }

    [TestMethod]
    public void CanPassComplexArgumentToFunctionTest()
    {
        var query = "select NothingToDo(Self) from @A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "@A",
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
        var table = vm.Run();

        Assert.AreEqual(1, table.Columns.Count());
        Assert.AreEqual("NothingToDo(Self)", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(BasicEntity), table.Columns.ElementAt(0).ColumnType);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(typeof(BasicEntity), table[0].Values[0].GetType());
    }

    [TestMethod]
    public void TableShouldReturnComplexTypeTest()
    {
        var query = "select Self from @A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "@A",
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
        var table = vm.Run();

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
        var query = "select 1, *, Name as Name2, ToString(Self) as SelfString from @A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {"@A", [entity]}
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();
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

        Assert.AreEqual("Self", table.Columns.ElementAt(5).ColumnName);
        Assert.AreEqual(typeof(BasicEntity), table.Columns.ElementAt(5).ColumnType);

        Assert.AreEqual("Money", table.Columns.ElementAt(6).ColumnName);
        Assert.AreEqual(typeof(decimal), table.Columns.ElementAt(6).ColumnType);

        Assert.AreEqual("Month", table.Columns.ElementAt(7).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(7).ColumnType);

        Assert.AreEqual("Time", table.Columns.ElementAt(8).ColumnName);
        Assert.AreEqual(typeof(DateTime), table.Columns.ElementAt(8).ColumnType);

        Assert.AreEqual("Id", table.Columns.ElementAt(9).ColumnName);
        Assert.AreEqual(typeof(int), table.Columns.ElementAt(9).ColumnType);

        Assert.AreEqual("NullableValue", table.Columns.ElementAt(10).ColumnName);
        Assert.AreEqual(typeof(int?), table.Columns.ElementAt(10).ColumnType);
            
        Assert.AreEqual("Array", table.Columns.ElementAt(11).ColumnName);
        Assert.AreEqual(typeof(int[]), table.Columns.ElementAt(11).ColumnType);
            
        Assert.AreEqual("Other", table.Columns.ElementAt(12).ColumnName);
        Assert.AreEqual(typeof(BasicEntity), table.Columns.ElementAt(12).ColumnType);
            
        Assert.AreEqual("Dictionary", table.Columns.ElementAt(13).ColumnName);
        Assert.AreEqual(typeof(Dictionary<string, string>), table.Columns.ElementAt(13).ColumnType);

        Assert.AreEqual("Name2", table.Columns.ElementAt(14).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(14).ColumnType);

        Assert.AreEqual("SelfString", table.Columns.ElementAt(15).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(15).ColumnType);

        Assert.IsTrue(table.Count == 1, "Table should have 1 entry");

        Assert.IsTrue(table.Any(entry => 
            (int)entry.Values[0] == Convert.ToInt32(1) &&
            (string)entry.Values[1] == "ABBA" &&
            (string)entry.Values[2] == "CRACOV" &&
            (string)entry.Values[3] == "POLAND" &&
            (decimal)entry.Values[4] == 250m &&
            entry.Values[5] == entity &&
            (decimal)entry.Values[6] == 1.23m &&
            (string)entry.Values[7] == "JANUARY" &&
            (DateTime)entry.Values[8] == DateTime.MaxValue &&
            (int)entry.Values[9] == 5 &&
            entry.Values[10] == null &&
            entry.Values[11] != null &&
            entry.Values[12] == entity &&
            entry.Values[13] != null &&
            (string)entry.Values[14] == "ABBA" &&
            (string)entry.Values[15] == "TEST STRING"
        ), "Entry should match all specified values");
    }

    [TestMethod]
    public void SimpleAccessArrayTest()
    {
        var query = @"select Self.Array[2] from @A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {"@A", [new BasicEntity("001"), new BasicEntity("002")]}
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual("Self.Array[2]", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(int), table.Columns.ElementAt(0).ColumnType);
        
        Assert.IsTrue(table.Count == 2, "Table should have 2 entries");

        Assert.IsTrue(table.All(entry => (int)entry.Values[0] == 2), "Both entries should have value 2");
    }

    [TestMethod]
    public void SimpleAccessObjectTest()
    {
        var query = @"select Self.Array from @A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {"@A", [new BasicEntity("001")]}
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual("Self.Array", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(int[]), table.Columns.ElementAt(0).ColumnType);

        Assert.AreEqual(1, table.Count);
    }

    [TestMethod]
    public void AccessObjectTest()
    {
        var query = @"select Self.Self.Array from @A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {"@A", [new BasicEntity("001")]}
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual("Self.Self.Array", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(int[]), table.Columns.ElementAt(0).ColumnType);

        Assert.AreEqual(1, table.Count);
    }

    [TestMethod]
    public void SimpleAccessObjectIncrementTest()
    {
        var query = @"select Inc(Self.Array[2]) from @A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {"@A", [new BasicEntity("001"), new BasicEntity("002")]}
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual("Inc(Self.Array[2])", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(long), table.Columns.ElementAt(0).ColumnType);
        
        Assert.IsTrue(table.Count == 2, "Table should have 2 entries");

        Assert.IsTrue(table.All(entry => (long)entry.Values[0] == Convert.ToInt64(3)), "Both entries should have value 3 (as long)");
    }

    [TestMethod]
    public void WhereWithOrTest()
    {
        var query = @"select Name from @A.Entities() where Name = '001' or Name = '005'";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {"@A", [new BasicEntity("001"), new BasicEntity("002"), new BasicEntity("005")]}
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.IsTrue(table.Count == 2, "Table should have 2 entries");

        Assert.IsTrue(table.Any(entry => (string)entry.Values[0] == "001"), "First entry should be '001'");
        Assert.IsTrue(table.Any(entry => (string)entry.Values[0] == "005"), "Second entry should be '005'");
    }

    [TestMethod]
    public void SimpleQueryTest()
    {
        var query = @"select Name as 'x1' from @A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {"@A", [new BasicEntity("001"), new BasicEntity("002")]}
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();
        
        Assert.IsTrue(table.Count == 2, "Table should have 2 entries");

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
        var query = @"select Name from @A.Entities() skip 2";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "@A",
                [
                    new BasicEntity("001"), new BasicEntity("002"), new BasicEntity("003"), new BasicEntity("004"),
                    new BasicEntity("005"), new BasicEntity("006")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.IsTrue(table.Count == 4, "Table should have 4 entries");

        Assert.IsTrue(table.Any(entry => (string)entry.Values[0] == "003"), "First entry should be '003'");
        Assert.IsTrue(table.Any(entry => (string)entry.Values[0] == "004"), "Second entry should be '004'");
        Assert.IsTrue(table.Any(entry => (string)entry.Values[0] == "005"), "Third entry should be '005'");
        Assert.IsTrue(table.Any(entry => (string)entry.Values[0] == "006"), "Fourth entry should be '006'");
    }

    [TestMethod]
    public void SimpleTakeTest()
    {
        var query = @"select Name from @A.Entities() take 2";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "@A",
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
        var table = vm.Run();

        Assert.IsTrue(table.Count == 2, "Table should have 2 entries");

        Assert.IsTrue(table.Any(entry => (string)entry.Values[0] == "001"), "First entry should be '001'");
        Assert.IsTrue(table.Any(entry => (string)entry.Values[0] == "002"), "Second entry should be '002'");
    }

    [TestMethod]
    public void GetHexTest()
    {
        var query = @"select ToHex(GetBytes(5), '|') as hexValue from @A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "@A",
                [
                    new BasicEntity("001")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("05|00", table[0][0]);
    }

    [TestMethod]
    public void SimpleSkipTakeTest()
    {
        var query = @"select Name from @A.Entities() skip 1 take 2";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "@A",
                [
                    new BasicEntity("001"), new BasicEntity("002"), new BasicEntity("003"), new BasicEntity("004"),
                    new BasicEntity("005"), new BasicEntity("006")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.IsTrue(table.Count == 2, "Table should have 2 entries");

        Assert.IsTrue(table.Any(entry => (string)entry.Values[0] == "002"), "First entry should be '002'");
        Assert.IsTrue(table.Any(entry => (string)entry.Values[0] == "003"), "Second entry should be '003'");
    }

    [TestMethod]
    public void SimpleSkipAboveTableAmountTest()
    {
        var query = @"select Name from @A.Entities() skip 100";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "@A",
                [
                    new BasicEntity("001"), new BasicEntity("002"), new BasicEntity("003"), new BasicEntity("004"),
                    new BasicEntity("005"), new BasicEntity("006")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(0, table.Count);
    }

    [TestMethod]
    public void SimpleTakeAboveTableAmountTest()
    {
        var query = @"select Name from @A.Entities() take 100";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "@A",
                [
                    new BasicEntity("001"), new BasicEntity("002"), new BasicEntity("003"), new BasicEntity("004"),
                    new BasicEntity("005"), new BasicEntity("006")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

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
        var query = @"select Name from @A.Entities() skip 100 take 100";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "@A",
                [
                    new BasicEntity("001"), new BasicEntity("002"), new BasicEntity("003"), new BasicEntity("004"),
                    new BasicEntity("005"), new BasicEntity("006")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(0, table.Count);
    }

    [TestMethod]
    public void ColumnNamesSimpleTest()
    {
        var query =
            @"select Name as TestName, GetOne(), GetOne() as TestColumn, GetTwo(4d, 'test') from @A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {"@A", []}
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

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
        var query = @"select Concat(Country, ToString(Population)) from @A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "@A", [
                    new BasicEntity("ABBA", 200)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(1, table.Columns.Count());
        Assert.AreEqual("Concat(Country, ToString(Population))", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("ABBA200", table[0].Values[0]);
    }

    [TestMethod]
    public void ColumnTypeDateTimeTest()
    {
        var query = "select Time from @A.entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "@A", [
                    new BasicEntity(DateTime.MinValue)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(1, table.Columns.Count());
        Assert.AreEqual("Time", table.Columns.ElementAt(0).ColumnName);

        Assert.AreEqual(1, table.Count());
        Assert.AreEqual(DateTime.MinValue, table[0].Values[0]);
    }

    [TestMethod]
    public void SimpleRowNumberStatTest()
    {
        var query = @"select RowNumber() from @A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "@A", [
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
        var table = vm.Run();

        Assert.IsTrue(table.Count == 6, "Table should have 6 entries");

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
        var query = "select 1.0, -1.0 from @A.entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "@A", [
                    new BasicEntity("xX")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

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
        var query = "desc @A.entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "@A", [
                    new BasicEntity("xX")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(3, table.Columns.Count());

        Assert.AreEqual("Name", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);

        Assert.AreEqual("Index", table.Columns.ElementAt(1).ColumnName);
        Assert.AreEqual(typeof(int), table.Columns.ElementAt(1).ColumnType);

        Assert.AreEqual("Type", table.Columns.ElementAt(2).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(2).ColumnType);

        Assert.IsTrue(table.Any(row => (string) row[0] == "Name" && (string) row[2] == "System.String"));
        Assert.IsTrue(table.Any(row => (string) row[0] == "City" && (string) row[2] == "System.String"));
        Assert.IsTrue(table.Any(row => (string) row[0] == "Country" && (string) row[2] == "System.String"));
        Assert.IsTrue(table.Any(row => (string) row[0] == "Self" && (string) row[2] == "Musoq.Evaluator.Tests.Schema.Basic.BasicEntity"));
        Assert.IsTrue(table.Any(row => (string) row[0] == "Money" && (string) row[2] == "System.Decimal"));
        Assert.IsTrue(table.Any(row => (string) row[0] == "Month" && (string) row[2] == "System.String"));
        Assert.IsTrue(table.Any(row => (string) row[0] == "Time" && (string) row[2] == "System.DateTime"));
        Assert.IsTrue(table.Any(row => (string) row[0] == "Id" && (string) row[2] == "System.Int32"));
        Assert.IsTrue(table.Any(row => (string) row[0] == "NullableValue" && (string) row[2] == "System.Nullable`1[[System.Int32, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]"));
    }

    [TestMethod]
    public void DescMethodTest()
    {
        var query = "desc @A.entities";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "@A", [
                    new BasicEntity("xX")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(1, table.Columns.Count());
        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("entities", table[0][0]);
    }

    [TestMethod]
    public void DescSchemaTest()
    {
        var query = "desc @A";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "@A", [
                    new BasicEntity("xX")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(1, table.Columns.Count());
        Assert.AreEqual(2, table.Count);

        Assert.AreEqual("empty", table[0][0]);
        Assert.AreEqual("entities", table[1][0]);
    }

    [TestMethod]
    public void AggregateValuesTest()
    {
        var query = @"select AggregateValues(Name) from @A.entities() a group by Name";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "@A", [
                    new BasicEntity("A"),
                    new BasicEntity("B")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();
        
        Assert.IsTrue(table.Count == 2, "Table should contain 2 rows");

        Assert.IsTrue(table.Any(row => (string)row[0] == "A") &&
                      table.Any(row => (string)row[0] == "B"),
            "Expected rows with values A and B");
    }

    [TestMethod]
    public void AggregateValuesParentTest()
    {
        var query = @"select AggregateValues(Name, 1) from @A.entities() a group by Name";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "@A", [
                    new BasicEntity("A"),
                    new BasicEntity("B")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual("A,B", table[0][0]);
    }

    [TestMethod]
    public void CoalesceTest()
    {
        var query = @"select Coalesce('a', 'b', 'c', 'e', 'f') from @A.entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "@A", [
                    new BasicEntity("A")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual("a", table[0][0]);
    }

    [TestMethod]
    public void ChooseTest()
    {
        var query = @"select Choose(2, 'a', 'b', 'c', 'e', 'f') from @A.entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "@A", [
                    new BasicEntity("A")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual("c", table[0][0]);
    }

    [TestMethod]
    public void MatchWithRegexTest()
    {
        var query = @"select Match('\d{7}', Name) from @A.entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "@A", [
                    new BasicEntity("3213213")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(true, table[0][0]);
    }

    [TestMethod]
    public void HeadWithStringTest()
    {
        var query = "select Head('ABCDEF', 2) from @A.entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "@A", [
                    new BasicEntity("3213213")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual("AB", table[0][0]);
    }

    [TestMethod]
    public void TailWithStringTest()
    {
        var query = "select Tail('ABCDEF', 2) from @A.entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "@A", [
                    new BasicEntity("3213213")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual("EF", table[0][0]);
    }

    [TestMethod]
    public void SubtractTwoAliasedValuesTest()
    {
        var query = "select a.Money - a.Money from @A.entities() a";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "@A", [
                    new BasicEntity("may", 2512m)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(0m, table[0][0]);
    }

    [TestMethod]
    public void SubtractThreeAliasedValuesTest()
    {
        var query = "select (a.Money - a.Population) / a.Money from @A.entities() a";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "@A", [
                    new BasicEntity("may", 100m) { Population = 10 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(0.9m, table[0][0]);
    }

    [TestMethod]
    public void FilterByComplexObjectAccessInWhereTest()
    {
        var query = "select Population from @A.entities() where Self.Money > 100";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "@A", [
                    new BasicEntity("may", 100m) { Population = 10 },
                    new BasicEntity("june", 200m) { Population = 20 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(20m, table[0][0]);
    }

    [TestMethod]
    public void ComputeStDevTest()
    {
        var query = "select StDev(Population) from @A.entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "@A", [
                    new BasicEntity("may", 100m) { Population = 10 },
                    new BasicEntity("june", 200m) { Population = 20 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.IsTrue(0.001m > (decimal)table[0][0] - 7.071m);
    }

    [TestMethod]
    public void CaseWhenSimpleTest()
    {
        var query = "select " +
                    "   (case " +
                    "       when Population > 100d" +
                    "       then true" +
                    "       else false" +
                    "   end)" +
                    "from @A.entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "@A", [
                    new BasicEntity("may", 100m) { Population = 100 },
                    new BasicEntity("june", 200m) { Population = 200 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();
        
        Assert.IsTrue(table.Count == 2, "Table should have 2 entries");

        Assert.IsTrue(table.Any(entry => (bool)entry[0] == false), "First entry should be false");
        Assert.IsTrue(table.Any(entry => (bool)entry[0] == true), "Second entry should be true");
    }

    [TestMethod]
    public void CaseWhenWithLibraryMethodCallTest()
    {
        var query = "select " +
                    "   (case " +
                    "       when Population > 100d" +
                    "       then entities.GetOne()" +
                    "       else entities.Inc(entities.GetOne())" +
                    "   end)" +
                    "from @A.entities() entities";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "@A", [
                    new BasicEntity("may", 100m) { Population = 100 },
                    new BasicEntity("june", 200m) { Population = 200 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.IsTrue(table.Count == 2, "Table should contain 2 rows");

        Assert.IsTrue(table.Any(row => (decimal)row[0] == 2m) && 
                      table.Any(row => (decimal)row[0] == 1m),
            "Expected rows with values 2 and 1");
    }

    [TestMethod]
    public void MultipleCaseWhenWithLibraryMethodCallTest()
    {
        var query = "select " +
                    "   (case " +
                    "       when Population > 100d" +
                    "       then entities.GetOne()" +
                    "       else entities.Inc(entities.GetOne())" +
                    "   end)," +
                    "   (case " +
                    "       when Population <= 100d" +
                    "       then entities.GetOne()" +
                    "       else entities.Inc(entities.GetOne())" +
                    "   end)" +
                    "from @A.entities() entities";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "@A", [
                    new BasicEntity("may", 100m) { Population = 100 },
                    new BasicEntity("june", 200m) { Population = 200 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.IsTrue(table.Count == 2, "Table should contain 2 rows");

        Assert.IsTrue(table.Any(row => 
                (decimal)row[0] == 2m && 
                (decimal)row[1] == 1m),
            "Row with values (2,1) not found");

        Assert.IsTrue(table.Any(row => 
                (decimal)row[0] == 1m && 
                (decimal)row[1] == 2m),
            "Row with values (1,2) not found");
    }

    [TestMethod]
    public void QueryWithTimeSpanTest()
    {
        var query = "select ToTimeSpan('00:12:15') from @A.entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "@A", [
                    new BasicEntity("may", 100m) { Population = 100 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(new TimeSpan(0, 12, 15), table[0][0]);
    }

    [TestMethod]
    public void QueryWithToDateTimeTest()
    {
        var query = "select ToDateTime('2012/01/13') from @A.entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "@A", [
                    new BasicEntity("may", 100m) { Population = 100 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(new DateTime(2012, 1, 13), table[0][0]);
    }

    [TestMethod]
    public void QueryWithToDateTimeAndTimeSpanAdditionTest()
    {
        var query = "select ToDateTime('2012/01/13') + ToTimeSpan('00:12:15') from @A.entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "@A", [
                    new BasicEntity("may", 100m) { Population = 100 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(new DateTime(2012, 1, 13, 0, 12, 15), table[0][0]);
    }

    [TestMethod]
    public void QueryWithTimeSpansAdditionTest()
    {
        var query = "select ToTimeSpan('00:12:15') + ToTimeSpan('00:12:15') from @A.entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "@A", [
                    new BasicEntity("may", 100m) { Population = 100 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(new TimeSpan(0, 24, 30), table[0][0]);
    }

    [TestMethod]
    public void RegexMatchesIntegrationTest()
    {
        var query = @"select RegexMatches('\d+', Name) from @A.entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "@A", [
                    new BasicEntity("Test 123 and 456")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(1, table.Columns.Count());
        Assert.AreEqual(typeof(string[]), table.Columns.ElementAt(0).ColumnType);

        Assert.AreEqual(1, table.Count);
        var result = (string[])table[0].Values[0];
        Assert.AreEqual(2, result.Length);
        Assert.AreEqual("123", result[0]);
        Assert.AreEqual("456", result[1]);
    }
}