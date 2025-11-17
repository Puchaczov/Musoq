using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Unknown;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class AutomaticNumericTypeInferenceRuntimeTypeDetectionTests : UnknownQueryTestsBase
{
    #region Runtime Type Detection for Object Columns

    [TestMethod]
    public void WhenObjectContainsByte_AndComparingToInt_ShouldWork()
    {
        const string query = "table Items {" +
                             "  Value 'System.Object'," +
                             "  Name 'System.String'" +
                             "};" +
                             "couple #test.whatever with table Items as Items; " +
                             "select Name from Items() where Value > 100";

        dynamic item1 = new ExpandoObject();
        item1.Value = (object)(byte)50;
        item1.Name = "Small";

        dynamic item2 = new ExpandoObject();
        item2.Value = (object)(byte)150;
        item2.Name = "Medium";

        dynamic item3 = new ExpandoObject();
        item3.Value = (object)(byte)200;
        item3.Name = "Large";

        var vm = CreateAndRunVirtualMachine(query, [item1, item2, item3]);
        var table = vm.Run();

        Assert.AreEqual(2, table.Count);
        var results = table.Select(row => row.Values[0]).Cast<string>().OrderBy(x => x).ToList();
        CollectionAssert.Contains(results, "Medium");
        CollectionAssert.Contains(results, "Large");
    }

    [TestMethod]
    public void WhenObjectContainsLong_AndComparingToInt_ShouldWork()
    {
        const string query = "table Items {" +
                             "  Value 'System.Object'," +
                             "  Name 'System.String'" +
                             "};" +
                             "couple #test.whatever with table Items as Items; " +
                             "select Name from Items() where Value > 1000";

        dynamic item1 = new ExpandoObject();
        item1.Value = (object)500L;
        item1.Name = "Small";

        dynamic item2 = new ExpandoObject();
        item2.Value = (object)1500L;
        item2.Name = "Medium";

        dynamic item3 = new ExpandoObject();
        item3.Value = (object)2500L;
        item3.Name = "Large";

        var vm = CreateAndRunVirtualMachine(query, [item1, item2, item3]);
        var table = vm.Run();

        Assert.AreEqual(2, table.Count);
        var results = table.Select(row => row.Values[0]).Cast<string>().OrderBy(x => x).ToList();
        CollectionAssert.Contains(results, "Medium");
        CollectionAssert.Contains(results, "Large");
    }

    [TestMethod]
    public void WhenObjectContainsDouble_AndComparingToDecimal_ShouldWork()
    {
        const string query = "table Items {" +
                             "  Price 'System.Object'," +
                             "  Name 'System.String'" +
                             "};" +
                             "couple #test.whatever with table Items as Items; " +
                             "select Name from Items() where Price > 25.00";

        dynamic item1 = new ExpandoObject();
        item1.Price = (object)19.99;
        item1.Name = "Item A";

        dynamic item2 = new ExpandoObject();
        item2.Price = (object)30.50;
        item2.Name = "Item B";

        dynamic item3 = new ExpandoObject();
        item3.Price = (object)45.75;
        item3.Name = "Item C";

        var vm = CreateAndRunVirtualMachine(query, [item1, item2, item3]);
        var table = vm.Run();

        Assert.AreEqual(2, table.Count);
        var results = table.Select(row => row.Values[0]).Cast<string>().OrderBy(x => x).ToList();
        CollectionAssert.Contains(results, "Item B");
        CollectionAssert.Contains(results, "Item C");
    }

    [TestMethod]
    public void WhenObjectContainsDecimal_AndComparingToDecimal_ShouldWork()
    {
        const string query = "table Items {" +
                             "  Price 'System.Object'," +
                             "  Name 'System.String'" +
                             "};" +
                             "couple #test.whatever with table Items as Items; " +
                             "select Name from Items() where Price = 30.50";

        dynamic item1 = new ExpandoObject();
        item1.Price = (object)19.99m;
        item1.Name = "Item A";

        dynamic item2 = new ExpandoObject();
        item2.Price = (object)30.50m;
        item2.Name = "Item B";

        dynamic item3 = new ExpandoObject();
        item3.Price = (object)45.75m;
        item3.Name = "Item C";

        var vm = CreateAndRunVirtualMachine(query, [item1, item2, item3]);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Item B", table[0].Values[0]);
    }

    [TestMethod]
    public void WhenObjectContainsMixedNumericTypes_ShouldHandleAll()
    {
        const string query = "table Items {" +
                             "  Value 'System.Object'," +
                             "  Name 'System.String'" +
                             "};" +
                             "couple #test.whatever with table Items as Items; " +
                             "select Name from Items() where Value > 100";

        dynamic item1 = new ExpandoObject();
        item1.Value = (object)(byte)50;
        item1.Name = "Byte50";

        dynamic item2 = new ExpandoObject();
        item2.Value = (object)150;
        item2.Name = "Int150";

        dynamic item3 = new ExpandoObject();
        item3.Value = (object)250L;
        item3.Name = "Long250";

        dynamic item4 = new ExpandoObject();
        item4.Value = (object)350.5;
        item4.Name = "Double350";

        dynamic item5 = new ExpandoObject();
        item5.Value = (object)450.75m;
        item5.Name = "Decimal450";

        var vm = CreateAndRunVirtualMachine(query, [item1, item2, item3, item4, item5]);
        var table = vm.Run();

        // All except Byte50 should match (Value > 100)
        Assert.AreEqual(4, table.Count);
        var results = table.Select(row => row.Values[0]).Cast<string>().OrderBy(x => x).ToList();
        CollectionAssert.Contains(results, "Int150");
        CollectionAssert.Contains(results, "Long250");
        CollectionAssert.Contains(results, "Double350");
        CollectionAssert.Contains(results, "Decimal450");
    }

    [TestMethod]
    public void WhenObjectContainsFloat_AndComparingToInt_ShouldWork()
    {
        const string query = "table Items {" +
                             "  Value 'System.Object'," +
                             "  Name 'System.String'" +
                             "};" +
                             "couple #test.whatever with table Items as Items; " +
                             "select Name from Items() where Value > 100";

        dynamic item1 = new ExpandoObject();
        item1.Value = (object)50.5f;
        item1.Name = "Small";

        dynamic item2 = new ExpandoObject();
        item2.Value = (object)150.7f;
        item2.Name = "Medium";

        dynamic item3 = new ExpandoObject();
        item3.Value = (object)250.9f;
        item3.Name = "Large";

        var vm = CreateAndRunVirtualMachine(query, [item1, item2, item3]);
        var table = vm.Run();

        Assert.AreEqual(2, table.Count);
        var results = table.Select(row => row.Values[0]).Cast<string>().OrderBy(x => x).ToList();
        CollectionAssert.Contains(results, "Medium");
        CollectionAssert.Contains(results, "Large");
    }

    [TestMethod]
    public void WhenObjectContainsShort_AndComparingToInt_ShouldWork()
    {
        const string query = "table Items {" +
                             "  Value 'System.Object'," +
                             "  Name 'System.String'" +
                             "};" +
                             "couple #test.whatever with table Items as Items; " +
                             "select Name from Items() where Value = 150";

        dynamic item1 = new ExpandoObject();
        item1.Value = (object)(short)50;
        item1.Name = "Small";

        dynamic item2 = new ExpandoObject();
        item2.Value = (object)(short)150;
        item2.Name = "Medium";

        dynamic item3 = new ExpandoObject();
        item3.Value = (object)(short)250;
        item3.Name = "Large";

        var vm = CreateAndRunVirtualMachine(query, [item1, item2, item3]);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Medium", table[0].Values[0]);
    }

    #endregion
}
