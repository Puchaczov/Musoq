using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Evaluator.Tests.Schema.Generic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class CrossApplyTests : GenericEntityTestBase
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
        public string Country { get; set; }
        
        public string Address { get; set; }
    }
    
    [TestMethod]
    public void WhenSchemaMethodCrossAppliedWithAnotherSchema_UsesValuesOfSchemaMethodWithinTableValue_ShouldPass()
    {
        const string query = "select a.City, a.Country, a.Population, b.Country, b.Money, b.Month from #schema.first() a cross apply #schema.second(a.Country) b";
        
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
    public void WhenSchemaMethodCrossAppliedWithAnotherSchema_UsesValuesOfSchemaMethodWithinTableValue_UseOnlyValuesOfCrossApplySchemaMethod_ShouldPass()
    {
        const string query = "select b.Country, b.Money, b.Month from #schema.first() a cross apply #schema.second(a.Country) b";
        
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
        
        Assert.AreEqual(3, table.Columns.Count());
        Assert.AreEqual("b.Country", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);
        Assert.AreEqual("b.Money", table.Columns.ElementAt(1).ColumnName);
        Assert.AreEqual(typeof(decimal), table.Columns.ElementAt(1).ColumnType);
        Assert.AreEqual("b.Month", table.Columns.ElementAt(2).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(2).ColumnType);
        
        Assert.AreEqual(5, table.Count);
        
        Assert.AreEqual("Country1", table[0].Values[0]);
        Assert.AreEqual(1000m, table[0].Values[1]);
        Assert.AreEqual("January", table[0].Values[2]);
        
        Assert.AreEqual("Country1", table[1].Values[0]);
        Assert.AreEqual(2000m, table[1].Values[1]);
        Assert.AreEqual("February", table[1].Values[2]);
        
        Assert.AreEqual("Country1", table[2].Values[0]);
        Assert.AreEqual(1000m, table[2].Values[1]);
        Assert.AreEqual("January", table[2].Values[2]);
        
        Assert.AreEqual("Country1", table[3].Values[0]);
        Assert.AreEqual(2000m, table[3].Values[1]);
        Assert.AreEqual("February", table[3].Values[2]);
        
        Assert.AreEqual("Country2", table[4].Values[0]);
        Assert.AreEqual(3000m, table[4].Values[1]);
        Assert.AreEqual("March", table[4].Values[2]);
    }
    
    [TestMethod]
    public void WhenSchemaMethodCrossAppliedWithAnotherSchema_UsesValuesOfSchemaMethodWithinTableValue_FilterWithAValue_ShouldPass()
    {
        const string query = "select b.Country, b.Money, b.Month from #schema.first() a cross apply #schema.second(a.Country) b where a.Country = 'Country2'";
        
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
        
        Assert.AreEqual(3, table.Columns.Count());
        Assert.AreEqual("b.Country", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);
        Assert.AreEqual("b.Money", table.Columns.ElementAt(1).ColumnName);
        Assert.AreEqual(typeof(decimal), table.Columns.ElementAt(1).ColumnType);
        Assert.AreEqual("b.Month", table.Columns.ElementAt(2).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(2).ColumnType);
        
        Assert.AreEqual(1, table.Count);
        
        Assert.AreEqual("Country2", table[0].Values[0]);
        Assert.AreEqual(3000m, table[0].Values[1]);
        Assert.AreEqual("March", table[0].Values[2]);
    }
    
    [TestMethod]
    public void WhenSchemaMethodCrossAppliedWithAnotherSameSchemas_UsesValuesOfSchemaMethodWithinTableValue_ShouldPass()
    {
        const string query = "select b.Country, b.Money, b.Month from #schema.first() a cross apply #schema.second(a.Country) b cross apply #schema.third(b.Country) c";
        
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
        
        var thirdSource = new List<CrossApplyClass3>
        {
            new() {Country = "Country1", Address = "Address1"},
            new() {Country = "Country1", Address = "Address2"},
            new() {Country = "Country2", Address = "Address3"}
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(
            query, 
            firstSource, 
            secondSource,
            thirdSource,
            null, 
            null,
            null,
            null,
            (parameters, source) => new ObjectRowsSource(source.Rows.Where(f => (string) f["Country"] == (string) parameters[0]).ToArray()),
            (parameters, source) => new ObjectRowsSource(source.Rows.Where(f => (string) f["Country"] == (string) parameters[0]).ToArray()));
        
        var table = vm.Run();
        
        Assert.AreEqual(3, table.Columns.Count());
        Assert.AreEqual("b.Country", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);
        Assert.AreEqual("b.Money", table.Columns.ElementAt(1).ColumnName);
        Assert.AreEqual(typeof(decimal), table.Columns.ElementAt(1).ColumnType);
        Assert.AreEqual("b.Month", table.Columns.ElementAt(2).ColumnName);
        
        Assert.AreEqual(9, table.Count);
        
        Assert.AreEqual("Country1", table[0].Values[0]);
        Assert.AreEqual(1000m, table[0].Values[1]);
        Assert.AreEqual("January", table[0].Values[2]);
        
        Assert.AreEqual("Country1", table[1].Values[0]);
        Assert.AreEqual(1000m, table[1].Values[1]);
        Assert.AreEqual("January", table[1].Values[2]);
        
        Assert.AreEqual("Country1", table[2].Values[0]);
        Assert.AreEqual(2000m, table[2].Values[1]);
        Assert.AreEqual("February", table[2].Values[2]);
        
        Assert.AreEqual("Country1", table[3].Values[0]);
        Assert.AreEqual(2000m, table[3].Values[1]);
        Assert.AreEqual("February", table[3].Values[2]);
        
        Assert.AreEqual("Country1", table[4].Values[0]);
        Assert.AreEqual(1000m, table[4].Values[1]);
        Assert.AreEqual("January", table[4].Values[2]);
        
        Assert.AreEqual("Country1", table[5].Values[0]);
        Assert.AreEqual(1000m, table[5].Values[1]);
        Assert.AreEqual("January", table[5].Values[2]);
        
        Assert.AreEqual("Country1", table[6].Values[0]);
        Assert.AreEqual(2000m, table[6].Values[1]);
        Assert.AreEqual("February", table[6].Values[2]);
        
        Assert.AreEqual("Country1", table[7].Values[0]);
        Assert.AreEqual(2000m, table[7].Values[1]);
        Assert.AreEqual("February", table[7].Values[2]);
        
        Assert.AreEqual("Country2", table[8].Values[0]);
        Assert.AreEqual(3000m, table[8].Values[1]);
        Assert.AreEqual("March", table[8].Values[2]);
    }
    
    [TestMethod]
    public void WhenSchemaMethodCrossAppliedWithAnotherSameSchemas_GroupedByCountry_ShouldPass()
    {
        const string query = "select b.Country from #schema.first() a cross apply #schema.second(a.Country) b cross apply #schema.third(b.Country) c group by b.Country";
        
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
        
        var thirdSource = new List<CrossApplyClass3>
        {
            new() {Country = "Country1", Address = "Address1"},
            new() {Country = "Country1", Address = "Address2"},
            new() {Country = "Country2", Address = "Address3"}
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(
            query, 
            firstSource, 
            secondSource,
            thirdSource,
            null, 
            null,
            null,
            null,
            (parameters, source) => new ObjectRowsSource(source.Rows.Where(f => (string) f["Country"] == (string) parameters[0]).ToArray()),
            (parameters, source) => new ObjectRowsSource(source.Rows.Where(f => (string) f["Country"] == (string) parameters[0]).ToArray()));
        
        var table = vm.Run();
        
        Assert.AreEqual(1, table.Columns.Count());
        Assert.AreEqual("b.Country", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);
        
        Assert.AreEqual(2, table.Count);
        
        Assert.AreEqual("Country1", table[0].Values[0]);
        Assert.AreEqual("Country2", table[1].Values[0]);
    }
    
    [TestMethod]
    public void WhenSchemaMethodCrossAppliedWithAnotherSameSchemas_WithFilterAndGroupBy_ShouldPass()
    {
        const string query = "select b.Country from #schema.first() a cross apply #schema.second(a.Country) b cross apply #schema.third(b.Country) c where b.Country = 'Country1' group by b.Country";
        
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
        
        var thirdSource = new List<CrossApplyClass3>
        {
            new() {Country = "Country1", Address = "Address1"},
            new() {Country = "Country1", Address = "Address2"},
            new() {Country = "Country2", Address = "Address3"}
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(
            query, 
            firstSource, 
            secondSource,
            thirdSource,
            null, 
            null,
            null,
            null,
            (parameters, source) => new ObjectRowsSource(source.Rows.Where(f => (string) f["Country"] == (string) parameters[0]).ToArray()),
            (parameters, source) => new ObjectRowsSource(source.Rows.Where(f => (string) f["Country"] == (string) parameters[0]).ToArray()));
        
        var table = vm.Run();
        
        Assert.AreEqual(1, table.Columns.Count());
        Assert.AreEqual("b.Country", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);
        
        Assert.AreEqual(1, table.Count);
        
        Assert.AreEqual("Country1", table[0].Values[0]);
    }
    
    [TestMethod]
    public void WhenSchemaMethodCrossAppliedWithAnotherSchema_UsesValuesOfSchemaMethodWithinTableValue_UsedWithinCte_ShouldPass()
    {
        const string query =
            """
            with rows as (
                select b.Country as Country, b.Money as Money, b.Month as Month from #schema.first() a cross apply #schema.second(a.Country) b
            )
            select Country, Money, Month from rows as p
            """;
        
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
        
        Assert.AreEqual(3, table.Columns.Count());
        Assert.AreEqual("Country", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);
        Assert.AreEqual("Money", table.Columns.ElementAt(1).ColumnName);
        Assert.AreEqual(typeof(decimal), table.Columns.ElementAt(1).ColumnType);
        Assert.AreEqual("Month", table.Columns.ElementAt(2).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(2).ColumnType);
        
        Assert.AreEqual(5, table.Count);
        
        Assert.AreEqual("Country1", table[0].Values[0]);
        Assert.AreEqual(1000m, table[0].Values[1]);
        Assert.AreEqual("January", table[0].Values[2]);
        
        Assert.AreEqual("Country1", table[1].Values[0]);
        Assert.AreEqual(2000m, table[1].Values[1]);
        Assert.AreEqual("February", table[1].Values[2]);
        
        Assert.AreEqual("Country1", table[2].Values[0]);
        Assert.AreEqual(1000m, table[2].Values[1]);
        Assert.AreEqual("January", table[2].Values[2]);
        
        Assert.AreEqual("Country1", table[3].Values[0]);
        Assert.AreEqual(2000m, table[3].Values[1]);
        Assert.AreEqual("February", table[3].Values[2]);
        
        Assert.AreEqual("Country2", table[4].Values[0]);
        Assert.AreEqual(3000m, table[4].Values[1]);
        Assert.AreEqual("March", table[4].Values[2]);
    }
}