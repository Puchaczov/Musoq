using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Unknown;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class AutomaticNumericTypeInference_AggregateTests : UnknownQueryTestsBase
{
    #region Phase 3: Aggregate Functions and GROUP BY

    [TestMethod]
    public void WhenUsingHavingWithStringColumnComparison_ShouldAutoConvert()
    {
        const string query = "table Items {" +
                             "  Category 'System.String'," +
                             "  Size 'System.String'" +
                             "};" +
                             "couple #test.whatever with table Items as Items; " +
                             "select Category, Count(Category) from Items() group by Category having Count(Category) > 1";

        dynamic item1 = new ExpandoObject();
        item1.Category = "A";
        item1.Size = "100";

        dynamic item2 = new ExpandoObject();
        item2.Category = "A";
        item2.Size = "200";

        dynamic item3 = new ExpandoObject();
        item3.Category = "B";
        item3.Size = "300";

        var vm = CreateAndRunVirtualMachine(query, new List<dynamic> { item1, item2, item3 });
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("A", table[0].Values[0]);
    }

    [TestMethod]
    public void WhenFilteringBeforeGroupByWithStringColumn_ShouldAutoConvert()
    {
        const string query = "table Items {" +
                             "  Category 'System.String'," +
                             "  Size 'System.String'" +
                             "};" +
                             "couple #test.whatever with table Items as Items; " +
                             "select Category, Sum(ToInt32(Size)) from Items() where Size > 100 group by Category";

        dynamic item1 = new ExpandoObject();
        item1.Category = "A";
        item1.Size = "50";

        dynamic item2 = new ExpandoObject();
        item2.Category = "A";
        item2.Size = "200";

        dynamic item3 = new ExpandoObject();
        item3.Category = "B";
        item3.Size = "300";

        var vm = CreateAndRunVirtualMachine(query, new List<dynamic> { item1, item2, item3 });
        var table = vm.Run();

        Assert.AreEqual(2, table.Count);
    }

    [TestMethod]
    public void WhenComparingStringColumnInWhereBeforeAggregate_ShouldAutoConvert()
    {
        const string query = "table Items {" +
                             "  Category 'System.String'," +
                             "  Size 'System.String'," +
                             "  Price 'System.String'" +
                             "};" +
                             "couple #test.whatever with table Items as Items; " +
                             "select Category from Items() where Size >= 1000 and Price <= 100 group by Category";

        dynamic item1 = new ExpandoObject();
        item1.Category = "A";
        item1.Size = "1500";
        item1.Price = "50";

        dynamic item2 = new ExpandoObject();
        item2.Category = "B";
        item2.Size = "500";
        item2.Price = "50";

        dynamic item3 = new ExpandoObject();
        item3.Category = "C";
        item3.Size = "1500";
        item3.Price = "150";

        var vm = CreateAndRunVirtualMachine(query, new List<dynamic> { item1, item2, item3 });
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("A", table[0].Values[0]);
    }

    [TestMethod]
    public void WhenUsingCountWithStringColumnFilter_ShouldAutoConvert()
    {
        const string query = "table Items {" +
                             "  Size 'System.String'," +
                             "  Name 'System.String'" +
                             "};" +
                             "couple #test.whatever with table Items as Items; " +
                             "select Count(Name) from Items() where Size > 1000";

        dynamic item1 = new ExpandoObject();
        item1.Size = "500";
        item1.Name = "Small";

        dynamic item2 = new ExpandoObject();
        item2.Size = "1500";
        item2.Name = "Large1";

        dynamic item3 = new ExpandoObject();
        item3.Size = "2000";
        item3.Name = "Large2";

        var vm = CreateAndRunVirtualMachine(query, new List<dynamic> { item1, item2, item3 });
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(2, table[0].Values[0]);
    }

    [TestMethod]
    public void WhenCombiningMultipleStringColumnFiltersWithGroupBy_ShouldAutoConvert()
    {
        const string query = "table Items {" +
                             "  Category 'System.String'," +
                             "  Size 'System.String'," +
                             "  Price 'System.String'," +
                             "  Quantity 'System.String'" +
                             "};" +
                             "couple #test.whatever with table Items as Items; " +
                             "select Category from Items() " +
                             "where Size > 1000 and Price < 100 and Quantity >= 5 " +
                             "group by Category";

        dynamic item1 = new ExpandoObject();
        item1.Category = "A";
        item1.Size = "1500";
        item1.Price = "50";
        item1.Quantity = "10";

        dynamic item2 = new ExpandoObject();
        item2.Category = "B";
        item2.Size = "500";
        item2.Price = "50";
        item2.Quantity = "10";

        var vm = CreateAndRunVirtualMachine(query, new List<dynamic> { item1, item2 });
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("A", table[0].Values[0]);
    }

    #endregion
}
