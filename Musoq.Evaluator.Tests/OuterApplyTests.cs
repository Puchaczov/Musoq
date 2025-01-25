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
        
        Assert.IsTrue(table.Count == 3, "Table should have 3 entries");

        Assert.IsTrue(table.Any(entry => 
            (string)entry[0] == "City1" && 
            (string)entry[1] == "Country1" && 
            (int)entry[2] == 100 && 
            (string)entry[3] == "Country1" && 
            (decimal)entry[4] == 1000m && 
            (string)entry[5] == "January"
        ), "First entry should match City1 details");

        Assert.IsTrue(table.Any(entry => 
            (string)entry[0] == "City2" && 
            (string)entry[1] == "Country1" && 
            (int)entry[2] == 200 && 
            (string)entry[3] == "Country1" && 
            (decimal)entry[4] == 1000m && 
            (string)entry[5] == "January"
        ), "Second entry should match City2 details");

        Assert.IsTrue(table.Any(entry => 
            (string)entry[0] == "City3" && 
            (string)entry[1] == "Country2" && 
            (int)entry[2] == 300 && 
            entry[3] == null && 
            entry[4] == null && 
            entry[5] == null
        ), "Third entry should match City3 details");
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
        
        Assert.IsTrue(table.Count == 4, "Table should contain 4 rows");

        Assert.IsTrue(table.Count(row =>
                new[] { "City1", "City2" }.Contains((string)row[0]) &&
                (string)row[1] == "Country1" &&
                new[] { 1000m, 2000m }.Contains((decimal)row[2]) &&
                new[] { "January", "February" }.Contains((string)row[3])) == 4,
            "Expected 4 rows matching the pattern: (City1|City2), Country1, (1000|2000), (January|February)");

        Assert.IsTrue(table.Count(row =>
                (string)row[0] == "City1" &&
                new[] { (1000m, "January"), (2000m, "February") }.Contains(((decimal)row[2], (string)row[3]))) == 2,
            "Expected 2 rows for City1 with correct amount/month combinations");

        Assert.IsTrue(table.Count(row =>
                (string)row[0] == "City2" &&
                new[] { (1000m, "January"), (2000m, "February") }.Contains(((decimal)row[2], (string)row[3]))) == 2,
            "Expected 2 rows for City2 with correct amount/month combinations");
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
            select a.Country, b.Sum(b.Money) as TotalMoney, b.Count(b.Money) as TransactionCount
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
        
        Assert.IsTrue(table.Count == 3, "Table should have 3 entries");

        Assert.IsTrue(table.Any(entry => 
                (string)entry[0] == "Country1" && 
                (decimal)entry[1] == 6000m && 
                (int)entry[2] == 4), 
            "First entry should represent Country1 with total 6000m and 4 transactions");

        Assert.IsTrue(table.Any(entry => 
                (string)entry[0] == "Country2" && 
                (decimal)entry[1] == 3000m && 
                (int)entry[2] == 1), 
            "Second entry should represent Country2 with total 3000m and 1 transaction");

        Assert.IsTrue(table.Any(entry => 
                (string)entry[0] == "Country3" && 
                (decimal)entry[1] == 0m && 
                (int)entry[2] == 0), 
            "Third entry should represent Country3 with 0m total and 0 transactions");
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
        
        Assert.AreEqual(3, table.Count, "Result should contain exactly 3 rows");

        Assert.IsTrue(table.Any(row => 
            (string)row[0] == "City1" && 
            (string)row[1] == "Country1" && 
            (decimal)row[2] == 2000m && 
            (string)row[3] == "February"
        ), "Expected combination (City1, Country1, 2000, February) not found");

        Assert.IsTrue(table.Any(row => 
            (string)row[0] == "City2" && 
            (string)row[1] == "Country2" && 
            (decimal)row[2] == 3000m && 
            (string)row[3] == "March"
        ), "Expected combination (City2, Country2, 3000, March) not found");

        Assert.IsTrue(table.Any(row => 
            (string)row[0] == "City3" && 
            (string)row[1] == "Country3" && 
            row[2] == null && 
            row[3] == null
        ), "Expected combination (City3, Country3, null, null) not found");
    }
}