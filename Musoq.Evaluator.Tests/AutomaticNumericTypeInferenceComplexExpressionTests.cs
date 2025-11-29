using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Unknown;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class AutomaticNumericTypeInferenceComplexExpressionTests : UnknownQueryTestsBase
{
    #region Phase 2: Complex Boolean Expressions

    [TestMethod]
    public void WhenComparingStringColumnInComplexBooleanExpression_ShouldAutoConvert()
    {
        const string query = "table Items {" +
                             "  Size 'System.String'," +
                             "  Price 'System.String'," +
                             "  Name 'System.String'" +
                             "};" +
                             "couple #test.whatever with table Items as Items; " +
                             "select Name from Items() where Size > 1000 and Price < 50";

        dynamic item1 = new ExpandoObject();
        item1.Size = "1500";
        item1.Price = "30";
        item1.Name = "Match";

        dynamic item2 = new ExpandoObject();
        item2.Size = "500";
        item2.Price = "30";
        item2.Name = "SizeTooSmall";

        dynamic item3 = new ExpandoObject();
        item3.Size = "1500";
        item3.Price = "70";
        item3.Name = "PriceTooHigh";

        var vm = CreateAndRunVirtualMachine(query, [item1, item2, item3]);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Match", table[0].Values[0]);
    }

    [TestMethod]
    public void WhenComparingStringColumnWithOrExpression_ShouldAutoConvert()
    {
        const string query = "table Items {" +
                             "  Size 'System.String'," +
                             "  Name 'System.String'" +
                             "};" +
                             "couple #test.whatever with table Items as Items; " +
                             "select Name from Items() where Size < 500 or Size > 2000";

        dynamic item1 = new ExpandoObject();
        item1.Size = "300";
        item1.Name = "Small";

        dynamic item2 = new ExpandoObject();
        item2.Size = "1500";
        item2.Name = "Medium";

        dynamic item3 = new ExpandoObject();
        item3.Size = "2500";
        item3.Name = "Large";

        var vm = CreateAndRunVirtualMachine(query, [item1, item2, item3]);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        var results = table.Select(row => row.Values[0]).Cast<string>().ToList();
        CollectionAssert.Contains(results, "Large");
        CollectionAssert.Contains(results, "Small");
    }

    [TestMethod]
    public void WhenComparingStringColumnInBetweenSimulation_ShouldAutoConvert()
    {
        const string query = "table Items {" +
                             "  Size 'System.String'," +
                             "  Name 'System.String'" +
                             "};" +
                             "couple #test.whatever with table Items as Items; " +
                             "select Name from Items() where Size >= 1000 and Size <= 2000";

        dynamic item1 = new ExpandoObject();
        item1.Size = "500";
        item1.Name = "TooSmall";

        dynamic item2 = new ExpandoObject();
        item2.Size = "1500";
        item2.Name = "InRange";

        dynamic item3 = new ExpandoObject();
        item3.Size = "2500";
        item3.Name = "TooLarge";

        var vm = CreateAndRunVirtualMachine(query, [item1, item2, item3]);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("InRange", table[0].Values[0]);
    }

    [TestMethod]
    public void WhenComparingMultipleStringColumnsWithDifferentTypes_ShouldAutoConvert()
    {
        const string query = "table Items {" +
                             "  IntValue 'System.String'," +
                             "  LongValue 'System.String'," +
                             "  Name 'System.String'" +
                             "};" +
                             "couple #test.whatever with table Items as Items; " +
                             "select Name from Items() where IntValue = 1000 and LongValue = 9223372036854775807l";

        dynamic item1 = new ExpandoObject();
        item1.IntValue = "1000";
        item1.LongValue = "9223372036854775807";
        item1.Name = "Match";

        dynamic item2 = new ExpandoObject();
        item2.IntValue = "500";
        item2.LongValue = "9223372036854775807";
        item2.Name = "NoMatch";

        var vm = CreateAndRunVirtualMachine(query, [item1, item2]);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Match", table[0].Values[0]);
    }

    public TestContext TestContext { get; set; }

    #endregion
}
