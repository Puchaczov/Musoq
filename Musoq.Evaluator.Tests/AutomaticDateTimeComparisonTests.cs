using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Unknown;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class AutomaticDateTimeComparisonTests : UnknownQueryTestsBase
{
    [TestMethod]
    public void WhenComparingDateTimeColumnWithStringLiteral_ShouldAutomaticallyConvert()
    {
        const string query = "table Events {" +
                             "  EventDate 'System.DateTime'," +
                             "  Name 'System.String'" +
                             "};" +
                             "couple #test.whatever with table Events as Events; " +
                             "select Name from Events() where EventDate > '2023-01-01'";

        dynamic first = new ExpandoObject();
        first.EventDate = new DateTime(2023, 6, 15);
        first.Name = "Event 1";

        dynamic second = new ExpandoObject();
        second.EventDate = new DateTime(2022, 12, 31);
        second.Name = "Event 2";

        var vm = CreateAndRunVirtualMachine(query, new List<dynamic>
        {
            first, second
        });

        var table = vm.Run();

        Assert.AreEqual(1, table.Columns.Count());
        Assert.AreEqual("Name", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Event 1", table[0].Values[0]);
    }

    [TestMethod]
    public void WhenComparingDateTimeOffsetColumnWithStringLiteral_ShouldAutomaticallyConvert()
    {
        const string query = "table Events {" +
                             "  EventDate 'System.DateTimeOffset'," +
                             "  Name 'System.String'" +
                             "};" +
                             "couple #test.whatever with table Events as Events; " +
                             "select Name from Events() where EventDate = '2023-01-01T12:00:00+00:00'";

        dynamic first = new ExpandoObject();
        first.EventDate = new DateTimeOffset(2023, 1, 1, 12, 0, 0, TimeSpan.Zero);
        first.Name = "Event 1";

        dynamic second = new ExpandoObject();
        second.EventDate = new DateTimeOffset(2023, 1, 2, 12, 0, 0, TimeSpan.Zero);
        second.Name = "Event 2";

        var vm = CreateAndRunVirtualMachine(query, new List<dynamic>
        {
            first, second
        });

        var table = vm.Run();

        Assert.AreEqual(1, table.Columns.Count());
        Assert.AreEqual("Name", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Event 1", table[0].Values[0]);
    }

    [TestMethod]
    public void WhenComparingTimeSpanColumnWithStringLiteral_ShouldAutomaticallyConvert()
    {
        const string query = "table Events {" +
                             "  Duration 'System.TimeSpan'," +
                             "  Name 'System.String'" +
                             "};" +
                             "couple #test.whatever with table Events as Events; " +
                             "select Name from Events() where Duration >= '02:00:00'";

        dynamic first = new ExpandoObject();
        first.Duration = new TimeSpan(3, 0, 0);
        first.Name = "Event 1";

        dynamic second = new ExpandoObject();
        second.Duration = new TimeSpan(1, 30, 0);
        second.Name = "Event 2";

        var vm = CreateAndRunVirtualMachine(query, new List<dynamic>
        {
            first, second
        });

        var table = vm.Run();

        Assert.AreEqual(1, table.Columns.Count());
        Assert.AreEqual("Name", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Event 1", table[0].Values[0]);
    }

    [TestMethod]
    public void WhenComparingWithVariousDateFormats_ShouldAutomaticallyConvert()
    {
        const string query = "table Events {" +
                             "  EventDate 'System.DateTime'," +
                             "  Name 'System.String'" +
                             "};" +
                             "couple #test.whatever with table Events as Events; " +
                             "select Name from Events() where EventDate <= '12/31/2023'";

        dynamic first = new ExpandoObject();
        first.EventDate = new DateTime(2023, 12, 15);
        first.Name = "Event 1";

        dynamic second = new ExpandoObject();
        second.EventDate = new DateTime(2024, 1, 5);
        second.Name = "Event 2";

        var vm = CreateAndRunVirtualMachine(query, new List<dynamic>
        {
            first, second
        });

        var table = vm.Run();

        Assert.AreEqual(1, table.Columns.Count());
        Assert.AreEqual("Name", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Event 1", table[0].Values[0]);
    }

    [TestMethod]
    public void WhenReversedStringDateTimeComparison_ShouldAutomaticallyConvert()
    {
        const string query = "table Events {" +
                             "  EventDate 'System.DateTime'," +
                             "  Name 'System.String'" +
                             "};" +
                             "couple #test.whatever with table Events as Events; " +
                             "select Name from Events() where '2023-01-01' < EventDate";

        dynamic first = new ExpandoObject();
        first.EventDate = new DateTime(2023, 6, 15);
        first.Name = "Event 1";

        dynamic second = new ExpandoObject();
        second.EventDate = new DateTime(2022, 12, 31);
        second.Name = "Event 2";

        var vm = CreateAndRunVirtualMachine(query, new List<dynamic>
        {
            first, second
        });

        var table = vm.Run();

        Assert.AreEqual(1, table.Columns.Count());
        Assert.AreEqual("Name", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Event 1", table[0].Values[0]);
    }
}