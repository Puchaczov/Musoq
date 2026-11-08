using System.Dynamic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public partial class AutomaticNumericTypeInferenceTests
{
    [TestMethod]
    public void WhenComparingStringColumnWithIntLiteral_UsingGreaterThan_ShouldAutoConvert()
    {
        const string query = "table Items {" +
                             "  Size: string," +
                             "  Name: string" +
                             "};" +
                             "couple #test.whatever with table Items as Items; " +
                             "select Name from Items() where Size > 1000";

        var vm = CreateAndRunVirtualMachine(query, CreateTestDataWithStringColumn());
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        var results = table.Select(row => row.ReadValues()[0]).Cast<string>().OrderBy(x => x).ToList();
        CollectionAssert.Contains(results, "Medium");
        CollectionAssert.Contains(results, "Large");
    }

    [TestMethod]
    public void WhenComparingStringColumnWithIntLiteral_UsingLessThan_ShouldAutoConvert()
    {
        const string query = "table Items {" +
                             "  Size: string," +
                             "  Name: string" +
                             "};" +
                             "couple #test.whatever with table Items as Items; " +
                             "select Name from Items() where Size < 1000";

        var vm = CreateAndRunVirtualMachine(query, CreateTestDataWithStringColumn());
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Small", table[0].ReadValues()[0]);
    }

    [TestMethod]
    public void WhenComparingStringColumnWithIntLiteral_UsingEquals_ShouldAutoConvert()
    {
        const string query = "table Items {" +
                             "  Size: string," +
                             "  Name: string" +
                             "};" +
                             "couple #test.whatever with table Items as Items; " +
                             "select Name from Items() where Size = 1500";

        var vm = CreateAndRunVirtualMachine(query, CreateTestDataWithStringColumn());
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Medium", table[0].ReadValues()[0]);
    }

    [TestMethod]
    public void WhenComparingStringColumnWithIntLiteral_UsingNotEquals_ShouldAutoConvert()
    {
        const string query = "table Items {" +
                             "  Size: string," +
                             "  Name: string" +
                             "};" +
                             "couple #test.whatever with table Items as Items; " +
                             "select Name from Items() where Size <> 1500";

        var vm = CreateAndRunVirtualMachine(query, CreateTestDataWithStringColumn());
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        var results = table.Select(row => row.ReadValues()[0]).Cast<string>().OrderBy(x => x).ToList();
        CollectionAssert.Contains(results, "Small");
        CollectionAssert.Contains(results, "Large");
    }

    [TestMethod]
    public void WhenComparingStringColumnWithIntLiteral_UsingGreaterOrEqual_ShouldAutoConvert()
    {
        const string query = "table Items {" +
                             "  Size: string," +
                             "  Name: string" +
                             "};" +
                             "couple #test.whatever with table Items as Items; " +
                             "select Name from Items() where Size >= 1500";

        var vm = CreateAndRunVirtualMachine(query, CreateTestDataWithStringColumn());
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        var results = table.Select(row => row.ReadValues()[0]).Cast<string>().OrderBy(x => x).ToList();
        CollectionAssert.Contains(results, "Medium");
        CollectionAssert.Contains(results, "Large");
    }

    [TestMethod]
    public void WhenComparingStringColumnWithIntLiteral_UsingLessOrEqual_ShouldAutoConvert()
    {
        const string query = "table Items {" +
                             "  Size: string," +
                             "  Name: string" +
                             "};" +
                             "couple #test.whatever with table Items as Items; " +
                             "select Name from Items() where Size <= 1500";

        var vm = CreateAndRunVirtualMachine(query, CreateTestDataWithStringColumn());
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        var results = table.Select(row => row.ReadValues()[0]).Cast<string>().OrderBy(x => x).ToList();
        CollectionAssert.Contains(results, "Small");
        CollectionAssert.Contains(results, "Medium");
    }



    [TestMethod]
    public void WhenComparingDeclaredNumericColumnWithIntLiteral_UsingGreaterThan_ShouldAutoConvert()
    {
        const string query = "table Items {" +
                             "  Value: double," +
                             "  Name: string" +
                             "};" +
                             "couple #test.whatever with table Items as Items; " +
                             "select Name from Items() where Value > 1000";

        var vm = CreateAndRunVirtualMachine(query, CreateTestDataWithDoubleValues());
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        var results = table.Select(row => row.ReadValues()[0]).Cast<string>().OrderBy(x => x).ToList();
        CollectionAssert.Contains(results, "Medium");
        CollectionAssert.Contains(results, "Large");
    }

    [TestMethod]
    public void WhenComparingDeclaredNumericColumnWithIntLiteral_UsingEquals_ShouldAutoConvert()
    {
        const string query = "table Items {" +
                             "  Value: double," +
                             "  Name: string" +
                             "};" +
                             "couple #test.whatever with table Items as Items; " +
                             "select Name from Items() where Value = 1500";

        var vm = CreateAndRunVirtualMachine(query, CreateTestDataWithDoubleValues());
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Medium", table[0].ReadValues()[0]);
    }

    [TestMethod]
    public void WhenComparingDeclaredNumericColumnWithZero_ShouldAutoConvert()
    {
        const string query = "table Items {" +
                             "  Count: double," +
                             "  Name: string" +
                             "};" +
                             "couple #test.whatever with table Items as Items; " +
                             "select Name from Items() where Count = 0";

        dynamic item1 = new ExpandoObject();
        item1.Count = (double)(0);
        item1.Name = "Zero";

        dynamic item2 = new ExpandoObject();
        item2.Count = (double)(5);
        item2.Name = "NonZero";

        var vm = CreateAndRunVirtualMachine(query, [item1, item2]);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Zero", table[0].ReadValues()[0]);
    }



    [TestMethod]
    public void WhenComparingDeclaredNumericColumnWithDecimalLiteral_ShouldAutoConvert()
    {
        const string query = "table Items {" +
                             "  Price: decimal," +
                             "  Name: string" +
                             "};" +
                             "couple #test.whatever with table Items as Items; " +
                             "select Name from Items() where Price < 19.99";

        var vm = CreateAndRunVirtualMachine(query, CreateTestDataWithDecimalPriceColumn());
        var table = vm.Run(TestContext.CancellationToken);


        Assert.AreEqual(0, table.Count);
    }

    [TestMethod]
    public void WhenComparingDeclaredNumericColumnWithDecimalLiteral_UsingEquals_ShouldAutoConvert()
    {
        const string query = "table Items {" +
                             "  Price: decimal," +
                             "  Name: string" +
                             "};" +
                             "couple #test.whatever with table Items as Items; " +
                             "select Name from Items() where Price = 30.00";

        var vm = CreateAndRunVirtualMachine(query, CreateTestDataWithDecimalPriceColumn());
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Item C", table[0].ReadValues()[0]);
    }



    [TestMethod]
    public void WhenComparingDecodedDoubleWithIntLiteral_WithPrecisionLoss_ShouldNotMatch()
    {
        const string query = "table Items {" +
                             "  Value: double," +
                             "  Name: string" +
                             "};" +
                             "couple #test.whatever with table Items as Items; " +
                             "select Name from Items() where Value = 100";

        dynamic item1 = new ExpandoObject();
        item1.Value = (double)(100.5);
        item1.Name = "Float";

        dynamic item2 = new ExpandoObject();
        item2.Value = (double)(100);
        item2.Name = "Int";

        var vm = CreateAndRunVirtualMachine(query, [item1, item2]);
        var table = vm.Run(TestContext.CancellationToken);


        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Int", table[0].ReadValues()[0]);
    }

    [TestMethod]
    public void WhenComparingDeclaredDecimalPriceWithDecimalLiteral_ShouldAllowConversion()
    {
        const string query = "table Items {" +
                             "  Price: decimal," +
                             "  Name: string" +
                             "};" +
                             "couple #test.whatever with table Items as Items; " +
                             "select Name from Items() where Price >= 19.99";

        var vm = CreateAndRunVirtualMachine(query, CreateTestDataWithDecimalPriceColumn());
        var table = vm.Run(TestContext.CancellationToken);


        Assert.AreEqual(3, table.Count);
    }



    [TestMethod]
    public void WhenComparingIntLiteralWithStringColumn_Reversed_ShouldAutoConvert()
    {
        const string query = "table Items {" +
                             "  Size: string," +
                             "  Name: string" +
                             "};" +
                             "couple #test.whatever with table Items as Items; " +
                             "select Name from Items() where 1000 < Size";

        var vm = CreateAndRunVirtualMachine(query, CreateTestDataWithStringColumn());
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        var results = table.Select(row => row.ReadValues()[0]).Cast<string>().OrderBy(x => x).ToList();
        CollectionAssert.Contains(results, "Medium");
        CollectionAssert.Contains(results, "Large");
    }

    [TestMethod]
    public void WhenComparingIntLiteralWithDeclaredNumericColumn_Reversed_ShouldAutoConvert()
    {
        const string query = "table Items {" +
                             "  Value: double," +
                             "  Name: string" +
                             "};" +
                             "couple #test.whatever with table Items as Items; " +
                             "select Name from Items() where 1500 = Value";

        var vm = CreateAndRunVirtualMachine(query, CreateTestDataWithDoubleValues());
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Medium", table[0].ReadValues()[0]);
    }


}
