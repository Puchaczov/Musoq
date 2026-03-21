using System.Dynamic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Unknown;

namespace Musoq.Evaluator.Tests;

/// <summary>
///     Tests derived from the TABLE/COUPLE Specification (musoq-table-couple-spec.md).
///     These use UnknownQueryTestsBase for dynamic schema testing.
/// </summary>
[TestClass]
public class SpecExplorationTableCoupleTests : UnknownQueryTestsBase
{
    public TestContext TestContext { get; set; }

    #region §3 TABLE Statement - Basic

    [TestMethod]
    public void Spec_TableCouple_BasicStringColumn()
    {
        const string query =
            "table DummyTable { Name: string };" +
            "couple #test.whatever with table DummyTable as SourceOfDummyRows;" +
            "select Name from SourceOfDummyRows()";

        dynamic item1 = new ExpandoObject();
        item1.Name = "Alice";

        dynamic item2 = new ExpandoObject();
        item2.Name = "Bob";

        var vm = CreateAndRunVirtualMachine(query, [item1, item2]);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);

        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "Alice"),
            "Row with 'Alice' not found");
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "Bob"),
            "Row with 'Bob' not found");
    }

    #endregion

    #region §5.1 Basic Pattern

    [TestMethod]
    public void Spec_TableCouple_MultipleColumns()
    {
        const string query =
            "table DataTable { Country: string, Population: decimal };" +
            "couple #test.whatever with table DataTable as Countries;" +
            "select Country, Population from Countries() where Population > 100";

        dynamic item1 = new ExpandoObject();
        item1.Country = "Poland";
        item1.Population = 38000000m;

        dynamic item2 = new ExpandoObject();
        item2.Country = "Vatican";
        item2.Population = 800m;

        dynamic item3 = new ExpandoObject();
        item3.Country = "Nauru";
        item3.Population = 10m;

        var vm = CreateAndRunVirtualMachine(query, [item1, item2, item3]);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count, "Should only include countries with Population > 100");
    }

    #endregion

    #region §5.4 With CTEs

    [TestMethod]
    public void Spec_TableCouple_WithCTE()
    {
        const string query =
            "table TypedRow { Id: int, Name: string };" +
            "couple #test.whatever with table TypedRow as TypedSource;" +
            "with FilteredData as (" +
            "    select Id, Name from TypedSource() where Id > 10" +
            ")" +
            "select * from FilteredData";

        dynamic item1 = new ExpandoObject();
        item1.Id = 5;
        item1.Name = "Low";

        dynamic item2 = new ExpandoObject();
        item2.Id = 15;
        item2.Name = "High";

        dynamic item3 = new ExpandoObject();
        item3.Id = 25;
        item3.Name = "Higher";

        var vm = CreateAndRunVirtualMachine(query, [item1, item2, item3]);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count, "Only items with Id > 10 should pass through CTE");
    }

    #endregion

    #region §6.3 Type Keywords Are Case-Insensitive

    [TestMethod]
    public void Spec_TableCouple_TypeKeywordsCaseInsensitive()
    {
        const string query =
            "table T { Col1: STRING, Col2: Int, Col3: DECIMAL };" +
            "couple #test.whatever with table T as Source;" +
            "select Col1, Col2, Col3 from Source()";

        dynamic item = new ExpandoObject();
        item.Col1 = "hello";
        item.Col2 = 42;
        item.Col3 = 3.14m;

        var vm = CreateAndRunVirtualMachine(query, [item]);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("hello", table[0].Values[0]);
    }

    #endregion

    #region §9.6 Nullable with Trailing Comma

    [TestMethod]
    public void Spec_TableCouple_TrailingComma()
    {
        const string query =
            "table NullableExample { Id: int?, Name: string, IsActive: bool?, };" +
            "couple #test.whatever with table NullableExample as Data;" +
            "select Id, Name, IsActive from Data()";

        dynamic item = new ExpandoObject();
        item.Id = 1;
        item.Name = "Test";
        item.IsActive = true;

        var vm = CreateAndRunVirtualMachine(query, [item]);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
    }

    #endregion

    #region §10.4 With Aggregations

    [TestMethod]
    public void Spec_TableCouple_WithAggregation()
    {
        const string query =
            "table Sales { Product: string, Amount: decimal };" +
            "couple #test.whatever with table Sales as SalesData;" +
            "select Product, Sum(Amount) from SalesData() group by Product";

        dynamic item1 = new ExpandoObject();
        item1.Product = "Widget";
        item1.Amount = 100m;

        dynamic item2 = new ExpandoObject();
        item2.Product = "Widget";
        item2.Amount = 200m;

        dynamic item3 = new ExpandoObject();
        item3.Product = "Gizmo";
        item3.Amount = 50m;

        var vm = CreateAndRunVirtualMachine(query, [item1, item2, item3]);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count, "Should have 2 groups: Widget and Gizmo");
    }

    #endregion

    #region §10.6 Statement Order Requirements

    [TestMethod]
    public void Spec_TableCouple_CorrectStatementOrder_TableBeforeCouple()
    {
        const string query =
            "table T1 { Col1: string };" +
            "table T2 { Col2: string };" +
            "couple #test.whatever with table T1 as Source1;" +
            "select Col1 from Source1()";

        dynamic item = new ExpandoObject();
        item.Col1 = "value";

        var vm = CreateAndRunVirtualMachine(query, [item]);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
    }

    #endregion

    #region §6.1 All Supported Type Keywords

    [TestMethod]
    public void Spec_TableCouple_IntType()
    {
        const string query =
            "table T { Value: int };" +
            "couple #test.whatever with table T as Source;" +
            "select Value from Source()";

        dynamic item = new ExpandoObject();
        item.Value = 42;

        var vm = CreateAndRunVirtualMachine(query, [item]);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(42, table[0].Values[0]);
    }

    [TestMethod]
    public void Spec_TableCouple_BoolType()
    {
        const string query =
            "table T { Active: bool };" +
            "couple #test.whatever with table T as Source;" +
            "select Active from Source()";

        dynamic item = new ExpandoObject();
        item.Active = true;

        var vm = CreateAndRunVirtualMachine(query, [item]);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.IsTrue((bool?)table[0].Values[0]);
    }

    [TestMethod]
    public void Spec_TableCouple_DecimalType()
    {
        const string query =
            "table T { Price: decimal };" +
            "couple #test.whatever with table T as Source;" +
            "select Price from Source()";

        dynamic item = new ExpandoObject();
        item.Price = 99.99m;

        var vm = CreateAndRunVirtualMachine(query, [item]);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(99.99m, table[0].Values[0]);
    }

    #endregion
}
