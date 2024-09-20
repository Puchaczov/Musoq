using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Generic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class OuterApplyTests : GenericEntityTestBase
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
    public void X()
    {
        const string query = "select a.City, a.Country, a.Population, b.Country, b.Money, b.Month from #schema.first() a outer apply #schema.second(a.Country) b";
        
        var firstSource = new List<CrossApplyClass1>
        {
            new() {City = "City1", Country = "Country1", Population = 100},
            new() {City = "City2", Country = "Country1", Population = 200},
            new() {City = "City3", Country = "Country2", Population = 300}
        }.ToArray();
        
        var secondSource = new List<CrossApplyClass2>
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
}