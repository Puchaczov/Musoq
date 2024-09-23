using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Generic;
using Musoq.Plugins.Attributes;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class CrossApplyCteTests : GenericEntityTestBase
{
    private class CrossApplyClass1
    {
        public string City { get; set; }
        
        public string Country { get; set; }
        
        public int Population { get; set; }
    }

    private class CrossApplyClass2
    {
        public string Country { get; set; }
        
        public decimal Money { get; set; }
        
        public string Month { get; set; }
    }
    
    private class CrossApplyClass3
    {
        public string Name { get; set; }
        
        [BindablePropertyAsTable]
        public string[] Skills { get; set; }
    }
    
    [TestMethod]
    public void WhenSchemaMethodCrossAppliedWithAnotherSchema_WithinCte_ShouldPass()
    {
        const string query = @"
with p as (
    select a.City, a.Country, a.Population, b.Country, b.Money, b.Month from #schema.first() a cross apply #schema.second(a.Country) b
)
select [a.City], [a.Country], [a.Population], [b.Country], [b.Money], [b.Month] from p";
        
        var firstSource = new List<CrossApplyClass1>
        {
            new() {City = "City1", Country = "Country1", Population = 100},
            new() {City = "City2", Country = "Country1", Population = 200},
            new() {City = "City3", Country = "Country2", Population = 300}
        }.ToArray();
        
        var secondSource = new List<CrossApplyClass2>
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
        Assert.AreEqual(typeof(decimal), table.Columns.ElementAt(4).ColumnType);
        Assert.AreEqual("b.Month", table.Columns.ElementAt(5).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(5).ColumnType);
        
        Assert.AreEqual(5, table.Count);
        
        Assert.AreEqual("City1", table[0].Values[0]);
        Assert.AreEqual("Country1", table[0].Values[1]);
        Assert.AreEqual(100, table[0].Values[2]);
        Assert.AreEqual("Country1", table[0].Values[3]);
        Assert.AreEqual(1000m, table[0].Values[4]);
        
        Assert.AreEqual("City1", table[1].Values[0]);
        Assert.AreEqual("Country1", table[1].Values[1]);
        Assert.AreEqual(100, table[1].Values[2]);
        Assert.AreEqual("Country1", table[1].Values[3]);
        Assert.AreEqual(2000m, table[1].Values[4]);
        
        Assert.AreEqual("City2", table[2].Values[0]);
        Assert.AreEqual("Country1", table[2].Values[1]);
        Assert.AreEqual(200, table[2].Values[2]);
        Assert.AreEqual("Country1", table[2].Values[3]);
        Assert.AreEqual(1000m, table[2].Values[4]);
        
        Assert.AreEqual("City2", table[3].Values[0]);
        Assert.AreEqual("Country1", table[3].Values[1]);
        Assert.AreEqual(200, table[3].Values[2]);
        Assert.AreEqual("Country1", table[3].Values[3]);
        Assert.AreEqual(2000m, table[3].Values[4]);
        
        Assert.AreEqual("City3", table[4].Values[0]);
        Assert.AreEqual("Country2", table[4].Values[1]);
        Assert.AreEqual(300, table[4].Values[2]);
        Assert.AreEqual("Country2", table[4].Values[3]);
        Assert.AreEqual(3000m, table[4].Values[4]);
    }
    
        [TestMethod]
    public void WhenSchemaMethodCrossAppliedWithAnotherSchema_UsesCte_ShouldPass()
    {
        const string query = @"
with p as (
    select 
        f.City as City, 
        f.Country as Country, 
        f.Population as Population 
    from #schema.first() f
)
select a.City, a.Country, a.Population, b.Country, b.Money, b.Month from p a cross apply #schema.second(a.Country) b";
        
        var firstSource = new List<CrossApplyClass1>
        {
            new() {City = "City1", Country = "Country1", Population = 100},
            new() {City = "City2", Country = "Country1", Population = 200},
            new() {City = "City3", Country = "Country2", Population = 300}
        }.ToArray();
        
        var secondSource = new List<CrossApplyClass2>
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
        Assert.AreEqual(typeof(decimal), table.Columns.ElementAt(4).ColumnType);
        Assert.AreEqual("b.Month", table.Columns.ElementAt(5).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(5).ColumnType);
        
        Assert.AreEqual(5, table.Count);
        
        Assert.AreEqual("City1", table[0].Values[0]);
        Assert.AreEqual("Country1", table[0].Values[1]);
        Assert.AreEqual(100, table[0].Values[2]);
        Assert.AreEqual("Country1", table[0].Values[3]);
        Assert.AreEqual(1000m, table[0].Values[4]);
        
        Assert.AreEqual("City1", table[1].Values[0]);
        Assert.AreEqual("Country1", table[1].Values[1]);
        Assert.AreEqual(100, table[1].Values[2]);
        Assert.AreEqual("Country1", table[1].Values[3]);
        Assert.AreEqual(2000m, table[1].Values[4]);
        
        Assert.AreEqual("City2", table[2].Values[0]);
        Assert.AreEqual("Country1", table[2].Values[1]);
        Assert.AreEqual(200, table[2].Values[2]);
        Assert.AreEqual("Country1", table[2].Values[3]);
        Assert.AreEqual(1000m, table[2].Values[4]);
        
        Assert.AreEqual("City2", table[3].Values[0]);
        Assert.AreEqual("Country1", table[3].Values[1]);
        Assert.AreEqual(200, table[3].Values[2]);
        Assert.AreEqual("Country1", table[3].Values[3]);
        Assert.AreEqual(2000m, table[3].Values[4]);
        
        Assert.AreEqual("City3", table[4].Values[0]);
        Assert.AreEqual("Country2", table[4].Values[1]);
        Assert.AreEqual(300, table[4].Values[2]);
        Assert.AreEqual("Country2", table[4].Values[3]);
        Assert.AreEqual(3000m, table[4].Values[4]);
    }
    
    [TestMethod]
    public void WhenSchemaMethodCrossAppliedSelfProperty_WithinCte_ShouldPass()
    {
        const string query = @"
with p as (
    select a.Name, b.Value from #schema.first() a cross apply a.Skills b
)
select [a.Name], [b.Value] from p";
        
        var firstSource = new List<CrossApplyClass3>
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
        
        Assert.AreEqual(9, table.Count);
        
        Assert.AreEqual("Name1", table[0].Values[0]);
        Assert.AreEqual("Skill1", table[0].Values[1]);
        
        Assert.AreEqual("Name1", table[1].Values[0]);
        Assert.AreEqual("Skill2", table[1].Values[1]);
        
        Assert.AreEqual("Name1", table[2].Values[0]);
        Assert.AreEqual("Skill3", table[2].Values[1]);
        
        Assert.AreEqual("Name2", table[3].Values[0]);
        Assert.AreEqual("Skill4", table[3].Values[1]);
        
        Assert.AreEqual("Name2", table[4].Values[0]);
        Assert.AreEqual("Skill5", table[4].Values[1]);
        
        Assert.AreEqual("Name2", table[5].Values[0]);
        Assert.AreEqual("Skill6", table[5].Values[1]);
        
        Assert.AreEqual("Name3", table[6].Values[0]);
        Assert.AreEqual("Skill7", table[6].Values[1]);
        
        Assert.AreEqual("Name3", table[7].Values[0]);
        Assert.AreEqual("Skill8", table[7].Values[1]);
        
        Assert.AreEqual("Name3", table[8].Values[0]);
        Assert.AreEqual("Skill9", table[8].Values[1]);
    }
    
    [TestMethod]
    public void WhenSchemaMethodCrossAppliedSelfProperty_UsesCte_ShouldPass()
    {
        const string query = @"
with first as (
    select a.Name as Name, a.Skills as Skills from #schema.first() a
)
select a.Name, b.Value from first a cross apply a.Skills b";
        
        var firstSource = new List<CrossApplyClass3>
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
        
        Assert.AreEqual(9, table.Count);
        
        Assert.AreEqual("Name1", table[0].Values[0]);
        Assert.AreEqual("Skill1", table[0].Values[1]);
        
        Assert.AreEqual("Name1", table[1].Values[0]);
        Assert.AreEqual("Skill2", table[1].Values[1]);
        
        Assert.AreEqual("Name1", table[2].Values[0]);
        Assert.AreEqual("Skill3", table[2].Values[1]);
        
        Assert.AreEqual("Name2", table[3].Values[0]);
        Assert.AreEqual("Skill4", table[3].Values[1]);
        
        Assert.AreEqual("Name2", table[4].Values[0]);
        Assert.AreEqual("Skill5", table[4].Values[1]);
        
        Assert.AreEqual("Name2", table[5].Values[0]);
        Assert.AreEqual("Skill6", table[5].Values[1]);
        
        Assert.AreEqual("Name3", table[6].Values[0]);
        Assert.AreEqual("Skill7", table[6].Values[1]);
        
        Assert.AreEqual("Name3", table[7].Values[0]);
        Assert.AreEqual("Skill8", table[7].Values[1]);
        
        Assert.AreEqual("Name3", table[8].Values[0]);
        Assert.AreEqual("Skill9", table[8].Values[1]);
    }
}