using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Unknown;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class AutomaticNumericTypeInferenceOverflowTests : UnknownQueryTestsBase
{
    #region Phase 7: Overflow and Type Boundary Tests

    [TestMethod]
    public void WhenStringValueOverflowsInt32_ShouldExcludeRow()
    {
        const string query = "table Items {" +
                             "  Age 'System.String'," +
                             "  Name 'System.String'" +
                             "};" +
                             "couple #test.whatever with table Items as Items; " +
                             "select Name from Items() where Age = 100";

        dynamic item1 = new ExpandoObject();
        item1.Age = "100";
        item1.Name = "ValidAge";

        dynamic item2 = new ExpandoObject();
        item2.Age = "2147483648";
        item2.Name = "OverflowAge";

        dynamic item3 = new ExpandoObject();
        item3.Age = "-2147483649";
        item3.Name = "UnderflowAge";

        var vm = CreateAndRunVirtualMachine(query, [item1, item2, item3]);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count, "One row with Age=100 should match");
        Assert.AreEqual("ValidAge", table[0].Values[0]);
    }

    [TestMethod]
    public void WhenStringValueOverflowsInt64_ShouldExcludeRow()
    {
        const string query = "table Items {" +
                             "  Count 'System.String'," +
                             "  Name 'System.String'" +
                             "};" +
                             "couple #test.whatever with table Items as Items; " +
                             "select Name from Items() where Count = 9223372036854775807";

        dynamic item1 = new ExpandoObject();
        item1.Count = "9223372036854775807";
        item1.Name = "MaxLong";

        dynamic item2 = new ExpandoObject();
        item2.Count = "9223372036854775808";
        item2.Name = "OverflowLong";

        dynamic item3 = new ExpandoObject();
        item3.Count = "100";
        item3.Name = "Normal";

        var vm = CreateAndRunVirtualMachine(query, [item1, item2, item3]);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count, "Only the max valid long value should match");
        Assert.AreEqual("MaxLong", table[0].Values[0]);
    }

    [TestMethod]
    public void WhenStringValueHasDecimalPart_AndComparingToInt_ShouldExcludeRow()
    {
        const string query = "table Items {" +
                             "  Amount 'System.String'," +
                             "  Name 'System.String'" +
                             "};" +
                             "couple #test.whatever with table Items as Items; " +
                             "select Name from Items() where Amount = 100";

        dynamic item1 = new ExpandoObject();
        item1.Amount = "100";
        item1.Name = "WholeNumber";

        dynamic item2 = new ExpandoObject();
        item2.Amount = "100.5";
        item2.Name = "DecimalNumber";

        dynamic item3 = new ExpandoObject();
        item3.Amount = "100.0";
        item3.Name = "DecimalWhole";

        var vm = CreateAndRunVirtualMachine(query, [item1, item2, item3]);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count, "Only the whole number should match");
        Assert.AreEqual("WholeNumber", table[0].Values[0]);
    }

    [TestMethod]
    public void WhenComparingStringToDecimalLiteral_OverflowDecimalRange_ShouldExcludeRow()
    {
        const string query = "table Items {" +
                             "  Price 'System.String'," +
                             "  Name 'System.String'" +
                             "};" +
                             "couple #test.whatever with table Items as Items; " +
                             "select Name from Items() where Price = 100.50";

        dynamic item1 = new ExpandoObject();
        item1.Price = "100,50";
        item1.Name = "ValidPrice";

        dynamic item2 = new ExpandoObject();
        item2.Price = "999999999999999999999999999999999";
        item2.Name = "OverflowPrice";

        dynamic item3 = new ExpandoObject();
        item3.Price = "not_a_number";
        item3.Name = "InvalidPrice";

        var vm = CreateAndRunVirtualMachine(query, [item1, item2, item3]);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count, "ValidPrice with 100.50 should match");
        Assert.AreEqual("ValidPrice", table[0].Values[0]);
    }

    [TestMethod]
    public void WhenMultipleRowsWithMixedValidAndOverflow_OnlyValidRowsMatch()
    {
        const string query = "table Items {" +
                             "  Size 'System.String'," +
                             "  Name 'System.String'" +
                             "};" +
                             "couple #test.whatever with table Items as Items; " +
                             "select Name from Items() where Size > 1000";

        var items = new List<dynamic>();
        
        dynamic item1 = new ExpandoObject();
        item1.Size = "1500";
        item1.Name = "Valid1";
        items.Add(item1);

        dynamic item2 = new ExpandoObject();
        item2.Size = "2000";
        item2.Name = "Valid2";
        items.Add(item2);

        dynamic item3 = new ExpandoObject();
        item3.Size = "2147483648";
        item3.Name = "Overflow";
        items.Add(item3);

        dynamic item4 = new ExpandoObject();
        item4.Size = "abc";
        item4.Name = "Invalid";
        items.Add(item4);

        dynamic item5 = new ExpandoObject();
        item5.Size = "500";
        item5.Name = "TooSmall";
        items.Add(item5);

        dynamic item6 = new ExpandoObject();
        item6.Size = null;
        item6.Name = "Null";
        items.Add(item6);

        var vm = CreateAndRunVirtualMachine(query, items);
        var table = vm.Run();

        Assert.AreEqual(2, table.Count, "Only 2 valid rows above threshold should match");
        var names = table.Select(row => (string)row[0]).ToList();
        CollectionAssert.Contains(names, "Valid1");
        CollectionAssert.Contains(names, "Valid2");
    }

    [TestMethod]
    public void WhenObjectTypeOverflowsDuringConversion_ShouldExcludeRow()
    {
        const string query = "table Items {" +
                             "  Value 'System.Object'," +
                             "  Name 'System.String'" +
                             "};" +
                             "couple #test.whatever with table Items as Items; " +
                             "select Name from Items() where Value = 100";

        dynamic item1 = new ExpandoObject();
        item1.Value = (object)100;
        item1.Name = "Match";

        dynamic item2 = new ExpandoObject();
        item2.Value = (object)100.5;
        item2.Name = "HasDecimal";

        dynamic item3 = new ExpandoObject();
        item3.Value = (object)9999999999L;
        item3.Name = "TooLarge";

        var vm = CreateAndRunVirtualMachine(query, [item1, item2, item3]);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count, "Only exact integer match should succeed");
        Assert.AreEqual("Match", table[0].Values[0]);
    }

    [TestMethod]
    public void WhenStringValueAtBoundary_ShouldMatchCorrectly()
    {
        const string query = "table Items {" +
                             "  Value 'System.String'," +
                             "  Name 'System.String'" +
                             "};" +
                             "couple #test.whatever with table Items as Items; " +
                             "select Name from Items() where Value = 2147483647";

        dynamic item1 = new ExpandoObject();
        item1.Value = "2147483647";
        item1.Name = "MaxValue";

        dynamic item2 = new ExpandoObject();
        item2.Value = "2147483648";
        item2.Name = "Overflow";

        var vm = CreateAndRunVirtualMachine(query, [item1, item2]);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count, "Only MaxValue should match");
        Assert.AreEqual("MaxValue", table[0].Values[0]);

        const string query2 = "table Items {" +
                              "  Value 'System.String'," +
                              "  Name 'System.String'" +
                              "};" +
                              "couple #test.whatever with table Items as Items; " +
                              "select Name from Items() where Value = -2147483648";

        dynamic item3 = new ExpandoObject();
        item3.Value = "-2147483648";
        item3.Name = "MinValue";

        dynamic item4 = new ExpandoObject();
        item4.Value = "-2147483649";
        item4.Name = "Underflow";

        var vm2 = CreateAndRunVirtualMachine(query2, [item3, item4]);
        var table2 = vm2.Run();

        Assert.AreEqual(1, table2.Count, "Only MinValue should match");
        Assert.AreEqual("MinValue", table2[0].Values[0]);
    }

    #endregion
}
