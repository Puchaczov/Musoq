using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Unknown;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class TimeSpanTests : UnknownQueryTestsBase
{
    [TestMethod]
    public void SumTimeSpanTest()
    {
        const string query = "table Periods {" +
                             "  Period 'System.TimeSpan'" +
                             "};" +
                             "couple #test.whatever with table Periods as Periods; " +
                             "select SumTimeSpan(Period) from Periods()";
        
        dynamic first = new ExpandoObject();
        
        first.Period = new TimeSpan(1, 0, 0);
        
        dynamic second = new ExpandoObject();
        
        second.Period = new TimeSpan(2, 0, 0);
        
        var vm = CreateAndRunVirtualMachine(query, new List<dynamic>()
        {
            first, second
        });
        
        var table = vm.Run();
        
        Assert.AreEqual(1, table.Columns.Count());
        Assert.AreEqual("SumTimeSpan(Period)", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(TimeSpan?), table.Columns.ElementAt(0).ColumnType);
        
        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(new TimeSpan(3, 0, 0), table[0].Values[0]);
    }
    
    [TestMethod]
    public void MinTimeSpanTest()
    {
        const string query = "table Periods {" +
                             "  Period 'System.TimeSpan'" +
                             "};" +
                             "couple #test.whatever with table Periods as Periods; " +
                             "select MinTimeSpan(Period) from Periods()";
        
        dynamic first = new ExpandoObject();
        
        first.Period = new TimeSpan(1, 0, 0);
        
        dynamic second = new ExpandoObject();
        
        second.Period = new TimeSpan(2, 0, 0);
        
        var vm = CreateAndRunVirtualMachine(query, new List<dynamic>()
        {
            first, second
        });
        
        var table = vm.Run();
        
        Assert.AreEqual(1, table.Columns.Count());
        Assert.AreEqual("MinTimeSpan(Period)", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(TimeSpan?), table.Columns.ElementAt(0).ColumnType);
        
        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(new TimeSpan(1, 0, 0), table[0].Values[0]);
    }
    
    [TestMethod]
    public void MaxTimeSpanTest()
    {
        const string query = "table Periods {" +
                             "  Period 'System.TimeSpan'" +
                             "};" +
                             "couple #test.whatever with table Periods as Periods; " +
                             "select MaxTimeSpan(Period) from Periods()";
        
        dynamic first = new ExpandoObject();
        
        first.Period = new TimeSpan(1, 0, 0);
        
        dynamic second = new ExpandoObject();
        
        second.Period = new TimeSpan(2, 0, 0);
        
        var vm = CreateAndRunVirtualMachine(query, new List<dynamic>()
        {
            first, second
        });
        
        var table = vm.Run();
        
        Assert.AreEqual(1, table.Columns.Count());
        Assert.AreEqual("MaxTimeSpan(Period)", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(TimeSpan?), table.Columns.ElementAt(0).ColumnType);
        
        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(new TimeSpan(2, 0, 0), table[0].Values[0]);
    }
}