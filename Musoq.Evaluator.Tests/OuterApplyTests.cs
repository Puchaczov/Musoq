using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Generic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class OuterApplyTests : GenericEntityTestBase
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
        public string Country { get; set; }
        
        public string Address { get; set; }
    }

    [TestMethod]
    public void OuterApply_NoMatchesShouldReturnNull_ShouldPass()
    {
        const string query = "select a.City, a.Country, a.Population, b.Country, b.Money, b.Month from #schema.first() a outer apply #schema.second(a.Country) b";
        
        var firstSource = new List<OuterApplyClass1>
        {
            new() {City = "City1", Country = "Country1", Population = 100},
            new() {City = "City2", Country = "Country1", Population = 200},
            new() {City = "City3", Country = "Country2", Population = 300}
        }.ToArray();
        
        var secondSource = new List<OuterApplyClass2>
        {
            new() {Country = "Country1", Money = 1000, Month = "January"},
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
        
        Assert.AreEqual(3, table.Count);
        
        Assert.AreEqual("City1", table[0][0]);
        Assert.AreEqual("Country1", table[0][1]);
        Assert.AreEqual(100, table[0][2]);
        Assert.AreEqual("Country1", table[0][3]);
        Assert.AreEqual(1000m, table[0][4]);
        Assert.AreEqual("January", table[0][5]);
        
        Assert.AreEqual("City2", table[1][0]);
        Assert.AreEqual("Country1", table[1][1]);
        Assert.AreEqual(200, table[1][2]);
        Assert.AreEqual("Country1", table[1][3]);
        Assert.AreEqual(1000m, table[1][4]);
        Assert.AreEqual("January", table[1][5]);
        
        Assert.AreEqual("City3", table[2][0]);
        Assert.AreEqual("Country2", table[2][1]);
        Assert.AreEqual(300, table[2][2]);
        Assert.IsNull(table[2][3]);
        Assert.IsNull(table[2][4]);
        Assert.IsNull(table[2][5]);
    }
    
    [TestMethod]
    public void OuterApply_MultipleMatches_ShouldPass()
    {
        const string query = "select a.City, a.Country, b.Money, b.Month from #schema.first() a outer apply #schema.second(a.Country) b";
        
        var firstSource = new List<OuterApplyClass1>
        {
            new() {City = "City1", Country = "Country1", Population = 100},
            new() {City = "City2", Country = "Country1", Population = 200},
        }.ToArray();
        
        var secondSource = new List<OuterApplyClass2>
        {
            new() {Country = "Country1", Money = 1000, Month = "January"},
            new() {Country = "Country1", Money = 2000, Month = "February"},
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
        
        Assert.AreEqual(4, table.Columns.Count());
        Assert.AreEqual("a.City", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual("a.Country", table.Columns.ElementAt(1).ColumnName);
        Assert.AreEqual("b.Money", table.Columns.ElementAt(2).ColumnName);
        Assert.AreEqual("b.Month", table.Columns.ElementAt(3).ColumnName);
        
        Assert.AreEqual(4, table.Count);
        
        Assert.AreEqual("City1", table[0][0]);
        Assert.AreEqual("Country1", table[0][1]);
        Assert.AreEqual(1000m, table[0][2]);
        Assert.AreEqual("January", table[0][3]);
        
        Assert.AreEqual("City1", table[1][0]);
        Assert.AreEqual("Country1", table[1][1]);
        Assert.AreEqual(2000m, table[1][2]);
        Assert.AreEqual("February", table[1][3]);
        
        Assert.AreEqual("City2", table[2][0]);
        Assert.AreEqual("Country1", table[2][1]);
        Assert.AreEqual(1000m, table[2][2]);
        Assert.AreEqual("January", table[2][3]);
        
        Assert.AreEqual("City2", table[3][0]);
        Assert.AreEqual("Country1", table[3][1]);
        Assert.AreEqual(2000m, table[3][2]);
        Assert.AreEqual("February", table[3][3]);
    }
    
    [TestMethod]
    public void OuterApply_NoMatches_ShouldPass()
    {
        const string query = "select a.City, a.Country, b.Money, b.Month from #schema.first() a outer apply #schema.second(a.Country) b";
        
        var firstSource = new List<OuterApplyClass1>
        {
            new() {City = "City1", Country = "Country1", Population = 100},
        }.ToArray();
        
        var secondSource = new List<OuterApplyClass2>
        {
            new() {Country = "Country2", Money = 1000, Month = "January"},
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
        
        Assert.AreEqual(4, table.Columns.Count());
        Assert.AreEqual("a.City", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual("a.Country", table.Columns.ElementAt(1).ColumnName);
        Assert.AreEqual("b.Money", table.Columns.ElementAt(2).ColumnName);
        Assert.AreEqual("b.Month", table.Columns.ElementAt(3).ColumnName);
        
        Assert.AreEqual(1, table.Count);
        
        Assert.AreEqual("City1", table[0][0]);
        Assert.AreEqual("Country1", table[0][1]);
        Assert.IsNull(table[0][2]);
        Assert.IsNull(table[0][3]);
    }
    
    [TestMethod]
    public void OuterApply_TripleApply_ShouldPass()
    {
        const string query = @"
            select a.City, a.Country, b.Money, b.Month, c.Address 
            from #schema.first() a 
            outer apply #schema.second(a.Country) b
            outer apply #schema.third(a.Country) c";
        
        var firstSource = new List<OuterApplyClass1>
        {
            new() {City = "City1", Country = "Country1", Population = 100},
            new() {City = "City2", Country = "Country2", Population = 200},
        }.ToArray();
        
        var secondSource = new List<OuterApplyClass2>
        {
            new() {Country = "Country1", Money = 1000, Month = "January"},
            new() {Country = "Country2", Money = 2000, Month = "February"},
        }.ToArray();

        var thirdSource = new List<OuterApplyClass3>
        {
            new() {Country = "Country1", Address = "Address1"},
            new() {Country = "Country3", Address = "Address3"},
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
        
        Assert.AreEqual(5, table.Columns.Count());
        Assert.AreEqual("a.City", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual("a.Country", table.Columns.ElementAt(1).ColumnName);
        Assert.AreEqual("b.Money", table.Columns.ElementAt(2).ColumnName);
        Assert.AreEqual("b.Month", table.Columns.ElementAt(3).ColumnName);
        Assert.AreEqual("c.Address", table.Columns.ElementAt(4).ColumnName);
        
        Assert.AreEqual(2, table.Count);
        
        Assert.AreEqual("City1", table[0][0]);
        Assert.AreEqual("Country1", table[0][1]);
        Assert.AreEqual(1000m, table[0][2]);
        Assert.AreEqual("January", table[0][3]);
        Assert.AreEqual("Address1", table[0][4]);
        
        Assert.AreEqual("City2", table[1][0]);
        Assert.AreEqual("Country2", table[1][1]);
        Assert.AreEqual(2000m, table[1][2]);
        Assert.AreEqual("February", table[1][3]);
        Assert.IsNull(table[1][4]);
    }
    
    [TestMethod]
    public void OuterApply_WithAggregation_ShouldPass()
    {
        const string query = @"
            select a.Country, Sum(b.Money) as TotalMoney, Count(b.Money) as TransactionCount
            from #schema.first() a 
            outer apply #schema.second(a.Country) b
            group by a.Country";
        
        var firstSource = new List<OuterApplyClass1>
        {
            new() {City = "City1", Country = "Country1", Population = 100},
            new() {City = "City2", Country = "Country1", Population = 200},
            new() {City = "City3", Country = "Country2", Population = 300},
            new() {City = "City4", Country = "Country3", Population = 400},
        }.ToArray();
        
        var secondSource = new List<OuterApplyClass2>
        {
            new() {Country = "Country1", Money = 1000, Month = "January"},
            new() {Country = "Country1", Money = 2000, Month = "February"},
            new() {Country = "Country2", Money = 3000, Month = "March"},
        }.ToArray();
        
        var vm = CreateAndRunVirtualMachine(
            query, 
            firstSource, 
            secondSource, 
            null, 
            null,
            null,
            (parameters, source) => new ObjectRowsSource(source.Rows.Where(f => (string)f["Country"] == (string)parameters[0]).ToArray()));
        
        var table = vm.Run();
        
        Assert.AreEqual(3, table.Columns.Count());
        Assert.AreEqual("a.Country", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual("TotalMoney", table.Columns.ElementAt(1).ColumnName);
        Assert.AreEqual("TransactionCount", table.Columns.ElementAt(2).ColumnName);
        
        Assert.AreEqual(3, table.Count);
        
        // Country1 (two cities, two transactions)
        Assert.AreEqual("Country1", table[0][0]);
        Assert.AreEqual(6000m, table[0][1]);  // (1000 + 2000) * 2 cities
        Assert.AreEqual(4, table[0][2]);  // 2 transactions * 2 cities
        
        // Country2 (one city, one transaction)
        Assert.AreEqual("Country2", table[1][0]);
        Assert.AreEqual(3000m, table[1][1]);
        Assert.AreEqual(1, table[1][2]);
        
        // Country3 (one city, no transactions)
        Assert.AreEqual("Country3", table[2][0]);
        Assert.AreEqual(0m, table[2][1]);  // No money, so sum is 0
        Assert.AreEqual(0, table[2][2]);  // No transactions, so count is 0
    }    
    
    [TestMethod]
    public void OuterApply_WithWhereClause_ShouldPass()
    {
        const string query = @"
            select a.City, a.Country, b.Money, b.Month 
            from #schema.first() a 
            outer apply #schema.second(a.Country) b
            where b.Money > 1500 or b.Money is null";
        
        var firstSource = new List<OuterApplyClass1>
        {
            new() {City = "City1", Country = "Country1", Population = 100},
            new() {City = "City2", Country = "Country2", Population = 200},
            new() {City = "City3", Country = "Country3", Population = 300},
        }.ToArray();
        
        var secondSource = new List<OuterApplyClass2>
        {
            new() {Country = "Country1", Money = 1000, Month = "January"},
            new() {Country = "Country1", Money = 2000, Month = "February"},
            new() {Country = "Country2", Money = 3000, Month = "March"},
        }.ToArray();
        
        var vm = CreateAndRunVirtualMachine(
            query, 
            firstSource, 
            secondSource, 
            null, 
            null,
            null,
            (parameters, source) => new ObjectRowsSource(source.Rows.Where(f => (string)f["Country"] == (string)parameters[0]).ToArray()));
        
        var table = vm.Run();
        
        Assert.AreEqual(4, table.Columns.Count());
        Assert.AreEqual("a.City", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual("a.Country", table.Columns.ElementAt(1).ColumnName);
        Assert.AreEqual("b.Money", table.Columns.ElementAt(2).ColumnName);
        Assert.AreEqual("b.Month", table.Columns.ElementAt(3).ColumnName);
        
        Assert.AreEqual(3, table.Count);
        
        // City1, Country1 (matches with Money > 1500)
        Assert.AreEqual("City1", table[0][0]);
        Assert.AreEqual("Country1", table[0][1]);
        Assert.AreEqual(2000m, table[0][2]);
        Assert.AreEqual("February", table[0][3]);
        
        // City2, Country2 (matches with Money > 1500)
        Assert.AreEqual("City2", table[1][0]);
        Assert.AreEqual("Country2", table[1][1]);
        Assert.AreEqual(3000m, table[1][2]);
        Assert.AreEqual("March", table[1][3]);
        
        // City3, Country3 (matches because Money is null)
        Assert.AreEqual("City3", table[2][0]);
        Assert.AreEqual("Country3", table[2][1]);
        Assert.IsNull(table[2][2]);
        Assert.IsNull(table[2][3]);
    }
}