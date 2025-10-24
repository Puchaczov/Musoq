using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Generic;
using Musoq.Plugins.Attributes;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class OuterApplyMixedTests : GenericEntityTestBase
{
    private class OuterApplyClass1
    {
        public string City { get; set; }

        public string Country { get; set; }
    }

    // ReSharper disable once MemberCanBePrivate.Global
    public class ComplexType1
    {
        public string StreetName { get; set; }

        public int HouseNumber { get; set; }

        [BindablePropertyAsTable] public ComplexType1[] Addresses { get; set; }
    }

    private class OuterApplyClass2
    {
        public string Country { get; set; }

        [BindablePropertyAsTable] public ComplexType1[] Addresses { get; set; }
    }

    [TestMethod]
    public void OuterApply_SchemaAndProperty_WithNestedProperty_ShouldPass()
    {
        const string query = @"
        select 
            a.City, 
            b.Country, 
            c.StreetName, 
            c.HouseNumber 
        from #schema.first() a 
        outer apply #schema.second(a.Country) b 
        outer apply b.Addresses c";

        var firstSource = new OuterApplyClass1[]
        {
            new() {Country = "USA", City = "New York"},
            new() {Country = "USA", City = "Los Angeles"},
        };

        var secondSource = new OuterApplyClass2[]
        {
            new()
            {
                Country = "USA",
                Addresses =
                [
                    new() {StreetName = "Broadway", HouseNumber = 123},
                    new() {StreetName = "Fifth Avenue", HouseNumber = 456}
                ]
            },
            new()
            {
                Country = "Canada",
                Addresses =
                [
                    new ComplexType1 {StreetName = "Yonge Street", HouseNumber = 789}
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(
            query,
            firstSource,
            secondSource,
            null,
            null,
            null,
            (parameters, source) =>
                new ObjectRowsSource(source.Rows.Where(f => (string) f["Country"] == (string) parameters[0]).ToArray())
        );

        var table = vm.Run();

        Assert.AreEqual(4, table.Columns.Count());
        Assert.AreEqual("a.City", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(1).ColumnType);
        Assert.AreEqual("b.Country", table.Columns.ElementAt(1).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(1).ColumnType);
        Assert.AreEqual("c.StreetName", table.Columns.ElementAt(2).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(2).ColumnType);
        Assert.AreEqual("c.HouseNumber", table.Columns.ElementAt(3).ColumnName);
        Assert.AreEqual(typeof(int?), table.Columns.ElementAt(3).ColumnType);

        Assert.AreEqual(4, table.Count);

        Assert.IsTrue(table.Count(r => 
            (string)r[0] == "New York" &&
            (string)r[1] == "USA" &&
            (string)r[2] == "Broadway" &&
            (int)r[3] == 123) == 1);

        Assert.IsTrue(table.Count(r => 
            (string)r[0] == "New York" &&
            (string)r[1] == "USA" &&
            (string)r[2] == "Fifth Avenue" &&
            (int)r[3] == 456) == 1);

        Assert.IsTrue(table.Count(r => 
            (string)r[0] == "Los Angeles" &&
            (string)r[1] == "USA" &&
            (string)r[2] == "Broadway" &&
            (int)r[3] == 123) == 1);

        Assert.IsTrue(table.Count(r => 
            (string)r[0] == "Los Angeles" && 
            (string)r[1] == "USA" &&
            (string)r[2] == "Fifth Avenue" &&
            (int)r[3] == 456) == 1);
    }

    private class OuterApplyClass3
    {
        public string Department { get; set; }

        public int Budget { get; set; }
    }

    private class OuterApplyClass4
    {
        public string Department { get; set; }

        public string Name { get; set; }

        public int Salary { get; set; }

        public string[] Skills { get; set; }
    }

    [TestMethod]
    public void OuterApply_SchemaAndMethod_WithComplexObjects_ShouldPass()
    {
        const string query = @"
        select 
            a.Department,
            a.Budget,
            b.Name, 
            b.Salary,
            c.Value
        from #schema.first() a 
        outer apply #schema.second(a.Department) b 
        outer apply b.Distinct(b.Skills) c";

        var firstSource = new OuterApplyClass3[]
        {
            new() {Department = "IT", Budget = 500000},
            new() {Department = "HR", Budget = 300000}
        };

        var secondSource = new OuterApplyClass4[]
        {
            new() {Department = "IT", Name = "John Doe", Salary = 50000, Skills = ["C#", "JavaScript", "C#"]},
            new() {Department = "IT", Name = "Jane Smith", Salary = 60000, Skills = ["C#", "JavaScript"]},
            new() {Department = "HR", Name = "John Doe", Salary = 50000, Skills = ["Communication", "Negotiation"]},
            new()
            {
                Department = "HR", Name = "Jane Smith", Salary = 60000,
                Skills = ["Communication", "Negotiation", "Communication"]
            }
        };

        var vm = CreateAndRunVirtualMachine(
            query,
            firstSource,
            secondSource,
            null,
            null,
            null,
            (parameters, source) =>
                new ObjectRowsSource(source.Rows.Where(f => (string) f["Department"] == (string) parameters[0])
                    .ToArray()));

        var table = vm.Run();

        Assert.AreEqual(5, table.Columns.Count());
        Assert.AreEqual("a.Department", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);
        Assert.AreEqual("a.Budget", table.Columns.ElementAt(1).ColumnName);
        Assert.AreEqual(typeof(int), table.Columns.ElementAt(1).ColumnType);
        Assert.AreEqual("b.Name", table.Columns.ElementAt(2).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(2).ColumnType);
        Assert.AreEqual("b.Salary", table.Columns.ElementAt(3).ColumnName);
        Assert.AreEqual(typeof(int?), table.Columns.ElementAt(3).ColumnType);
        Assert.AreEqual("c.Value", table.Columns.ElementAt(4).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(4).ColumnType);
        
        Assert.IsTrue(table.Count == 8, "Table should contain 8 rows");

        Assert.IsTrue(table.Count(row => 
                (string)row[0] == "IT" && 
                (int)row[1] == 500000 &&
                new[] { ("John Doe", 50000), ("Jane Smith", 60000) }.Contains(((string)row[2], (int)row[3])) &&
                new[] { "C#", "JavaScript" }.Contains((string)row[4])) == 4,
            "Expected 4 IT rows with correct employee and skill combinations");

        Assert.IsTrue(table.Count(row => 
                (string)row[0] == "HR" && 
                (int)row[1] == 300000 &&
                new[] { ("John Doe", 50000), ("Jane Smith", 60000) }.Contains(((string)row[2], (int)row[3])) &&
                new[] { "Communication", "Negotiation" }.Contains((string)row[4])) == 4,
            "Expected 4 HR rows with correct employee and skill combinations");
    }

    private class OuterApplyClass5
    {
        public string Department { get; set; }
        public int Budget { get; set; }
        
        [BindablePropertyAsTable]
        public ComplexType2[] Employees { get; set; }
    }

    // ReSharper disable once MemberCanBePrivate.Global
    public class ComplexType2
    {
        public string Name { get; set; }
        
        public string[] Skills { get; set; }
    }

    [TestMethod]
    public void OuterApply_PropertyAndMethod_WithFiltering_ShouldPass()
    {
        const string query = @"
    select 
        a.Department,
        b.Name,
        c.Value
    from #schema.first() a 
    outer apply a.Employees b 
    outer apply a.Distinct(b.Skills) c
    where a.Budget > 400000";

        var firstSource = new OuterApplyClass5[]
        {
            new()
            {
                Department = "IT",
                Budget = 500000,
                Employees =
                [
                    new ComplexType2
                        {Name = "John Doe", Skills = ["C#", "C#"]},
                    new ComplexType2
                        {Name = "Jane Smith", Skills = ["Java"]}
                ]
            },
            new()
            {
                Department = "HR",
                Budget = 300000,
                Employees =
                [
                    new ComplexType2
                    {
                        Name = "Alice Johnson",
                        Skills = ["Communication", "Negotiation", "Communication", "Negotiation"]
                    }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, firstSource);

        var table = vm.Run();
        
        Assert.AreEqual(3, table.Columns.Count());
        Assert.AreEqual("a.Department", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);
        Assert.AreEqual("b.Name", table.Columns.ElementAt(1).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(1).ColumnType);
        Assert.AreEqual("c.Value", table.Columns.ElementAt(2).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(2).ColumnType);
        
        Assert.AreEqual(2, table.Count);
        
        Assert.IsTrue(table.Any(row => (string)row[0] == "IT" &&
                                       (string)row[1] == "John Doe" &&
                                       (string)row[2] == "C#"), "Expected row with IT, John Doe, C# not found");
        Assert.IsTrue(table.Any(row => (string)row[0] == "IT" &&
                                       (string)row[1] == "Jane Smith" &&
                                       (string)row[2] == "Java"), "Expected row with IT, Jane Smith, Java not found");
    }

    [TestMethod]
    public void OuterApply_PropertyAndMethod_GroupBy_WithFiltering_ShouldPass()
    {
        const string query = @"
    select 
        a.Department,
        Count(a.Department)
    from #schema.first() a 
    outer apply a.Employees b 
    outer apply a.Distinct(b.Skills) c
    where a.Budget > 400000
    group by a.Department";

        var firstSource = new OuterApplyClass5[]
        {
            new()
            {
                Department = "IT",
                Budget = 500000,
                Employees =
                [
                    new ComplexType2
                        {Name = "John Doe", Skills = ["C#", "C#"]},
                    new ComplexType2
                        {Name = "Jane Smith", Skills = ["Java"]}
                ]
            },
            new()
            {
                Department = "HR",
                Budget = 300000,
                Employees =
                [
                    new ComplexType2
                    {
                        Name = "Alice Johnson",
                        Skills = ["Communication", "Negotiation", "Communication", "Negotiation"]
                    }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, firstSource);

        var table = vm.Run();
        
        Assert.AreEqual(2, table.Columns.Count());
        Assert.AreEqual("a.Department", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);
        Assert.AreEqual("Count(a.Department)", table.Columns.ElementAt(1).ColumnName);
        Assert.AreEqual(typeof(int), table.Columns.ElementAt(1).ColumnType);
        
        Assert.AreEqual(1, table.Count);
        
        Assert.AreEqual("IT", table[0][0]);
        Assert.AreEqual(2, table[0][1]);
    }

    private class OuterApplyClass6
    {
        public string Name { get; set; }
        
        public string Surname { get; set; }
        
        public int Id { get; set; }
    }
    
    private class OuterApplyClass7
    {   
        public int Id { get; set; }
        
        [BindablePropertyAsTable]
        public string[] Skills { get; set; }
    }
    
    [TestMethod]
    public void OuterApply_InnerJoinAndUseProperty_ShouldPass()
    {
        const string query = @"
    select 
        a.Name,
        a.Surname,
        c.Value
    from #schema.first() a
    inner join #schema.second() b on a.Id = b.Id
    outer apply b.Skills c";
        
        var firstSource = new OuterApplyClass6[]
        {
            new() {Name = "John", Surname = "Doe", Id = 1},
            new() {Name = "Jane", Surname = "Smith", Id = 2},
            new() {Name = "Alice", Surname = "Johnson", Id = 3}
        };

        var secondSource = new OuterApplyClass7[]
        {
            new() {Id = 1, Skills = ["C#", "JavaScript"]},
            new() {Id = 2, Skills = ["Java"]},
            new() {Id = 3, Skills = ["Communication", "Negotiation"]}
        };

        var vm = CreateAndRunVirtualMachine(query, firstSource, secondSource);

        var table = vm.Run();
        
        Assert.AreEqual(3, table.Columns.Count());
        Assert.AreEqual("a.Name", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);
        Assert.AreEqual("a.Surname", table.Columns.ElementAt(1).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(1).ColumnType);
        Assert.AreEqual("c.Value", table.Columns.ElementAt(2).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(2).ColumnType);
        
        Assert.IsTrue(table.Count == 5, "Table should contain 5 rows");

        Assert.IsTrue(table.Count(row => 
                          (string) row[0] == "John" && 
                          (string) row[1] == "Doe") == 2 &&
                      table.Any(row =>
                          (string) row[0] == "John" && 
                          (string) row[1] == "Doe" && 
                          (string) row[2] == "C#") &&
                      table.Any(row =>
                          (string) row[0] == "John" && 
                          (string) row[1] == "Doe" && 
                          (string) row[2] == "JavaScript"),
            "Expected two rows for John Doe with C# and JavaScript skills");

        Assert.IsTrue(table.Count(row =>
                          (string) row[0] == "Jane" && 
                          (string) row[1] == "Smith") == 1 &&
                      table.Any(row =>
                          (string) row[0] == "Jane" && 
                          (string) row[1] == "Smith" && 
                          (string) row[2] == "Java"),
            "Expected one row for Jane Smith with Java skill");

        Assert.IsTrue(table.Count(row =>
                          (string) row[0] == "Alice" && 
                          (string) row[1] == "Johnson") == 2 &&
                      table.Any(row =>
                          (string) row[0] == "Alice" && 
                          (string) row[1] == "Johnson" && 
                          (string) row[2] == "Communication") &&
                      table.Any(row =>
                          (string) row[0] == "Alice" && 
                          (string) row[1] == "Johnson" && 
                          (string) row[2] == "Negotiation"),
            "Expected two rows for Alice Johnson with Communication and Negotiation skills");
    }
    
    [TestMethod]
    public void OuterApply_LeftJoinAndUseProperty_ShouldPass()
    {
        const string query = @"
    select 
        a.Name,
        a.Surname,
        c.Value
    from #schema.first() a
    left outer join #schema.second() b on a.Id = b.Id
    outer apply b.Skills c";
        
        var firstSource = new OuterApplyClass6[]
        {
            new() {Name = "John", Surname = "Doe", Id = 1},
            new() {Name = "Jane", Surname = "Smith", Id = 2},
            new() {Name = "Alice", Surname = "Johnson", Id = 3}
        };

        var secondSource = new OuterApplyClass7[]
        {
            new() {Id = 1, Skills = ["C#", "JavaScript"]},
            new() {Id = 2, Skills = ["Java"]},
            new() {Id = 3, Skills = ["Communication", "Negotiation"]}
        };

        var vm = CreateAndRunVirtualMachine(query, firstSource, secondSource);

        var table = vm.Run();
        
        Assert.AreEqual(3, table.Columns.Count());
        Assert.AreEqual("a.Name", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);
        Assert.AreEqual("a.Surname", table.Columns.ElementAt(1).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(1).ColumnType);
        Assert.AreEqual("c.Value", table.Columns.ElementAt(2).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(2).ColumnType);
        
        Assert.IsTrue(table.Count == 5, "Table should contain 5 rows");

        Assert.IsTrue(table.Count(row => 
                (string)row[0] == "John" && 
                (string)row[1] == "Doe" && 
                new[] { "C#", "JavaScript" }.Contains((string)row[2])) == 2,
            "Expected 2 rows for John Doe with C# and JavaScript skills");

        Assert.IsTrue(table.Any(row => 
                (string)row[0] == "Jane" && 
                (string)row[1] == "Smith" && 
                (string)row[2] == "Java"),
            "Row for Jane Smith with Java skill not found");

        Assert.IsTrue(table.Count(row => 
                (string)row[0] == "Alice" && 
                (string)row[1] == "Johnson" && 
                new[] { "Communication", "Negotiation" }.Contains((string)row[2])) == 2,
            "Expected 2 rows for Alice Johnson with Communication and Negotiation skills");
    }
}
