using System.Collections.Generic;
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
        
        Assert.IsTrue(table.Any(row => 
            (string)row[0] == "New York" && 
            (string)row[1] == "USA" && 
            (string)row[2] == "Broadway" && 
            (int)row[3] == 123
        ), "First row should match New York, USA, Broadway, 123");

        Assert.IsTrue(table.Any(row => 
            (string)row[0] == "New York" && 
            (string)row[1] == "USA" && 
            (string)row[2] == "Fifth Avenue" && 
            (int)row[3] == 456
        ), "Second row should match New York, USA, Fifth Avenue, 456");

        Assert.IsTrue(table.Any(row => 
            (string)row[0] == "Los Angeles" && 
            (string)row[1] == "USA" && 
            (string)row[2] == "Broadway" && 
            (int)row[3] == 123
        ), "Third row should match Los Angeles, USA, Broadway, 123");

        Assert.IsTrue(table.Any(row => 
            (string)row[0] == "Los Angeles" && 
            (string)row[1] == "USA" && 
            (string)row[2] == "Fifth Avenue" && 
            (int)row[3] == 456
        ), "Fourth row should match Los Angeles, USA, Fifth Avenue, 456");
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
        
        Assert.IsTrue(table.Count == 8, "Table should have 8 entries");

        // IT Department with John Doe
        Assert.IsTrue(table.Any(entry => 
           (string)entry[0] == "IT" && 
           (int)entry[1] == 500000 && 
           (string)entry[2] == "John Doe" && 
           (int)entry[3] == 50000 && 
           (string)entry[4] == "C#"), 
           "Should have IT entry for John Doe with C#");

        Assert.IsTrue(table.Any(entry => 
           (string)entry[0] == "IT" && 
           (int)entry[1] == 500000 && 
           (string)entry[2] == "John Doe" && 
           (int)entry[3] == 50000 && 
           (string)entry[4] == "JavaScript"), 
           "Should have IT entry for John Doe with JavaScript");

        Assert.IsTrue(table.Any(entry => 
           (string)entry[0] == "IT" && 
           (int)entry[1] == 500000 && 
           (string)entry[2] == "Jane Smith" && 
           (int)entry[3] == 60000 && 
           (string)entry[4] == "C#"), 
           "Should have IT entry for Jane Smith with C#");

        Assert.IsTrue(table.Any(entry => 
           (string)entry[0] == "IT" && 
           (int)entry[1] == 500000 && 
           (string)entry[2] == "Jane Smith" && 
           (int)entry[3] == 60000 && 
           (string)entry[4] == "JavaScript"), 
           "Should have IT entry for Jane Smith with JavaScript");

        // HR Department with John Doe
        Assert.IsTrue(table.Any(entry => 
           (string)entry[0] == "HR" && 
           (int)entry[1] == 300000 && 
           (string)entry[2] == "John Doe" && 
           (int)entry[3] == 50000 && 
           (string)entry[4] == "Communication"), 
           "Should have HR entry for John Doe with Communication");

        Assert.IsTrue(table.Any(entry => 
           (string)entry[0] == "HR" && 
           (int)entry[1] == 300000 && 
           (string)entry[2] == "John Doe" && 
           (int)entry[3] == 50000 && 
           (string)entry[4] == "Negotiation"), 
           "Should have HR entry for John Doe with Negotiation");

        Assert.IsTrue(table.Any(entry => 
           (string)entry[0] == "HR" && 
           (int)entry[1] == 300000 && 
           (string)entry[2] == "Jane Smith" && 
           (int)entry[3] == 60000 && 
           (string)entry[4] == "Communication"), 
           "Should have HR entry for Jane Smith with Communication");

        Assert.IsTrue(table.Any(entry => 
           (string)entry[0] == "HR" && 
           (int)entry[1] == 300000 && 
           (string)entry[2] == "Jane Smith" && 
           (int)entry[3] == 60000 && 
           (string)entry[4] == "Negotiation"), 
           "Should have HR entry for Jane Smith with Negotiation");        
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
        
        Assert.IsTrue(table.Count == 2, "Table should have 2 entries");

        Assert.IsTrue(table.Any(row => 
            (string)row[0] == "IT" && 
            (string)row[1] == "John Doe" && 
            (string)row[2] == "C#"
        ), "First row should match IT, John Doe, C#");

        Assert.IsTrue(table.Any(row => 
            (string)row[0] == "IT" && 
            (string)row[1] == "Jane Smith" && 
            (string)row[2] == "Java"
        ), "Second row should match IT, Jane Smith, Java");
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
        
        Assert.IsTrue(table.Count == 1, "Table should have 1 entry");

        Assert.IsTrue(table.Any(row => 
            (string)row[0] == "IT" && 
            (int)row[1] == 2
        ), "First row should match IT, 2");
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
        
        Assert.IsTrue(table.Count == 5, "Table should have 5 entries");

        Assert.IsTrue(table.Any(entry => 
                (string)entry[0] == "John" && 
                (string)entry[1] == "Doe" && 
                (string)entry[2] == "C#"), 
            "First entry for John Doe should match");

        Assert.IsTrue(table.Any(entry => 
                (string)entry[0] == "John" && 
                (string)entry[1] == "Doe" && 
                (string)entry[2] == "JavaScript"), 
            "Second entry for John Doe should match");

        Assert.IsTrue(table.Any(entry => 
                (string)entry[0] == "Jane" && 
                (string)entry[1] == "Smith" && 
                (string)entry[2] == "Java"), 
            "Entry for Jane Smith should match");

        Assert.IsTrue(table.Any(entry => 
                (string)entry[0] == "Alice" && 
                (string)entry[1] == "Johnson" && 
                (string)entry[2] == "Communication"), 
            "First entry for Alice Johnson should match");

        Assert.IsTrue(table.Any(entry => 
                (string)entry[0] == "Alice" && 
                (string)entry[1] == "Johnson" && 
                (string)entry[2] == "Negotiation"), 
            "Second entry for Alice Johnson should match");
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
        
        Assert.IsTrue(table.Count == 5, "Table should contain 5 rows");

        Assert.IsTrue(table.Count(row => 
                (string)row[0] == "John" && 
                (string)row[1] == "Doe" &&
                ((string)row[2] == "C#" || (string)row[2] == "JavaScript")) == 2,
            "Expected data for John Doe not found");

        Assert.IsTrue(table.Any(row => 
                (string)row[0] == "Jane" && 
                (string)row[1] == "Smith" &&
                (string)row[2] == "Java"),
            "Expected data for Jane Smith not found");

        Assert.IsTrue(table.Count(row => 
                (string)row[0] == "Alice" && 
                (string)row[1] == "Johnson" &&
                ((string)row[2] == "Communication" || (string)row[2] == "Negotiation")) == 2,
            "Expected data for Alice Johnson not found");
    }
    
    public class SpecialCaseEmptyType
    {
        public string Value => "Value";
    }

    public class SpecialCaseLibrary1 : GenericLibrary
    {
        [BindableMethod]
        public IEnumerable<string> GetOne()
        {
            return ["One"];
        }
        
        [BindableMethod]
        public IEnumerable<SpecialCaseComplexType2> GetSpecialCaseComplexType2(string whatever)
        {
            return
            [
                new SpecialCaseComplexType2()
            ];
        }
    }
    
    public class SpecialCaseComplexType1
    {
        public string Name => "John";
    }
    
    public class SpecialCaseComplexType2
    {
        [BindablePropertyAsTable]
        public IEnumerable<SpecialCaseComplexType1> Employees => [new()];
    }

    [TestMethod]
    public void CrossApply_WhenTwoMethodsReturnsEntities_AndThenUseColumnOfEntity_ShouldPass()
    {
        const string query = @"select a.Value, d.Name from #schema.first() a cross apply a.GetOne() b cross apply a.GetSpecialCaseComplexType2(b.Value) c cross apply c.Employees d";
        
        var vm = CreateAndRunVirtualMachine<SpecialCaseEmptyType, SpecialCaseLibrary1>(query, [new SpecialCaseEmptyType()]);
        
        var table = vm.Run();
        
        Assert.AreEqual(2, table.Columns.Count());
        
        Assert.AreEqual("a.Value", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);
        
        Assert.AreEqual("d.Name", table.Columns.ElementAt(1).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(1).ColumnType);
        
        Assert.AreEqual(1, table.Count);
        
        Assert.IsTrue(table.Any(row => 
            (string)row[0] == "Value" && 
            (string)row[1] == "John"
        ), "First row should match Value, John");
    }

    [TestMethod]
    public void WhenCteHasTheSameAliasWithinDifferentQueries_ShouldNotThrow()
    {
        const string query = "with p as (select 1 from #schema.first() a cross apply a.Split('a,b', ',') b), r as (select 1 from #schema.first() a cross apply a.Split('a,b', ',') b) select * from p";
        var firstSource = System.Array.Empty<CrossApplyClass5>();
        var vm = CreateAndRunVirtualMachine(
            query,
            firstSource);
        
        vm.Run();
    }
}
