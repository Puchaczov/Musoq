using System.Dynamic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Unknown;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class AutomaticNumericTypeInferenceDebugDecimalTest : UnknownQueryTestsBase
{
    public TestContext TestContext { get; set; }

    [TestMethod]
    public void Debug_StringToDecimal_SimpleCase()
    {
        const string query = "table Items {" +
                             "  Price 'System.String'," +
                             "  Name 'System.String'" +
                             "};" +
                             "couple #test.whatever with table Items as Items; " +
                             "select Name from Items() where Price = 100.50";

        dynamic item1 = new ExpandoObject();
        item1.Price = "100,50";
        item1.Name = "Match";

        var vm = CreateAndRunVirtualMachine(query, [item1]);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count, $"Expected 1 match but got {table.Count}");
        Assert.AreEqual("Match", table[0].Values[0]);
    }

    [TestMethod]
    public void Debug_StringToDecimal_IntegerWorks()
    {
        const string query = "table Items {" +
                             "  Price 'System.String'," +
                             "  Name 'System.String'" +
                             "};" +
                             "couple #test.whatever with table Items as Items; " +
                             "select Name from Items() where Price = 100";

        dynamic item1 = new ExpandoObject();
        item1.Price = "100";
        item1.Name = "Match";

        var vm = CreateAndRunVirtualMachine(query, [item1]);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Match", table[0].Values[0]);
    }

    [TestMethod]
    public void Debug_ObjectToDecimal_Works()
    {
        const string query = "table Items {" +
                             "  Price 'System.Object'," +
                             "  Name 'System.String'" +
                             "};" +
                             "couple #test.whatever with table Items as Items; " +
                             "select Name from Items() where Price = 100.50";

        dynamic item1 = new ExpandoObject();
        item1.Price = (object)100.50;
        item1.Name = "Match";

        var vm = CreateAndRunVirtualMachine(query, [item1]);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Match", table[0].Values[0]);
    }
}
