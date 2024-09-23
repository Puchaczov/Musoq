using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Generic;
using Musoq.Plugins.Attributes;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class CrossApplyMixedTests : GenericEntityTestBase
{
    private class CrossApplyClass1
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

    private class CrossApplyClass2
    {
        public string Country { get; set; }

        [BindablePropertyAsTable] public ComplexType1[] Addresses { get; set; }
    }

    [TestMethod]
    public void CrossApply_SchemaAndProperty_WithNestedProperty()
    {
        const string query = @"
        select 
            a.City, 
            b.Country, 
            c.StreetName, 
            c.HouseNumber 
        from #schema.first() a 
        cross apply #schema.second(a.Country) b 
        cross apply b.Addresses c";

        var firstSource = new CrossApplyClass1[]
        {
            new() {Country = "USA", City = "New York"},
            new() {Country = "USA", City = "Los Angeles"},
        };

        var secondSource = new CrossApplyClass2[]
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
        Assert.AreEqual(typeof(int), table.Columns.ElementAt(3).ColumnType);

        Assert.AreEqual(4, table.Count);

        Assert.AreEqual("New York", table[0][0]);
        Assert.AreEqual("USA", table[0][1]);
        Assert.AreEqual("Broadway", table[0][2]);
        Assert.AreEqual(123, table[0][3]);

        Assert.AreEqual("New York", table[1][0]);
        Assert.AreEqual("USA", table[1][1]);
        Assert.AreEqual("Fifth Avenue", table[1][2]);
        Assert.AreEqual(456, table[1][3]);

        Assert.AreEqual("Los Angeles", table[2][0]);
        Assert.AreEqual("USA", table[2][1]);
        Assert.AreEqual("Broadway", table[2][2]);
        Assert.AreEqual(123, table[2][3]);

        Assert.AreEqual("Los Angeles", table[3][0]);
        Assert.AreEqual("USA", table[3][1]);
        Assert.AreEqual("Fifth Avenue", table[3][2]);
        Assert.AreEqual(456, table[3][3]);
    }

    private class CrossApplyClass3
    {
        public string Department { get; set; }

        public int Budget { get; set; }
    }

    private class CrossApplyClass4
    {
        public string Department { get; set; }

        public string Name { get; set; }

        public int Salary { get; set; }

        public string[] Skills { get; set; }
    }

    [TestMethod]
    public void CrossApply_SchemaAndMethod_WithComplexObjects()
    {
        const string query = @"
        select 
            a.Department,
            a.Budget,
            b.Name, 
            b.Salary,
            c.Value
        from #schema.first() a 
        cross apply #schema.second(a.Department) b 
        cross apply b.Distinct(b.Skills) c";

        var firstSource = new CrossApplyClass3[]
        {
            new() {Department = "IT", Budget = 500000},
            new() {Department = "HR", Budget = 300000}
        };

        var secondSource = new CrossApplyClass4[]
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
        Assert.AreEqual(typeof(int), table.Columns.ElementAt(3).ColumnType);
        Assert.AreEqual("c.Value", table.Columns.ElementAt(4).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(4).ColumnType);

        Assert.AreEqual(8, table.Count);

        Assert.AreEqual("IT", table[0][0]);
        Assert.AreEqual(500000, table[0][1]);
        Assert.AreEqual("John Doe", table[0][2]);
        Assert.AreEqual(50000, table[0][3]);
        Assert.AreEqual("C#", table[0][4]);

        Assert.AreEqual("IT", table[1][0]);
        Assert.AreEqual(500000, table[1][1]);
        Assert.AreEqual("John Doe", table[1][2]);
        Assert.AreEqual(50000, table[1][3]);
        Assert.AreEqual("JavaScript", table[1][4]);

        Assert.AreEqual("IT", table[2][0]);
        Assert.AreEqual(500000, table[2][1]);
        Assert.AreEqual("Jane Smith", table[2][2]);
        Assert.AreEqual(60000, table[2][3]);
        Assert.AreEqual("C#", table[2][4]);

        Assert.AreEqual("IT", table[3][0]);
        Assert.AreEqual(500000, table[3][1]);
        Assert.AreEqual("Jane Smith", table[3][2]);
        Assert.AreEqual(60000, table[3][3]);
        Assert.AreEqual("JavaScript", table[3][4]);

        Assert.AreEqual("HR", table[4][0]);
        Assert.AreEqual(300000, table[4][1]);
        Assert.AreEqual("John Doe", table[4][2]);
        Assert.AreEqual(50000, table[4][3]);
        Assert.AreEqual("Communication", table[4][4]);

        Assert.AreEqual("HR", table[5][0]);
        Assert.AreEqual(300000, table[5][1]);
        Assert.AreEqual("John Doe", table[5][2]);
        Assert.AreEqual(50000, table[5][3]);
        Assert.AreEqual("Negotiation", table[5][4]);

        Assert.AreEqual("HR", table[6][0]);
        Assert.AreEqual(300000, table[6][1]);
        Assert.AreEqual("Jane Smith", table[6][2]);
        Assert.AreEqual(60000, table[6][3]);
        Assert.AreEqual("Communication", table[6][4]);

        Assert.AreEqual("HR", table[7][0]);
        Assert.AreEqual(300000, table[7][1]);
        Assert.AreEqual("Jane Smith", table[7][2]);
        Assert.AreEqual(60000, table[7][3]);
        Assert.AreEqual("Negotiation", table[7][4]);
    }

    private class CrossApplyClass5
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
    public void CrossApply_PropertyAndMethod_WithFiltering()
    {
        const string query = @"
    select 
        a.Department,
        b.Name,
        c.Value
    from #schema.first() a 
    cross apply a.Employees b 
    cross apply a.Distinct(b.Skills) c
    where a.Budget > 400000";

        var firstSource = new CrossApplyClass5[]
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
        
        Assert.AreEqual("IT", table[0][0]);
        Assert.AreEqual("John Doe", table[0][1]);
        Assert.AreEqual("C#", table[0][2]);
        
        Assert.AreEqual("IT", table[1][0]);
        Assert.AreEqual("Jane Smith", table[1][1]);
        Assert.AreEqual("Java", table[1][2]);
    }

    [TestMethod]
    public void CrossApply_PropertyAndMethod_GroupBy_WithFiltering()
    {
        const string query = @"
    select 
        a.Department,
        Count(a.Department)
    from #schema.first() a 
    cross apply a.Employees b 
    cross apply a.Distinct(b.Skills) c
    where a.Budget > 400000
    group by a.Department";

        var firstSource = new CrossApplyClass5[]
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

    private class CrossApplyClass6
    {
        public string Name { get; set; }
        
        public string Surname { get; set; }
        
        public int Id { get; set; }
    }
    
    private class CrossApplyClass7
    {   
        public int Id { get; set; }
        
        [BindablePropertyAsTable]
        public string[] Skills { get; set; }
    }
    
    [TestMethod]
    public void CrossApply_InnerJoinAndUseProperty_ShouldPass()
    {
        const string query = @"
    select 
        a.Name,
        a.Surname,
        c.Value
    from #schema.first() a
    inner join #schema.second() b on a.Id = b.Id
    cross apply b.Skills c";
        
        var firstSource = new CrossApplyClass6[]
        {
            new() {Name = "John", Surname = "Doe", Id = 1},
            new() {Name = "Jane", Surname = "Smith", Id = 2},
            new() {Name = "Alice", Surname = "Johnson", Id = 3}
        };

        var secondSource = new CrossApplyClass7[]
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
        
        Assert.AreEqual(5, table.Count);
        
        Assert.AreEqual("John", table[0][0]);
        Assert.AreEqual("Doe", table[0][1]);
        Assert.AreEqual("C#", table[0][2]);
        
        Assert.AreEqual("John", table[1][0]);
        Assert.AreEqual("Doe", table[1][1]);
        Assert.AreEqual("JavaScript", table[1][2]);
        
        Assert.AreEqual("Jane", table[2][0]);
        Assert.AreEqual("Smith", table[2][1]);
        Assert.AreEqual("Java", table[2][2]);
            
        Assert.AreEqual("Alice", table[3][0]);
        Assert.AreEqual("Johnson", table[3][1]);
        Assert.AreEqual("Communication", table[3][2]);
        
        Assert.AreEqual("Alice", table[4][0]);
        Assert.AreEqual("Johnson", table[4][1]);
        Assert.AreEqual("Negotiation", table[4][2]);
    }
    
    [TestMethod]
    public void CrossApply_LeftJoinAndUseProperty_ShouldPass()
    {
        const string query = @"
    select 
        a.Name,
        a.Surname,
        c.Value
    from #schema.first() a
    left outer join #schema.second() b on a.Id = b.Id
    cross apply b.Skills c";
        
        var firstSource = new CrossApplyClass6[]
        {
            new() {Name = "John", Surname = "Doe", Id = 1},
            new() {Name = "Jane", Surname = "Smith", Id = 2},
            new() {Name = "Alice", Surname = "Johnson", Id = 3}
        };

        var secondSource = new CrossApplyClass7[]
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
        
        Assert.AreEqual(5, table.Count);
        
        Assert.AreEqual("John", table[0][0]);
        Assert.AreEqual("Doe", table[0][1]);
        Assert.AreEqual("C#", table[0][2]);
        
        Assert.AreEqual("John", table[1][0]);
        Assert.AreEqual("Doe", table[1][1]);
        Assert.AreEqual("JavaScript", table[1][2]);
        
        Assert.AreEqual("Jane", table[2][0]);
        Assert.AreEqual("Smith", table[2][1]);
        Assert.AreEqual("Java", table[2][2]);
            
        Assert.AreEqual("Alice", table[3][0]);
        Assert.AreEqual("Johnson", table[3][1]);
        Assert.AreEqual("Communication", table[3][2]);
        
        Assert.AreEqual("Alice", table[4][0]);
        Assert.AreEqual("Johnson", table[4][1]);
        Assert.AreEqual("Negotiation", table[4][2]);
    }
}