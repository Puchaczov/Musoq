using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Generic;
using Musoq.Plugins.Attributes;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class OuterApplyCteTests : GenericEntityTestBase
{
    private class OuterApplyClass1
    {
        public string City { get; set; }
        
        public string Country { get; set; }
        
        public int Population { get; set; }
    }

    private class OuterApplyClass2
    {
        public string Country { get; set; }
        
        public decimal Money { get; set; }
        
        public string Month { get; set; }
    }
    
    private class OuterApplyClass3
    {
        public string Name { get; set; }
        
        [BindablePropertyAsTable]
        public string[] Skills { get; set; }
    }
    
    [TestMethod]
    public void WhenSchemaMethodOuterAppliedWithAnotherSchema_WithinCte_ShouldPass()
    {
        const string query = @"
with p as (
    select a.City, a.Country, a.Population, b.Country, b.Money, b.Month from @schema.first() a outer apply @schema.second(a.Country) b
)
select [a.City], [a.Country], [a.Population], [b.Country], [b.Money], [b.Month] from p";
        
        var firstSource = new List<OuterApplyClass1>
        {
            new() {City = "City1", Country = "Country1", Population = 100},
            new() {City = "City2", Country = "Country1", Population = 200},
            new() {City = "City3", Country = "Country2", Population = 300}
        }.ToArray();
        
        var secondSource = new List<OuterApplyClass2>
        {
            new() {Country = "Country1", Money = 1000, Month = "January"},
            new() {Country = "Country1", Money = 2000, Month = "February"},
            new() {Country = "Country2", Money = 3000, Month = "March"}
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(
            query, 
            firstSource, 
            secondSource, 
            null, 
            null,
            null,
            (parameters, source) => new ObjectRowsSource(source.Rows.Where(f => (string) f["Country"] == (string) parameters[0]).ToArray()));
        
        var table = vm.Run();
        
        Assert.AreEqual(6, table.Columns.Count());
        Assert.AreEqual("a.City", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);
        Assert.AreEqual("a.Country", table.Columns.ElementAt(1).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(1).ColumnType);
        Assert.AreEqual("a.Population", table.Columns.ElementAt(2).ColumnName);
        Assert.AreEqual(typeof(int), table.Columns.ElementAt(2).ColumnType);
        Assert.AreEqual("b.Country", table.Columns.ElementAt(3).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(3).ColumnType);
        Assert.AreEqual("b.Money", table.Columns.ElementAt(4).ColumnName);
        Assert.AreEqual(typeof(decimal?), table.Columns.ElementAt(4).ColumnType);
        Assert.AreEqual("b.Month", table.Columns.ElementAt(5).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(5).ColumnType);
        
        Assert.IsTrue(table.Count == 5, "Table should contain 5 rows");

        Assert.IsTrue(table.Count(row => 
                          (string)row.Values[0] == "City1" && 
                          (string)row.Values[1] == "Country1" &&
                          (int)row.Values[2] == 100 &&
                          (string)row.Values[3] == "Country1") == 2 &&
                      table.Any(row =>
                          (string)row.Values[0] == "City1" &&
                          (decimal)row.Values[4] == 1000m) &&
                      table.Any(row =>
                          (string)row.Values[0] == "City1" &&
                          (decimal)row.Values[4] == 2000m),
            "Expected two rows for City1 in Country1 with value 100 and amounts 1000 and 2000");

        Assert.IsTrue(table.Count(row => 
                          (string)row.Values[0] == "City2" && 
                          (string)row.Values[1] == "Country1" &&
                          (int)row.Values[2] == 200 &&
                          (string)row.Values[3] == "Country1") == 2 &&
                      table.Any(row =>
                          (string)row.Values[0] == "City2" &&
                          (decimal)row.Values[4] == 1000m) &&
                      table.Any(row =>
                          (string)row.Values[0] == "City2" &&
                          (decimal)row.Values[4] == 2000m),
            "Expected two rows for City2 in Country1 with value 200 and amounts 1000 and 2000");

        Assert.IsTrue(table.Any(row =>
                (string)row.Values[0] == "City3" && 
                (string)row.Values[1] == "Country2" &&
                (int)row.Values[2] == 300 &&
                (string)row.Values[3] == "Country2" &&
                (decimal)row.Values[4] == 3000m),
            "Expected one row for City3 in Country2 with value 300 and amount 3000");
    }
    
        [TestMethod]
    public void WhenSchemaMethodOuterAppliedWithAnotherSchema_UsesCte_ShouldPass()
    {
        const string query = @"
with p as (
    select 
        f.City as City, 
        f.Country as Country, 
        f.Population as Population 
    from @schema.first() f
)
select a.City, a.Country, a.Population, b.Country, b.Money, b.Month from p a outer apply @schema.second(a.Country) b";
        
        var firstSource = new List<OuterApplyClass1>
        {
            new() {City = "City1", Country = "Country1", Population = 100},
            new() {City = "City2", Country = "Country1", Population = 200},
            new() {City = "City3", Country = "Country2", Population = 300}
        }.ToArray();
        
        var secondSource = new List<OuterApplyClass2>
        {
            new() {Country = "Country1", Money = 1000, Month = "January"},
            new() {Country = "Country1", Money = 2000, Month = "February"},
            new() {Country = "Country2", Money = 3000, Month = "March"}
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(
            query, 
            firstSource, 
            secondSource, 
            null, 
            null,
            null,
            (parameters, source) => new ObjectRowsSource(source.Rows.Where(f => (string) f["Country"] == (string) parameters[0]).ToArray()));
        
        var table = vm.Run();
        
        Assert.AreEqual(6, table.Columns.Count());
        Assert.AreEqual("a.City", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);
        Assert.AreEqual("a.Country", table.Columns.ElementAt(1).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(1).ColumnType);
        Assert.AreEqual("a.Population", table.Columns.ElementAt(2).ColumnName);
        Assert.AreEqual(typeof(int), table.Columns.ElementAt(2).ColumnType);
        Assert.AreEqual("b.Country", table.Columns.ElementAt(3).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(3).ColumnType);
        Assert.AreEqual("b.Money", table.Columns.ElementAt(4).ColumnName);
        Assert.AreEqual(typeof(decimal?), table.Columns.ElementAt(4).ColumnType);
        Assert.AreEqual("b.Month", table.Columns.ElementAt(5).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(5).ColumnType);
        
        Assert.IsTrue(table.Count == 5, "Table should contain 5 rows");

        Assert.IsTrue(table.Count(row => 
                          (string)row.Values[0] == "City1" && 
                          (string)row.Values[1] == "Country1" &&
                          (int)row.Values[2] == 100 &&
                          (string)row.Values[3] == "Country1") == 2 &&
                      table.Any(row =>
                          (string)row.Values[0] == "City1" &&
                          (decimal)row.Values[4] == 1000m) &&
                      table.Any(row =>
                          (string)row.Values[0] == "City1" &&
                          (decimal)row.Values[4] == 2000m),
            "Expected two rows for City1 in Country1 with value 100 and amounts 1000 and 2000");

        Assert.IsTrue(table.Count(row => 
                          (string)row.Values[0] == "City2" && 
                          (string)row.Values[1] == "Country1" &&
                          (int)row.Values[2] == 200 &&
                          (string)row.Values[3] == "Country1") == 2 &&
                      table.Any(row =>
                          (string)row.Values[0] == "City2" &&
                          (decimal)row.Values[4] == 1000m) &&
                      table.Any(row =>
                          (string)row.Values[0] == "City2" &&
                          (decimal)row.Values[4] == 2000m),
            "Expected two rows for City2 in Country1 with value 200 and amounts 1000 and 2000");

        Assert.IsTrue(table.Any(row =>
                (string)row.Values[0] == "City3" && 
                (string)row.Values[1] == "Country2" &&
                (int)row.Values[2] == 300 &&
                (string)row.Values[3] == "Country2" &&
                (decimal)row.Values[4] == 3000m),
            "Expected one row for City3 in Country2 with value 300 and amount 3000");
    }
    
    [TestMethod]
    public void WhenSchemaMethodOuterAppliedSelfProperty_WithinCte_ShouldPass()
    {
        const string query = @"
with p as (
    select a.Name, b.Value from @schema.first() a outer apply a.Skills b
)
select [a.Name], [b.Value] from p";
        
        var firstSource = new List<OuterApplyClass3>
        {
            new() {Name = "Name1", Skills = ["Skill1", "Skill2", "Skill3"]},
            new() {Name = "Name2", Skills = ["Skill4", "Skill5", "Skill6"]},
            new() {Name = "Name3", Skills = ["Skill7", "Skill8", "Skill9"]}
        }.ToArray();
        
        var vm = CreateAndRunVirtualMachine(
            query, 
            firstSource);
        
        var table = vm.Run();
        
        Assert.AreEqual(2, table.Columns.Count());
        
        Assert.AreEqual("a.Name", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);
        
        Assert.AreEqual("b.Value", table.Columns.ElementAt(1).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(1).ColumnType);
        
// Verify total count first
        Assert.AreEqual(9, table.Count, "Result should contain exactly 9 name-skill pairs");

// Verify each name-skill combination exists
        Assert.IsTrue(table.Any(row => 
            (string)row.Values[0] == "Name1" && 
            (string)row.Values[1] == "Skill1"
        ), "Expected pair (Name1, Skill1) not found");

        Assert.IsTrue(table.Any(row => 
            (string)row.Values[0] == "Name1" && 
            (string)row.Values[1] == "Skill2"
        ), "Expected pair (Name1, Skill2) not found");

        Assert.IsTrue(table.Any(row => 
            (string)row.Values[0] == "Name1" && 
            (string)row.Values[1] == "Skill3"
        ), "Expected pair (Name1, Skill3) not found");

        Assert.IsTrue(table.Any(row => 
            (string)row.Values[0] == "Name2" && 
            (string)row.Values[1] == "Skill4"
        ), "Expected pair (Name2, Skill4) not found");

        Assert.IsTrue(table.Any(row => 
            (string)row.Values[0] == "Name2" && 
            (string)row.Values[1] == "Skill5"
        ), "Expected pair (Name2, Skill5) not found");

        Assert.IsTrue(table.Any(row => 
            (string)row.Values[0] == "Name2" && 
            (string)row.Values[1] == "Skill6"
        ), "Expected pair (Name2, Skill6) not found");

        Assert.IsTrue(table.Any(row => 
            (string)row.Values[0] == "Name3" && 
            (string)row.Values[1] == "Skill7"
        ), "Expected pair (Name3, Skill7) not found");

        Assert.IsTrue(table.Any(row => 
            (string)row.Values[0] == "Name3" && 
            (string)row.Values[1] == "Skill8"
        ), "Expected pair (Name3, Skill8) not found");

        Assert.IsTrue(table.Any(row => 
            (string)row.Values[0] == "Name3" && 
            (string)row.Values[1] == "Skill9"
        ), "Expected pair (Name3, Skill9) not found");
    }
    
    [TestMethod]
    public void WhenSchemaMethodOuterAppliedSelfProperty_UsesCte_ShouldPass()
    {
        const string query = @"
with first as (
    select a.Name as Name, a.Skills as Skills from @schema.first() a
)
select a.Name, b.Value from first a outer apply a.Skills b";
        
        var firstSource = new List<OuterApplyClass3>
        {
            new() {Name = "Name1", Skills = ["Skill1", "Skill2", "Skill3"]},
            new() {Name = "Name2", Skills = ["Skill4", "Skill5", "Skill6"]},
            new() {Name = "Name3", Skills = ["Skill7", "Skill8", "Skill9"]}
        }.ToArray();
        
        var vm = CreateAndRunVirtualMachine(
            query, 
            firstSource);
        
        var table = vm.Run();
        
        Assert.AreEqual(2, table.Columns.Count());
        
        Assert.AreEqual("a.Name", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);
        
        Assert.AreEqual("b.Value", table.Columns.ElementAt(1).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(1).ColumnType);
        
        Assert.IsTrue(table.Count == 9, "Table should contain 9 rows");

        Assert.IsTrue(table.Count(row => 
                (string)row.Values[0] == "Name1" && 
                new[] { "Skill1", "Skill2", "Skill3" }.Contains((string)row.Values[1])) == 3,
            "Expected 3 rows for Name1 with Skills 1-3");

        Assert.IsTrue(table.Count(row => 
                (string)row.Values[0] == "Name2" && 
                new[] { "Skill4", "Skill5", "Skill6" }.Contains((string)row.Values[1])) == 3,
            "Expected 3 rows for Name2 with Skills 4-6");

        Assert.IsTrue(table.Count(row => 
                (string)row.Values[0] == "Name3" && 
                new[] { "Skill7", "Skill8", "Skill9" }.Contains((string)row.Values[1])) == 3,
            "Expected 3 rows for Name3 with Skills 7-9");
    }
}