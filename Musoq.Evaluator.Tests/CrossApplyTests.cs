using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
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
        Assert.IsTrue(table.Count == 5, "Table should contain 5 rows");

        Assert.IsTrue(table.Any(row => 
                (string)row.Values[0] == "City1" && 
                (string)row.Values[1] == "Country1" && 
                (int)row.Values[2] == 100 && 
                (string)row.Values[3] == "Country1" && 
                (decimal)row.Values[4] == 1000m), 
            "Row City1/Country1/100/Country1/1000 not found");

        Assert.IsTrue(table.Any(row => 
                (string)row.Values[0] == "City1" && 
                (string)row.Values[1] == "Country1" && 
                (int)row.Values[2] == 100 && 
                (string)row.Values[3] == "Country1" && 
                (decimal)row.Values[4] == 2000m),
            "Row City1/Country1/100/Country1/2000 not found");

        Assert.IsTrue(table.Any(row => 
                (string)row.Values[0] == "City2" && 
                (string)row.Values[1] == "Country1" && 
                (int)row.Values[2] == 200 && 
                (string)row.Values[3] == "Country1" && 
                (decimal)row.Values[4] == 1000m),
            "Row City2/Country1/200/Country1/1000 not found");

        Assert.IsTrue(table.Any(row => 
                (string)row.Values[0] == "City2" && 
                (string)row.Values[1] == "Country1" && 
                (int)row.Values[2] == 200 && 
                (string)row.Values[3] == "Country1" && 
                (decimal)row.Values[4] == 2000m),
            "Row City2/Country1/200/Country1/2000 not found");

        Assert.IsTrue(table.Any(row => 
                (string)row.Values[0] == "City3" && 
                (string)row.Values[1] == "Country2" && 
                (int)row.Values[2] == 300 && 
                (string)row.Values[3] == "Country2" && 
                (decimal)row.Values[4] == 3000m),
            "Row City3/Country2/300/Country2/3000 not found");
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
        
        Assert.IsTrue(table.Count == 5, "Table should have 5 entries");

        Assert.IsTrue(table.Any(entry => 
            (string)entry.Values[0] == "Country1" && 
            (decimal)entry.Values[1] == 1000m && 
            (string)entry.Values[2] == "January"
        ), "First entry should be Country1, 1000, January");

        Assert.IsTrue(table.Any(entry => 
            (string)entry.Values[0] == "Country1" && 
            (decimal)entry.Values[1] == 2000m && 
            (string)entry.Values[2] == "February"
        ), "Second entry should be Country1, 2000, February");

        Assert.IsTrue(table.Any(entry => 
            (string)entry.Values[0] == "Country1" && 
            (decimal)entry.Values[1] == 1000m && 
            (string)entry.Values[2] == "January"
        ), "Third entry should be Country1, 1000, January");

        Assert.IsTrue(table.Any(entry => 
            (string)entry.Values[0] == "Country1" && 
            (decimal)entry.Values[1] == 2000m && 
            (string)entry.Values[2] == "February"
        ), "Fourth entry should be Country1, 2000, February");

        Assert.IsTrue(table.Any(entry => 
            (string)entry.Values[0] == "Country2" && 
            (decimal)entry.Values[1] == 3000m && 
            (string)entry.Values[2] == "March"
        ), "Fifth entry should be Country2, 3000, March");
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
        
        Assert.IsTrue(table.Count == 9, "Table should contain 9 rows");

        Assert.IsTrue(table.Count(row => 
                (string)row.Values[0] == "Country1" && 
                (decimal)row.Values[1] == 1000m && 
                (string)row.Values[2] == "January") == 4,
            "Should have exactly 4 rows of Country1/1000/January");

        Assert.IsTrue(table.Count(row => 
                (string)row.Values[0] == "Country1" && 
                (decimal)row.Values[1] == 2000m && 
                (string)row.Values[2] == "February") == 4,
            "Should have exactly 4 rows of Country1/2000/February");

        Assert.IsTrue(table.Count(row => 
                (string)row.Values[0] == "Country2" && 
                (decimal)row.Values[1] == 3000m && 
                (string)row.Values[2] == "March") == 1,
            "Should have exactly 1 row of Country2/3000/March");
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
        
        Assert.IsTrue(table.Count == 2, "Table should contain 2 rows");

        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "Country1"), "Missing Country1 row");
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "Country2"), "Missing Country2 row");
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
        
        Assert.IsTrue(table.Count == 5, "Table should contain 5 rows");

        Assert.IsTrue(table.Any(row => 
                (string)row.Values[0] == "Country1" && 
                (decimal)row.Values[1] == 1000m && 
                (string)row.Values[2] == "January"), 
            "Missing Country1/1000/January row");

        Assert.IsTrue(table.Any(row => 
                (string)row.Values[0] == "Country1" && 
                (decimal)row.Values[1] == 2000m && 
                (string)row.Values[2] == "February"),
            "Missing Country1/2000/February row");

        Assert.IsTrue(table.Count(row => 
                (string)row.Values[0] == "Country1" && 
                (decimal)row.Values[1] == 1000m && 
                (string)row.Values[2] == "January") == 2,
            "Should have exactly 2 rows of Country1/1000/January");

        Assert.IsTrue(table.Count(row => 
                (string)row.Values[0] == "Country1" && 
                (decimal)row.Values[1] == 2000m && 
                (string)row.Values[2] == "February") == 2,
            "Should have exactly 2 rows of Country1/2000/February");

        Assert.IsTrue(table.Any(row => 
                (string)row.Values[0] == "Country2" && 
                (decimal)row.Values[1] == 3000m && 
                (string)row.Values[2] == "March"),
            "Missing Country2/3000/March row");
    }
}
