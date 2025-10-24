using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class NullabilityTests : BasicEntityTestBase
{
    [TestMethod]
    public void QueryWithWhereWithNullableValueResultTest()
    {
        var query = "select NullableValue from #A.Entities() where NullableValue is not null and NullableValue <> 5";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity{NullableValue = 1},
                    new BasicEntity{NullableValue = null},
                    new BasicEntity{NullableValue = 2}
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();
        
        Assert.AreEqual(2, table.Count, "Table should contain 2 rows");
        Assert.IsTrue(table.Any(row => (int)row.Values[0] == 1) && table.Any(row => (int)row.Values[0] == 2), "Expected values 1 and 2 not found");
    }

    [TestMethod]
    public void WhenOneOfResultsAreExplicitlyNull_ShouldInferNullabilityTypeFromQueryContext()
    {
        var query = "select (case when NullableValue is null then 0 else null end) from #A.Entities()";
            
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity{NullableValue = null},
                    new BasicEntity{NullableValue = 125}
                ]
            }
        };
            
        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();
        
        Assert.AreEqual(2, table.Count, "Table should have 2 entries");

        Assert.IsTrue(table.Where(entry => entry.Values[0] != null).Any(entry => (int)entry.Values[0] == 0), "First entry should be 0");
        Assert.IsTrue(table.Any(entry => entry.Values[0] == null), "Second entry should be null");
    }

    [TestMethod]
    public void QueryWithWhereWithNullableMethodResultTest()
    {
        var query = "select NullableValue from #A.Entities() where NullableMethod(NullableValue) is not null and NullableMethod(NullableValue) <> 5";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity{ NullableValue = 1 },
                    new BasicEntity{ NullableValue = null },
                    new BasicEntity{ NullableValue = 2 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();
        
        Assert.AreEqual(2, table.Count, "Table should have 2 entries");

        Assert.IsTrue(table.Any(entry => (int)entry.Values[0] == 1), "First entry should be 1");
        Assert.IsTrue(table.Any(entry => (int)entry.Values[0] == 2), "Second entry should be 2");
    }

    [TestMethod]
    public void GroupBySingleColumnWithNullGroupTest()
    {
        var query = @"select Name from #A.Entities() group by Name";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("ABBA"),
                    new BasicEntity("ABBA"),
                    new BasicEntity("BABBA"),
                    new BasicEntity("ABBA"),
                    new BasicEntity("BABBA"),
                    new BasicEntity("CECCA"),
                    new BasicEntity("ABBA"),
                    new BasicEntity(null),
                    new BasicEntity(null)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(1, table.Columns.Count());
        Assert.AreEqual("Name", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);
        
        Assert.AreEqual(4, table.Count, "Table should have 4 entries");

        Assert.IsTrue(table.Any(entry => (string)entry.Values[0] == "ABBA"), "First entry should be 'ABBA'");
        Assert.IsTrue(table.Any(entry => (string)entry.Values[0] == "BABBA"), "Second entry should be 'BABBA'");
        Assert.IsTrue(table.Any(entry => (string)entry.Values[0] == "CECCA"), "Third entry should be 'CECCA'");
        Assert.IsTrue(table.Any(entry => entry.Values[0] == null), "Fourth entry should be null");
    }

    [TestMethod]
    public void GroupByMultiColumnWithNullGroupTest()
    {
        var query = @"select Country, City from #A.Entities() group by Country, City";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("POLAND", "WARSAW"),
                    new BasicEntity("POLAND", null),
                    new BasicEntity("UK", "LONDON"),
                    new BasicEntity("POLAND", null),
                    new BasicEntity("UK", "MANCHESTER"),
                    new BasicEntity("ANGOLA", "LLL"),
                    new BasicEntity("POLAND", "WARSAW")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(2, table.Columns.Count());
        Assert.AreEqual("Country", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);
        Assert.AreEqual("City", table.Columns.ElementAt(1).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(1).ColumnType);

        Assert.AreEqual(5, table.Count, "Table should have 5 entries");

        Assert.IsTrue(table.Any(entry => 
            (string)entry.Values[0] == "POLAND" && 
            (string)entry.Values[1] == "WARSAW"
        ), "First entry should be POLAND, WARSAW");

        Assert.IsTrue(table.Any(entry => 
            (string)entry.Values[0] == "POLAND" && 
            entry.Values[1] == null
        ), "Second entry should be POLAND with null city");

        Assert.IsTrue(table.Any(entry => 
            (string)entry.Values[0] == "UK" && 
            (string)entry.Values[1] == "LONDON"
        ), "Third entry should be UK, LONDON");

        Assert.IsTrue(table.Any(entry => 
            (string)entry.Values[0] == "UK" && 
            (string)entry.Values[1] == "MANCHESTER"
        ), "Fourth entry should be UK, MANCHESTER");

        Assert.IsTrue(table.Any(entry => 
            (string)entry.Values[0] == "ANGOLA" && 
            (string)entry.Values[1] == "LLL"
        ), "Fifth entry should be ANGOLA, LLL");
    }

    [TestMethod]
    public void IsNotNullReferenceTypeTest()
    {
        var query = @"select Name from #A.Entities() where Name is not null";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("001"), new BasicEntity(null), new BasicEntity("003"), new BasicEntity(null),
                    new BasicEntity("005"), new BasicEntity("006")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(4, table.Count, "Table should have 4 entries");
        Assert.IsTrue(table.Any(entry => (string)entry.Values[0] == "001"), "First entry should be '001'");
        Assert.IsTrue(table.Any(entry => (string)entry.Values[0] == "003"), "Second entry should be '003'");
        Assert.IsTrue(table.Any(entry => (string)entry.Values[0] == "005"), "Third entry should be '005'");
        Assert.IsTrue(table.Any(entry => (string)entry.Values[0] == "006"), "Fourth entry should be '006'");
    }

    [TestMethod]
    public void IsNullReferenceTypeTest()
    {
        var query = @"select City from #A.Entities() where Country is null";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("Poland", "Gdansk"), new BasicEntity(null, "Warsaw"), new BasicEntity("France", "Paris"), new BasicEntity(null, "Bratislava")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();
        
        Assert.AreEqual(2, table.Count, "Table should have 2 entries");

        Assert.IsTrue(table.Any(entry => (string)entry.Values[0] == "Warsaw"), "First entry should be Warsaw");
        Assert.IsTrue(table.Any(entry => (string)entry.Values[0] == "Bratislava"), "Second entry should be Bratislava");
    }


    [TestMethod]
    public void IsNotNullValueTypeTest()
    {
        var query = @"select Population from #A.Entities() where Population is not null";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("ABC", 100), new BasicEntity("CBA", 200), new BasicEntity("aaa")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();
        
        Assert.AreEqual(3, table.Count, "Table should have 3 entries");

        Assert.IsTrue(table.Any(entry => 
                (decimal)entry.Values[0] == 100m), 
            "First entry should be 100m");

        Assert.IsTrue(table.Any(entry => 
                (decimal)entry.Values[0] == 200m), 
            "Second entry should be 200m");

        Assert.IsTrue(table.Any(entry => 
                (decimal)entry.Values[0] == 0m), 
            "Third entry should be 0m");
    }

    [TestMethod]
    public void IsNullValueTypeTest()
    {
        var query = @"select Population from #A.Entities() where Population is null";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("ABC", 100), 
                    new BasicEntity("CBA", 200), 
                    new BasicEntity("aaa")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(0, table.Count);
    }

    [TestMethod]
    public void OrOperator_WhenLeftFieldIsNull_ShouldShowLeftNull()
    {
        var query = @"select Country, City from #A.Entities() where City is null or Country = 'England'";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("Poland", "Warsaw"), 
                    new BasicEntity("England", "London"), 
                    new BasicEntity("Brazil", null),
                    new BasicEntity(null, "Bratislava"),
                    new BasicEntity(null, null)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();
        
        Assert.AreEqual(3, table.Count, "Table should have 3 entries");

        Assert.IsTrue(table.Any(entry => 
            (string)entry.Values[0] == "England" && 
            (string)entry.Values[1] == "London"
        ), "First entry should be England, London");

        Assert.IsTrue(table.Any(entry => 
            (string)entry.Values[0] == "Brazil" && 
            entry.Values[1] == null
        ), "Second entry should be Brazil with null city");

        Assert.IsTrue(table.Any(entry => 
            entry.Values[0] == null && 
            entry.Values[1] == null
        ), "Third entry should have null values");
    }

    [TestMethod]
    public void OrOperator_WhenRightFieldIsNull_ShouldShowLeftNull()
    {
        var query = @"select Country, City from #A.Entities() where Country = 'Poland' or Country is null";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("Poland", "Warsaw"), 
                    new BasicEntity("England", "London"), 
                    new BasicEntity("Brazil", null),
                    new BasicEntity(null, "Bratislava"),
                    new BasicEntity(null, null)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(3, table.Count, "Table should contain 3 rows");

        Assert.IsTrue(table.Any(row => 
                (string)row.Values[0] == "Poland" && 
                (string)row.Values[1] == "Warsaw"), 
            "Row with Poland/Warsaw not found");

        Assert.IsTrue(table.Any(row => 
                row.Values[0] == null && 
                (string)row.Values[1] == "Bratislava"),
            "Row with null/Bratislava not found");

        Assert.IsTrue(table.Any(row => 
                row.Values[0] == null && 
                row.Values[1] == null),
            "Row with null/null not found");
    }

    [TestMethod]
    public void OrOperator_WhenBothFieldsAreNull_ShouldShowThreeRows()
    {
        var query = @"select Country, City from #A.Entities() where City is null or Country is null";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("Poland", "Warsaw"), 
                    new BasicEntity("England", "London"), 
                    new BasicEntity("Brazil", null),
                    new BasicEntity(null, "Bratislava"),
                    new BasicEntity(null, null)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();
        
        Assert.AreEqual(3, table.Count, "Table should contain 3 rows");

        Assert.IsTrue(table.Any(row => 
                (string)row.Values[0] == "Brazil" && 
                row.Values[1] == null),
            "Expected row with Brazil and null");

        Assert.IsTrue(table.Any(row => 
                row.Values[0] == null && 
                (string)row.Values[1] == "Bratislava"),
            "Expected row with null and Bratislava");

        Assert.IsTrue(table.Any(row => 
                row.Values[0] == null && 
                row.Values[1] == null),
            "Expected row with both nulls");
    }
        
    [TestMethod]
    public void AndOperator_WhenLeftFieldIsNull_ShouldShowLeftNull()
    {
        var query = @"select Country, City from #A.Entities() where City is null and Country = 'Brazil'";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("Poland", "Warsaw"), 
                    new BasicEntity("England", "London"), 
                    new BasicEntity("Brazil", null),
                    new BasicEntity(null, "Bratislava"),
                    new BasicEntity(null, null)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
            
        Assert.AreEqual("Brazil", table[0].Values[0]);
        Assert.IsNull(table[0].Values[1]);
    }

    [TestMethod]
    public void AndOperator_WhenRightFieldIsNull_ShouldShowLeftNull()
    {
        var query = @"select Country, City from #A.Entities() where City = 'Bratislava' and Country is null";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("Poland", "Warsaw"), 
                    new BasicEntity("England", "London"), 
                    new BasicEntity("Brazil", null),
                    new BasicEntity(null, "Bratislava"),
                    new BasicEntity(null, null)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();
            
        Assert.AreEqual(1, table.Count);
            
        Assert.IsNull(table[0].Values[0]);
        Assert.AreEqual("Bratislava", table[0].Values[1]);
    }

    [TestMethod]
    public void AndOperator_WhenBothFieldsAreNull_ShouldShowThreeRows()
    {
        var query = @"select Country, City from #A.Entities() where City is null and Country is null";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("Poland", "Warsaw"), 
                    new BasicEntity("England", "London"), 
                    new BasicEntity("Brazil", null),
                    new BasicEntity(null, "Bratislava"),
                    new BasicEntity(null, null)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
            
        Assert.IsNull(table[0].Values[0]);
        Assert.IsNull(table[0].Values[1]);
    }
        
    [TestMethod]
    public void WhenMethodCalledWithNullValue_ShouldReturnNull()
    {
        var query = @"select NullableMethod(null) from #A.Entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity{ NullableValue = 1 },
                    new BasicEntity{ NullableValue = null },
                    new BasicEntity{ NullableValue = 2 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(3, table.Count, "Table should have 3 entries");

        Assert.IsTrue(table.All(entry => entry.Values[0] == null), "All entries should have null first value");
    }
}
