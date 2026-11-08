using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Unknown;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class CoupleProviderIndependentExamplesTests : UnknownQueryTestsBase
{
    [TestMethod]
    public void GenericProducts_ShouldUseThePublishedTableDefinedReads()
    {
        const string query = "table Items { Name: string, Price: decimal }; " +
            "couple schema.products with table Items as Products; select Name, Price from Products();";
        var table = CreateAndRunVirtualMachine(query,
            [new Dictionary<string, object?> { ["Name"] = "Book", ["Price"] = 12.50m },
             new Dictionary<string, object?> { ["Name"] = "Pen", ["Price"] = 2.00m }])
            .Run(TestContext.CancellationToken);
        _ = table.Count;

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)), ("Price", typeof(decimal?)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["Book", 12.50m], ["Pen", 2.00m]);
    }

    [TestMethod]
    public void GenericRows_ShouldKeepTheCteDeclarationsAndExportedNamesLocal()
    {
        const string query = "table TypedRow { Id: int, Name: string }; " +
            "couple schema.rows with table TypedRow as TypedSource; " +
            "with FilteredData as (select t.Id, t.Name from TypedSource() t where t.Id > 10) " +
            "select Id, Name from FilteredData;";
        var table = CreateAndRunVirtualMachine(query,
            [new Dictionary<string, object?> { ["Id"] = 12, ["Name"] = "Poland" },
             new Dictionary<string, object?> { ["Id"] = 2, ["Name"] = "Germany" }])
            .Run(TestContext.CancellationToken);
        _ = table.Count;

        TableMaterializationTestHelper.AssertColumns(table, ("Id", typeof(int?)), ("Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [12, "Poland"]);
    }

    [TestMethod]
    public void GenericNullableRows_ShouldKeepExplicitNullsAndTrailingCommaSyntax()
    {
        const string query = "table NullableExample { Id: int?, Name: string, IsActive: bool?, }; " +
            "couple schema.nullableRows with table NullableExample as Data; select Id, Name, IsActive from Data();";
        var table = CreateAndRunVirtualMachine(query,
            [new Dictionary<string, object?> { ["Id"] = null, ["Name"] = "Ada", ["IsActive"] = null },
             new Dictionary<string, object?> { ["Id"] = 1, ["Name"] = "Bob", ["IsActive"] = true }])
            .Run(TestContext.CancellationToken);
        _ = table.Count;

        TableMaterializationTestHelper.AssertColumns(table,
            ("Id", typeof(int?)), ("Name", typeof(string)), ("IsActive", typeof(bool?)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, [null, "Ada", null], [1, "Bob", true]);
    }
}
