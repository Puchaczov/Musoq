using System.Dynamic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public partial class AutomaticNumericTypeInferenceTests
{
    [TestMethod]
    public void WhenComparingStringColumnWithNull_ShouldNotMatch()
    {
        const string query = "table Items {" +
                             "  Size: string," +
                             "  Name: string" +
                             "};" +
                             "couple #test.whatever with table Items as Items; " +
                             "select Name from Items() where Size = 1000";

        dynamic item1 = new ExpandoObject();
        item1.Size = null;
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
                             "  Value: object," +
                             "  Name: string" +
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
                             "  Value: object," +
                             "  Name: string" +
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
                             "  Size: string," +
                             "  Name: string" +
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
                             "  Size: string," +
                             "  Name: string" +
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
                             "  Size: string," +
                             "  Name: string" +
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
                             "  Value: object," +
                             "  Name: string" +
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

}
