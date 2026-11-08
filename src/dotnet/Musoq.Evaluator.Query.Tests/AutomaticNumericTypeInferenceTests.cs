using System.Collections.Generic;
using System.Dynamic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Unknown;

namespace Musoq.Evaluator.Tests;

[TestClass]
public partial class AutomaticNumericTypeInferenceTests : UnknownQueryTestsBase
{

    [TestMethod]
    public void WhenComparingStringWithLongLiteral_ShouldPromoteToLong()
    {
        const string query = "table Items {" +
                             "  Size: string," +
                             "  Name: string" +
                             "};" +
                             "couple #test.whatever with table Items as Items; " +
                             "select Name from Items() where Size = 9223372036854775807l";

        dynamic item1 = new ExpandoObject();
        item1.Size = "9223372036854775807";
        item1.Name = "MaxLong";

        var vm = CreateAndRunVirtualMachine(query, [item1]);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("MaxLong", table[0].ReadValues()[0]);
    }



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

    private static List<dynamic> CreateTestDataWithDoubleValues()
    {
        dynamic item1 = new ExpandoObject();
        item1.Value = 500d;
        item1.Name = "Small";

        dynamic item2 = new ExpandoObject();
        item2.Value = 1500d;
        item2.Name = "Medium";

        dynamic item3 = new ExpandoObject();
        item3.Value = 2500d;
        item3.Name = "Large";

        return [item1, item2, item3];
    }

    private static List<dynamic> CreateTestDataWithDecimalPriceColumn()
    {
        dynamic item1 = new ExpandoObject();
        item1.Price = 19.99m;
        item1.Name = "Item A";

        dynamic item2 = new ExpandoObject();
        item2.Price = 25.50m;
        item2.Name = "Item B";

        dynamic item3 = new ExpandoObject();
        item3.Price = 30.00m;
        item3.Name = "Item C";

        return [item1, item2, item3];
    }




}
