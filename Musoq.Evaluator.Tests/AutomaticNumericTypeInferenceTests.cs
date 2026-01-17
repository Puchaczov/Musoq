using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Unknown;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class AutomaticNumericTypeInferenceTests : UnknownQueryTestsBase
{
    #region Type Promotion Tests

    [TestMethod]
    public void WhenComparingStringWithLongLiteral_ShouldPromoteToLong()
    {
        const string query = "table Items {" +
                             "  Size 'System.String'," +
                             "  Name 'System.String'" +
                             "};" +
                             "couple #test.whatever with table Items as Items; " +
                             "select Name from Items() where Size = 9223372036854775807l";

        dynamic item1 = new ExpandoObject();
        item1.Size = "9223372036854775807";
        item1.Name = "MaxLong";

        var vm = CreateAndRunVirtualMachine(query, [item1]);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("MaxLong", table[0].Values[0]);
    }

    #endregion

    #region Test Data Setup

    private static List<dynamic> CreateTestDataWithStringColumn()
    {
        dynamic item1 = new ExpandoObject();
        item1.Size = "500";
        item1.Name = "Small";

        dynamic item2 = new ExpandoObject();
        item2.Size = "1500";
        item2.Name = "Medium";

        dynamic item3 = new ExpandoObject();
        item3.Size = "2500";
        item3.Name = "Large";

        return [item1, item2, item3];
    }

    private static List<dynamic> CreateTestDataWithObjectColumn()
    {
        dynamic item1 = new ExpandoObject();
        item1.Value = (object)500;
        item1.Name = "Small";

        dynamic item2 = new ExpandoObject();
        item2.Value = (object)1500;
        item2.Name = "Medium";

        dynamic item3 = new ExpandoObject();
        item3.Value = (object)2500;
        item3.Name = "Large";

        return [item1, item2, item3];
    }

    private static List<dynamic> CreateTestDataWithObjectFloatColumn()
    {
        dynamic item1 = new ExpandoObject();
        item1.Price = (object)19.99;
        item1.Name = "Item A";

        dynamic item2 = new ExpandoObject();
        item2.Price = (object)25.50;
        item2.Name = "Item B";

        dynamic item3 = new ExpandoObject();
        item3.Price = (object)30.00;
        item3.Name = "Item C";

        return [item1, item2, item3];
    }

    #endregion

    #region String Column Tests

    [TestMethod]
    public void WhenComparingStringColumnWithIntLiteral_UsingGreaterThan_ShouldAutoConvert()
    {
        const string query = "table Items {" +
                             "  Size 'System.String'," +
                             "  Name 'System.String'" +
                             "};" +
                             "couple #test.whatever with table Items as Items; " +
                             "select Name from Items() where Size > 1000";

        var vm = CreateAndRunVirtualMachine(query, CreateTestDataWithStringColumn());
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        var results = table.Select(row => row.Values[0]).Cast<string>().OrderBy(x => x).ToList();
        CollectionAssert.Contains(results, "Medium");
        CollectionAssert.Contains(results, "Large");
    }

    [TestMethod]
    public void WhenComparingStringColumnWithIntLiteral_UsingLessThan_ShouldAutoConvert()
    {
        const string query = "table Items {" +
                             "  Size 'System.String'," +
                             "  Name 'System.String'" +
                             "};" +
                             "couple #test.whatever with table Items as Items; " +
                             "select Name from Items() where Size < 1000";

        var vm = CreateAndRunVirtualMachine(query, CreateTestDataWithStringColumn());
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Small", table[0].Values[0]);
    }

    [TestMethod]
    public void WhenComparingStringColumnWithIntLiteral_UsingEquals_ShouldAutoConvert()
    {
        const string query = "table Items {" +
                             "  Size 'System.String'," +
                             "  Name 'System.String'" +
                             "};" +
                             "couple #test.whatever with table Items as Items; " +
                             "select Name from Items() where Size = 1500";

        var vm = CreateAndRunVirtualMachine(query, CreateTestDataWithStringColumn());
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Medium", table[0].Values[0]);
    }

    [TestMethod]
    public void WhenComparingStringColumnWithIntLiteral_UsingNotEquals_ShouldAutoConvert()
    {
        const string query = "table Items {" +
                             "  Size 'System.String'," +
                             "  Name 'System.String'" +
                             "};" +
                             "couple #test.whatever with table Items as Items; " +
                             "select Name from Items() where Size <> 1500";

        var vm = CreateAndRunVirtualMachine(query, CreateTestDataWithStringColumn());
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        var results = table.Select(row => row.Values[0]).Cast<string>().OrderBy(x => x).ToList();
        CollectionAssert.Contains(results, "Small");
        CollectionAssert.Contains(results, "Large");
    }

    [TestMethod]
    public void WhenComparingStringColumnWithIntLiteral_UsingGreaterOrEqual_ShouldAutoConvert()
    {
        const string query = "table Items {" +
                             "  Size 'System.String'," +
                             "  Name 'System.String'" +
                             "};" +
                             "couple #test.whatever with table Items as Items; " +
                             "select Name from Items() where Size >= 1500";

        var vm = CreateAndRunVirtualMachine(query, CreateTestDataWithStringColumn());
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        var results = table.Select(row => row.Values[0]).Cast<string>().OrderBy(x => x).ToList();
        CollectionAssert.Contains(results, "Medium");
        CollectionAssert.Contains(results, "Large");
    }

    [TestMethod]
    public void WhenComparingStringColumnWithIntLiteral_UsingLessOrEqual_ShouldAutoConvert()
    {
        const string query = "table Items {" +
                             "  Size 'System.String'," +
                             "  Name 'System.String'" +
                             "};" +
                             "couple #test.whatever with table Items as Items; " +
                             "select Name from Items() where Size <= 1500";

        var vm = CreateAndRunVirtualMachine(query, CreateTestDataWithStringColumn());
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        var results = table.Select(row => row.Values[0]).Cast<string>().OrderBy(x => x).ToList();
        CollectionAssert.Contains(results, "Small");
        CollectionAssert.Contains(results, "Medium");
    }

    #endregion

    #region Object Column Tests - Integer Literals

    [TestMethod]
    public void WhenComparingObjectColumnWithIntLiteral_UsingGreaterThan_ShouldAutoConvert()
    {
        const string query = "table Items {" +
                             "  Value 'System.Object'," +
                             "  Name 'System.String'" +
                             "};" +
                             "couple #test.whatever with table Items as Items; " +
                             "select Name from Items() where Value > 1000";

        var vm = CreateAndRunVirtualMachine(query, CreateTestDataWithObjectColumn());
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        var results = table.Select(row => row.Values[0]).Cast<string>().OrderBy(x => x).ToList();
        CollectionAssert.Contains(results, "Medium");
        CollectionAssert.Contains(results, "Large");
    }

    [TestMethod]
    public void WhenComparingObjectColumnWithIntLiteral_UsingEquals_ShouldAutoConvert()
    {
        const string query = "table Items {" +
                             "  Value 'System.Object'," +
                             "  Name 'System.String'" +
                             "};" +
                             "couple #test.whatever with table Items as Items; " +
                             "select Name from Items() where Value = 1500";

        var vm = CreateAndRunVirtualMachine(query, CreateTestDataWithObjectColumn());
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Medium", table[0].Values[0]);
    }

    [TestMethod]
    public void WhenComparingObjectColumnWithZero_ShouldAutoConvert()
    {
        const string query = "table Items {" +
                             "  Count 'System.Object'," +
                             "  Name 'System.String'" +
                             "};" +
                             "couple #test.whatever with table Items as Items; " +
                             "select Name from Items() where Count = 0";

        dynamic item1 = new ExpandoObject();
        item1.Count = (object)0;
        item1.Name = "Zero";

        dynamic item2 = new ExpandoObject();
        item2.Count = (object)5;
        item2.Name = "NonZero";

        var vm = CreateAndRunVirtualMachine(query, [item1, item2]);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Zero", table[0].Values[0]);
    }

    #endregion

    #region Object Column Tests - Decimal Literals

    [TestMethod]
    public void WhenComparingObjectColumnWithDecimalLiteral_ShouldAutoConvert()
    {
        const string query = "table Items {" +
                             "  Price 'System.Object'," +
                             "  Name 'System.String'" +
                             "};" +
                             "couple #test.whatever with table Items as Items; " +
                             "select Name from Items() where Price < 19.99";

        var vm = CreateAndRunVirtualMachine(query, CreateTestDataWithObjectFloatColumn());
        var table = vm.Run(TestContext.CancellationToken);


        Assert.AreEqual(0, table.Count);
    }

    [TestMethod]
    public void WhenComparingObjectColumnWithDecimalLiteral_UsingEquals_ShouldAutoConvert()
    {
        const string query = "table Items {" +
                             "  Price 'System.Object'," +
                             "  Name 'System.String'" +
                             "};" +
                             "couple #test.whatever with table Items as Items; " +
                             "select Name from Items() where Price = 30.00";

        var vm = CreateAndRunVirtualMachine(query, CreateTestDataWithObjectFloatColumn());
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Item C", table[0].Values[0]);
    }

    #endregion

    #region Strict Mode - Precision Loss Tests

    [TestMethod]
    public void WhenComparingObjectFloatWithIntLiteral_WithPrecisionLoss_ShouldNotMatch()
    {
        const string query = "table Items {" +
                             "  Value 'System.Object'," +
                             "  Name 'System.String'" +
                             "};" +
                             "couple #test.whatever with table Items as Items; " +
                             "select Name from Items() where Value = 100";

        dynamic item1 = new ExpandoObject();
        item1.Value = (object)100.5;
        item1.Name = "Float";

        dynamic item2 = new ExpandoObject();
        item2.Value = (object)100;
        item2.Name = "Int";

        var vm = CreateAndRunVirtualMachine(query, [item1, item2]);
        var table = vm.Run(TestContext.CancellationToken);


        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Int", table[0].Values[0]);
    }

    [TestMethod]
    public void WhenComparingObjectFloatWithDecimalLiteral_ShouldAllowConversion()
    {
        const string query = "table Items {" +
                             "  Price 'System.Object'," +
                             "  Name 'System.String'" +
                             "};" +
                             "couple #test.whatever with table Items as Items; " +
                             "select Name from Items() where Price >= 19.99";

        var vm = CreateAndRunVirtualMachine(query, CreateTestDataWithObjectFloatColumn());
        var table = vm.Run(TestContext.CancellationToken);


        Assert.AreEqual(3, table.Count);
    }

    #endregion

    #region Bidirectional Comparison Tests

    [TestMethod]
    public void WhenComparingIntLiteralWithStringColumn_Reversed_ShouldAutoConvert()
    {
        const string query = "table Items {" +
                             "  Size 'System.String'," +
                             "  Name 'System.String'" +
                             "};" +
                             "couple #test.whatever with table Items as Items; " +
                             "select Name from Items() where 1000 < Size";

        var vm = CreateAndRunVirtualMachine(query, CreateTestDataWithStringColumn());
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        var results = table.Select(row => row.Values[0]).Cast<string>().OrderBy(x => x).ToList();
        CollectionAssert.Contains(results, "Medium");
        CollectionAssert.Contains(results, "Large");
    }

    [TestMethod]
    public void WhenComparingIntLiteralWithObjectColumn_Reversed_ShouldAutoConvert()
    {
        const string query = "table Items {" +
                             "  Value 'System.Object'," +
                             "  Name 'System.String'" +
                             "};" +
                             "couple #test.whatever with table Items as Items; " +
                             "select Name from Items() where 1500 = Value";

        var vm = CreateAndRunVirtualMachine(query, CreateTestDataWithObjectColumn());
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Medium", table[0].Values[0]);
    }

    #endregion

    #region CASE WHEN Tests

    [TestMethod]
    public void WhenUsingStringColumnComparisonInCaseWhen_ShouldAutoConvert()
    {
        const string query = "table Items {" +
                             "  Size 'System.String'," +
                             "  Name 'System.String'" +
                             "};" +
                             "couple #test.whatever with table Items as Items; " +
                             "select Name, " +
                             "case when Size < 1000 then 'Small' " +
                             "     when Size <= 2000 then 'Medium' " +
                             "     else 'Large' end as Category " +
                             "from Items()";

        var vm = CreateAndRunVirtualMachine(query, CreateTestDataWithStringColumn());
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);

        var results = table.Select(row => new { Name = row.Values[0] as string, Category = row.Values[1] as string })
            .OrderBy(x => x.Name).ToList();

        Assert.AreEqual("Large", results[0].Name);
        Assert.AreEqual("Large", results[0].Category);

        Assert.AreEqual("Medium", results[1].Name);
        Assert.AreEqual("Medium", results[1].Category);

        Assert.AreEqual("Small", results[2].Name);
        Assert.AreEqual("Small", results[2].Category);
    }

    [TestMethod]
    public void WhenUsingObjectColumnComparisonInCaseWhen_ShouldAutoConvert()
    {
        const string query = "table Items {" +
                             "  Value 'System.Object'," +
                             "  Name 'System.String'" +
                             "};" +
                             "couple #test.whatever with table Items as Items; " +
                             "select Name, " +
                             "case when Value = 0 then 'Zero' " +
                             "     when Value > 1000 then 'High' " +
                             "     else 'Low' end as Level " +
                             "from Items()";

        var vm = CreateAndRunVirtualMachine(query, CreateTestDataWithObjectColumn());
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);

        var highCount = table.Count(row => row.Values[1] as string == "High");
        var lowCount = table.Count(row => row.Values[1] as string == "Low");

        Assert.AreEqual(2, highCount);
        Assert.AreEqual(1, lowCount);
    }

    #endregion

    #region Edge Cases

    [TestMethod]
    public void WhenStringColumnContainsInvalidNumber_ShouldHandleGracefully()
    {
        const string query = "table Items {" +
                             "  Size 'System.String'," +
                             "  Name 'System.String'" +
                             "};" +
                             "couple #test.whatever with table Items as Items; " +
                             "select Name from Items() where Size > 1000";

        dynamic item1 = new ExpandoObject();
        item1.Size = "abc";
        item1.Name = "Invalid";

        dynamic item2 = new ExpandoObject();
        item2.Size = "2000";
        item2.Name = "Valid";

        var vm = CreateAndRunVirtualMachine(query, [item1, item2]);
        var table = vm.Run(TestContext.CancellationToken);


        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Valid", table[0].Values[0]);
    }

    [TestMethod]
    public void WhenObjectColumnIsNull_ShouldHandleGracefully()
    {
        const string query = "table Items {" +
                             "  Value 'System.Object'," +
                             "  Name 'System.String'" +
                             "};" +
                             "couple #test.whatever with table Items as Items; " +
                             "select Name from Items() where Value > 1000";

        dynamic item1 = new ExpandoObject();
        item1.Value = null;
        item1.Name = "Null";

        dynamic item2 = new ExpandoObject();
        item2.Value = (object)2000;
        item2.Name = "Valid";

        var vm = CreateAndRunVirtualMachine(query, [item1, item2]);
        var table = vm.Run(TestContext.CancellationToken);


        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Valid", table[0].Values[0]);
    }

    [TestMethod]
    public void WhenComparingWithHexLiteral_ShouldAutoConvert()
    {
        const string query = "table Items {" +
                             "  Size 'System.String'," +
                             "  Name 'System.String'" +
                             "};" +
                             "couple #test.whatever with table Items as Items; " +
                             "select Name from Items() where Size = 0xFF";

        dynamic item1 = new ExpandoObject();
        item1.Size = "255";
        item1.Name = "Match";

        dynamic item2 = new ExpandoObject();
        item2.Size = "100";
        item2.Name = "NoMatch";

        var vm = CreateAndRunVirtualMachine(query, [item1, item2]);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Match", table[0].Values[0]);
    }

    [TestMethod]
    public void WhenComparingWithBinaryLiteral_ShouldAutoConvert()
    {
        const string query = "table Items {" +
                             "  Size 'System.String'," +
                             "  Name 'System.String'" +
                             "};" +
                             "couple #test.whatever with table Items as Items; " +
                             "select Name from Items() where Size > 0b1111";

        dynamic item1 = new ExpandoObject();
        item1.Size = "10";
        item1.Name = "Small";

        dynamic item2 = new ExpandoObject();
        item2.Size = "20";
        item2.Name = "Large";

        var vm = CreateAndRunVirtualMachine(query, [item1, item2]);
        var table = vm.Run(TestContext.CancellationToken);


        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Large", table[0].Values[0]);
    }

    #endregion

    #region Phase 1: Critical Edge Cases

    [TestMethod]
    public void WhenComparingStringColumnWithInvalidNumericString_ShouldNotMatch()
    {
        const string query = "table Items {" +
                             "  Size 'System.String'," +
                             "  Name 'System.String'" +
                             "};" +
                             "couple #test.whatever with table Items as Items; " +
                             "select Name from Items() where Size > 1000";

        dynamic item1 = new ExpandoObject();
        item1.Size = "not_a_number";
        item1.Name = "Invalid";

        dynamic item2 = new ExpandoObject();
        item2.Size = "1500";
        item2.Name = "Valid";

        var vm = CreateAndRunVirtualMachine(query, [item1, item2]);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Valid", table[0].Values[0]);
    }

    [TestMethod]
    public void WhenComparingObjectColumnWithNaN_ShouldNotMatch()
    {
        const string query = "table Items {" +
                             "  Value 'System.Object'," +
                             "  Name 'System.String'" +
                             "};" +
                             "couple #test.whatever with table Items as Items; " +
                             "select Name from Items() where Value = 1000";

        dynamic item1 = new ExpandoObject();
        item1.Value = (object)double.NaN;
        item1.Name = "NaN";

        dynamic item2 = new ExpandoObject();
        item2.Value = (object)1000.0;
        item2.Name = "Valid";

        var vm = CreateAndRunVirtualMachine(query, [item1, item2]);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Valid", table[0].Values[0]);
    }

    [TestMethod]
    public void WhenComparingObjectColumnWithPositiveInfinity_ShouldNotMatch()
    {
        const string query = "table Items {" +
                             "  Value 'System.Object'," +
                             "  Name 'System.String'" +
                             "};" +
                             "couple #test.whatever with table Items as Items; " +
                             "select Name from Items() where Value < 2000";

        dynamic item1 = new ExpandoObject();
        item1.Value = (object)double.PositiveInfinity;
        item1.Name = "PosInf";

        dynamic item2 = new ExpandoObject();
        item2.Value = (object)1000.0;
        item2.Name = "Valid";

        var vm = CreateAndRunVirtualMachine(query, [item1, item2]);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Valid", table[0].Values[0]);
    }

    [TestMethod]
    public void WhenComparingStringColumnWithNull_ShouldNotMatch()
    {
        const string query = "table Items {" +
                             "  Size 'System.String'," +
                             "  Name 'System.String'" +
                             "};" +
                             "couple #test.whatever with table Items as Items; " +
                             "select Name from Items() where Size = 1000";

        dynamic item1 = new ExpandoObject();
        item1.Size = (string)null;
        item1.Name = "NullString";

        dynamic item2 = new ExpandoObject();
        item2.Size = "1000";
        item2.Name = "Valid";

        var vm = CreateAndRunVirtualMachine(query, [item1, item2]);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Valid", table[0].Values[0]);
    }

    [TestMethod]
    public void WhenComparingObjectDoubleWithIntLiteral_WithVerySmallFraction_ShouldNotMatch()
    {
        const string query = "table Items {" +
                             "  Value 'System.Object'," +
                             "  Name 'System.String'" +
                             "};" +
                             "couple #test.whatever with table Items as Items; " +
                             "select Name from Items() where Value = 1000";

        dynamic item1 = new ExpandoObject();
        item1.Value = (object)1000.0000001;
        item1.Name = "SmallFraction";

        dynamic item2 = new ExpandoObject();
        item2.Value = (object)1000;
        item2.Name = "Exact";

        var vm = CreateAndRunVirtualMachine(query, [item1, item2]);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Exact", table[0].Values[0]);
    }

    [TestMethod]
    public void WhenComparingObjectFloatWithIntLiteral_WithNegativeFraction_ShouldNotMatch()
    {
        const string query = "table Items {" +
                             "  Value 'System.Object'," +
                             "  Name 'System.String'" +
                             "};" +
                             "couple #test.whatever with table Items as Items; " +
                             "select Name from Items() where Value = -100";

        dynamic item1 = new ExpandoObject();
        item1.Value = (object)-100.5f;
        item1.Name = "NegativeFraction";

        dynamic item2 = new ExpandoObject();
        item2.Value = (object)-100;
        item2.Name = "Exact";

        var vm = CreateAndRunVirtualMachine(query, [item1, item2]);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Exact", table[0].Values[0]);
    }

    [TestMethod]
    public void WhenComparingStringColumnWithBoundaryMinInt32_ShouldMatch()
    {
        const string query = "table Items {" +
                             "  Size 'System.String'," +
                             "  Name 'System.String'" +
                             "};" +
                             "couple #test.whatever with table Items as Items; " +
                             "select Name from Items() where Size = -2147483648";

        dynamic item1 = new ExpandoObject();
        item1.Size = "-2147483648";
        item1.Name = "MinInt32";

        var vm = CreateAndRunVirtualMachine(query, [item1]);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("MinInt32", table[0].Values[0]);
    }

    [TestMethod]
    public void WhenComparingStringColumnWithBoundaryMaxInt32_ShouldMatch()
    {
        const string query = "table Items {" +
                             "  Size 'System.String'," +
                             "  Name 'System.String'" +
                             "};" +
                             "couple #test.whatever with table Items as Items; " +
                             "select Name from Items() where Size = 2147483647";

        dynamic item1 = new ExpandoObject();
        item1.Size = "2147483647";
        item1.Name = "MaxInt32";

        var vm = CreateAndRunVirtualMachine(query, [item1]);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("MaxInt32", table[0].Values[0]);
    }

    [TestMethod]
    public void WhenComparingStringColumnWithZeroAndNegativeZero_ShouldMatchBoth()
    {
        const string query = "table Items {" +
                             "  Size 'System.String'," +
                             "  Name 'System.String'" +
                             "};" +
                             "couple #test.whatever with table Items as Items; " +
                             "select Name from Items() where Size = 0";

        dynamic item1 = new ExpandoObject();
        item1.Size = "0";
        item1.Name = "Zero";

        dynamic item2 = new ExpandoObject();
        item2.Size = "-0";
        item2.Name = "NegativeZero";

        var vm = CreateAndRunVirtualMachine(query, [item1, item2]);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
    }

    [TestMethod]
    public void WhenComparingObjectColumnWithBoundaryMaxInt32_StrictMode_ShouldMatchExactly()
    {
        const string query = "table Items {" +
                             "  Value 'System.Object'," +
                             "  Name 'System.String'" +
                             "};" +
                             "couple #test.whatever with table Items as Items; " +
                             "select Name from Items() where Value = 2147483647";

        dynamic item1 = new ExpandoObject();
        item1.Value = (object)2147483647;
        item1.Name = "Exact";

        dynamic item2 = new ExpandoObject();
        item2.Value = (object)2147483647.1;
        item2.Name = "WithFraction";

        var vm = CreateAndRunVirtualMachine(query, [item1, item2]);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Exact", table[0].Values[0]);
    }

    public TestContext TestContext { get; set; }

    #endregion
}