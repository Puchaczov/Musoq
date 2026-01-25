using System.Dynamic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Unknown;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class AutomaticNumericTypeInferenceCaseWhenTests : UnknownQueryTestsBase
{
    #region Phase 4: CASE WHEN Expressions

    [TestMethod]
    public void WhenUsingCaseWhenWithStringColumnInCondition_ShouldAutoConvert()
    {
        const string query = "table Items {" +
                             "  Size 'System.String'," +
                             "  Name 'System.String'" +
                             "};" +
                             "couple #test.whatever with table Items as Items; " +
                             "select case when Size > 1000 then 'Large' else 'Small' end as Category from Items()";

        dynamic item1 = new ExpandoObject();
        item1.Size = "1500";
        item1.Name = "Item1";

        dynamic item2 = new ExpandoObject();
        item2.Size = "500";
        item2.Name = "Item2";

        var vm = CreateAndRunVirtualMachine(query, [item1, item2]);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        var results = table.Select(row => (string)row.Values[0]).ToList();
        CollectionAssert.Contains(results, "Large");
        CollectionAssert.Contains(results, "Small");
    }

    [TestMethod]
    public void WhenUsingMultipleCaseWhenWithStringColumns_ShouldAutoConvert()
    {
        const string query = "table Items {" +
                             "  Size 'System.String'," +
                             "  Name 'System.String'" +
                             "};" +
                             "couple #test.whatever with table Items as Items; " +
                             "select case " +
                             "  when Size < 1000 then 'Small' " +
                             "  when Size < 2000 then 'Medium' " +
                             "  else 'Large' " +
                             "end as Category from Items()";

        dynamic item1 = new ExpandoObject();
        item1.Size = "500";
        item1.Name = "Item1";

        dynamic item2 = new ExpandoObject();
        item2.Size = "1500";
        item2.Name = "Item2";

        dynamic item3 = new ExpandoObject();
        item3.Size = "2500";
        item3.Name = "Item3";

        var vm = CreateAndRunVirtualMachine(query, [item1, item2, item3]);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);

        var results = table.Select(row => row.Values[0]).Cast<string>().ToList();
        CollectionAssert.Contains(results, "Small");
        CollectionAssert.Contains(results, "Medium");
        CollectionAssert.Contains(results, "Large");
    }

    [TestMethod]
    public void WhenUsingCaseWhenWithBetweenSimulation_ShouldAutoConvert()
    {
        const string query = "table Items {" +
                             "  Size 'System.String'," +
                             "  Name 'System.String'" +
                             "};" +
                             "couple #test.whatever with table Items as Items; " +
                             "select case " +
                             "  when Size >= 1000 and Size <= 2000 then 'InRange' " +
                             "  else 'OutOfRange' " +
                             "end as RangeCheck from Items()";

        dynamic item1 = new ExpandoObject();
        item1.Size = "1500";
        item1.Name = "Item1";

        dynamic item2 = new ExpandoObject();
        item2.Size = "500";
        item2.Name = "Item2";

        var vm = CreateAndRunVirtualMachine(query, [item1, item2]);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        var results = table.Select(row => (string)row.Values[0]).ToList();
        CollectionAssert.Contains(results, "InRange");
        CollectionAssert.Contains(results, "OutOfRange");
    }

    [TestMethod]
    public void WhenUsingCaseWhenWithMultipleStringColumnsInConditions_ShouldAutoConvert()
    {
        const string query = "table Items {" +
                             "  Size 'System.String'," +
                             "  Price 'System.String'," +
                             "  Name 'System.String'" +
                             "};" +
                             "couple #test.whatever with table Items as Items; " +
                             "select case " +
                             "  when Size > 1000 and Price < 50 then 'LargeAndCheap' " +
                             "  when Size > 1000 then 'LargeAndExpensive' " +
                             "  else 'Small' " +
                             "end as Classification from Items()";

        dynamic item1 = new ExpandoObject();
        item1.Size = "1500";
        item1.Price = "30";
        item1.Name = "Item1";

        dynamic item2 = new ExpandoObject();
        item2.Size = "1500";
        item2.Price = "70";
        item2.Name = "Item2";

        dynamic item3 = new ExpandoObject();
        item3.Size = "500";
        item3.Price = "30";
        item3.Name = "Item3";

        var vm = CreateAndRunVirtualMachine(query, [item1, item2, item3]);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);
        var results = table.Select(row => (string)row.Values[0]).ToList();
        CollectionAssert.Contains(results, "LargeAndCheap");
        CollectionAssert.Contains(results, "LargeAndExpensive");
        CollectionAssert.Contains(results, "Small");
    }

    [TestMethod]
    public void WhenUsingCaseWhenInWhereClause_ShouldAutoConvert()
    {
        const string query = "table Items {" +
                             "  Size 'System.String'," +
                             "  Name 'System.String'" +
                             "};" +
                             "couple #test.whatever with table Items as Items; " +
                             "select Name from Items() " +
                             "where case when Size > 1000 then 1 else 0 end = 1";

        dynamic item1 = new ExpandoObject();
        item1.Size = "1500";
        item1.Name = "Large";

        dynamic item2 = new ExpandoObject();
        item2.Size = "500";
        item2.Name = "Small";

        var vm = CreateAndRunVirtualMachine(query, [item1, item2]);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Large", table[0].Values[0]);
    }

    [TestMethod]
    public void WhenUsingNestedCaseWhenWithStringColumn_ShouldAutoConvert()
    {
        const string query = "table Items {" +
                             "  Size 'System.String'," +
                             "  Name 'System.String'" +
                             "};" +
                             "couple #test.whatever with table Items as Items; " +
                             "select case " +
                             "  when Size > 2000 then 'VeryLarge' " +
                             "  else case " +
                             "    when Size > 1000 then 'Large' " +
                             "    else 'Small' " +
                             "  end " +
                             "end as Category from Items()";

        dynamic item1 = new ExpandoObject();
        item1.Size = "2500";
        item1.Name = "Item1";

        dynamic item2 = new ExpandoObject();
        item2.Size = "1500";
        item2.Name = "Item2";

        dynamic item3 = new ExpandoObject();
        item3.Size = "500";
        item3.Name = "Item3";

        var vm = CreateAndRunVirtualMachine(query, [item1, item2, item3]);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);
        var results = table.Select(row => (string)row.Values[0]).ToList();
        CollectionAssert.Contains(results, "VeryLarge");
        CollectionAssert.Contains(results, "Large");
        CollectionAssert.Contains(results, "Small");
    }

    public TestContext TestContext { get; set; }

    #endregion
}
