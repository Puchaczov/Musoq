using System.Dynamic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public partial class AutomaticNumericTypeInferenceTests
{

    [TestMethod]
    public void WhenUsingStringColumnComparisonInCaseWhen_ShouldAutoConvert()
    {
        const string query = "table Items {" +
                             "  Size: string," +
                             "  Name: string" +
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
                             "  Value: object," +
                             "  Name: string" +
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



    [TestMethod]
    public void WhenStringColumnContainsInvalidNumber_ShouldHandleGracefully()
    {
        const string query = "table Items {" +
                             "  Size: string," +
                             "  Name: string" +
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
                             "  Value: object," +
                             "  Name: string" +
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
                             "  Size: string," +
                             "  Name: string" +
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
                             "  Size: string," +
                             "  Name: string" +
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



    [TestMethod]
    public void WhenComparingStringColumnWithInvalidNumericString_ShouldNotMatch()
    {
        const string query = "table Items {" +
                             "  Size: string," +
                             "  Name: string" +
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
                             "  Value: object," +
                             "  Name: string" +
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
                             "  Value: object," +
                             "  Name: string" +
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

}
