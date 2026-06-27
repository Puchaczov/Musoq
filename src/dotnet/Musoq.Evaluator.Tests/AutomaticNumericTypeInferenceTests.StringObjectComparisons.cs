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
        var results = table.Select(row => row.Values[0]).Cast<string>().OrderBy(x => x).ToList();
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
        Assert.AreEqual("Small", table[0].Values[0]);
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
        Assert.AreEqual("Medium", table[0].Values[0]);
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
        var results = table.Select(row => row.Values[0]).Cast<string>().OrderBy(x => x).ToList();
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
        var results = table.Select(row => row.Values[0]).Cast<string>().OrderBy(x => x).ToList();
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
        var results = table.Select(row => row.Values[0]).Cast<string>().OrderBy(x => x).ToList();
        CollectionAssert.Contains(results, "Small");
        CollectionAssert.Contains(results, "Medium");
    }



    [TestMethod]
    public void WhenComparingObjectColumnWithIntLiteral_UsingGreaterThan_ShouldAutoConvert()
    {
        const string query = "table Items {" +
                             "  Value: object," +
                             "  Name: string" +
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
                             "  Value: object," +
                             "  Name: string" +
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
                             "  Count: object," +
                             "  Name: string" +
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



    [TestMethod]
    public void WhenComparingObjectColumnWithDecimalLiteral_ShouldAutoConvert()
    {
        const string query = "table Items {" +
                             "  Price: object," +
                             "  Name: string" +
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
                             "  Price: object," +
                             "  Name: string" +
                             "};" +
                             "couple #test.whatever with table Items as Items; " +
                             "select Name from Items() where Price = 30.00";

        var vm = CreateAndRunVirtualMachine(query, CreateTestDataWithObjectFloatColumn());
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Item C", table[0].Values[0]);
    }



    [TestMethod]
    public void WhenComparingObjectFloatWithIntLiteral_WithPrecisionLoss_ShouldNotMatch()
    {
        const string query = "table Items {" +
                             "  Value: object," +
                             "  Name: string" +
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
                             "  Price: object," +
                             "  Name: string" +
                             "};" +
                             "couple #test.whatever with table Items as Items; " +
                             "select Name from Items() where Price >= 19.99";

        var vm = CreateAndRunVirtualMachine(query, CreateTestDataWithObjectFloatColumn());
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
        var results = table.Select(row => row.Values[0]).Cast<string>().OrderBy(x => x).ToList();
        CollectionAssert.Contains(results, "Medium");
        CollectionAssert.Contains(results, "Large");
    }

    [TestMethod]
    public void WhenComparingIntLiteralWithObjectColumn_Reversed_ShouldAutoConvert()
    {
        const string query = "table Items {" +
                             "  Value: object," +
                             "  Name: string" +
                             "};" +
                             "couple #test.whatever with table Items as Items; " +
                             "select Name from Items() where 1500 = Value";

        var vm = CreateAndRunVirtualMachine(query, CreateTestDataWithObjectColumn());
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Medium", table[0].Values[0]);
    }


}
