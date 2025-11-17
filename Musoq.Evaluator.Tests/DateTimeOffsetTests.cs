using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Unknown;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class DateTimeOffsetTests : UnknownQueryTestsBase
{
    [TestMethod]
    public void MinDateTimeOffsetTest()
    {
        const string query = "table Dates {" +
                             "  Date 'System.DateTimeOffset'" +
                             "};" +
                             "couple #test.whatever with table Dates as Dates; " +
                             "select MinDateTimeOffset(Date) from Dates()";
        
        dynamic first = new ExpandoObject();
        first.Date = new DateTimeOffset(new DateTime(2023, 1, 1));
        
        dynamic second = new ExpandoObject();
        second.Date = new DateTimeOffset(new DateTime(2023, 2, 1));
        
        var vm = CreateAndRunVirtualMachine(query,
        [
            first, second
        ]);
        
        var table = vm.Run();
        
        Assert.AreEqual(1, table.Columns.Count());
        Assert.AreEqual("MinDateTimeOffset(Date)", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(DateTimeOffset?), table.Columns.ElementAt(0).ColumnType);
        
        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(new DateTimeOffset(new DateTime(2023, 1, 1)), table[0].Values[0]);
    }

    [TestMethod]
    public void MaxDateTimeOffsetTest()
    {
        const string query = "table Dates {" +
                             "  Date 'System.DateTimeOffset'" +
                             "};" +
                             "couple #test.whatever with table Dates as Dates; " +
                             "select MaxDateTimeOffset(Date) from Dates()";
        
        dynamic first = new ExpandoObject();
        first.Date = new DateTimeOffset(new DateTime(2023, 1, 1));
        
        dynamic second = new ExpandoObject();
        second.Date = new DateTimeOffset(new DateTime(2023, 2, 1));
        
        var vm = CreateAndRunVirtualMachine(query,
        [
            first, second
        ]);
        
        var table = vm.Run();
        
        Assert.AreEqual(1, table.Columns.Count());
        Assert.AreEqual("MaxDateTimeOffset(Date)", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(DateTimeOffset?), table.Columns.ElementAt(0).ColumnType);
        
        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(new DateTimeOffset(new DateTime(2023, 2, 1)), table[0].Values[0]);
    }
    
    [TestMethod]
    public void SubtractDateTimeOffsetsTest()
    {
        const string query = "table Dates {" +
                             "  Date1 'System.DateTimeOffset'," +
                             "  Date2 'System.DateTimeOffset'" +
                             "};" +
                             "couple #test.whatever with table Dates as Dates; " +
                             "select SubtractDateTimeOffsets(Date1, Date2) from Dates()";
        
        dynamic first = new ExpandoObject();
        first.Date1 = new DateTimeOffset(new DateTime(2023, 1, 1));
        first.Date2 = new DateTimeOffset(new DateTime(2023, 1, 1));
        
        var vm = CreateAndRunVirtualMachine(query,
        [
            first
        ]);
        
        var table = vm.Run();
        
        Assert.AreEqual(1, table.Columns.Count());
        Assert.AreEqual("SubtractDateTimeOffsets(Date1, Date2)", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(TimeSpan?), table.Columns.ElementAt(0).ColumnType);
        
        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(TimeSpan.Zero, table[0].Values[0]);
    }
    
    [TestMethod]
    public void SubtractDateTimeOffsets_MinusOperatorTest()
    {
        const string query = "table Dates {" +
                             "  Date1 'System.DateTimeOffset'," +
                             "  Date2 'System.DateTimeOffset'" +
                             "};" +
                             "couple #test.whatever with table Dates as Dates; " +
                             "select Date1 - Date2 from Dates()";
        
        dynamic first = new ExpandoObject();
        first.Date1 = new DateTimeOffset(new DateTime(2023, 1, 1));
        first.Date2 = new DateTimeOffset(new DateTime(2023, 1, 1));
        
        var vm = CreateAndRunVirtualMachine(query,
        [
            first
        ]);
        
        var table = vm.Run();
        
        Assert.AreEqual(1, table.Columns.Count());
        Assert.AreEqual("Date1 - Date2", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(TimeSpan), table.Columns.ElementAt(0).ColumnType);
        
        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(TimeSpan.Zero, table[0].Values[0]);
    }
}
