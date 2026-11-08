using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tables;
using Musoq.Evaluator.Tests.Schema.Generic;
using Musoq.Schema;
using Musoq.TypedLibraryExamples;

using static Musoq.Evaluator.Tests.LibraryExecutionDocumentationQueryTestsFixtures;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class LibraryExecutionDocumentationQueryTests : GenericEntityTestBase
{
    [TestMethod]
    public void AuthoringExample_WhenGrouped_ShouldExecuteVerbatimWithIndependentCustomState()
    {
        var query = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Documentation", "grouped.sql"));
        using var compiled = CreateAndRunVirtualMachine<Invoice, InvoiceLibrary>(query, Rows());
        using var table = compiled.Run(TestContext.CancellationToken);
        var rows = table.ToArray();
        TableMaterializationTestHelper.AssertColumns(table, ("Customer", typeof(string)), ("Total", typeof(decimal?)),
            ("Positive", typeof(long)), ("AverageQuantity", typeof(decimal?)));
        Assert.HasCount(2, rows);
        Assert.AreEqual("A", rows[0].GetValue<string>(0));
        Assert.AreEqual(3.75m, rows[0].GetValue<decimal?>(1));
        Assert.AreEqual(2L, rows[0].GetValue<long>(2));
        Assert.AreEqual(1.5m, rows[0].GetValue<decimal?>(3));
        Assert.AreEqual("B", rows[1].GetValue<string>(0));
        Assert.AreEqual(-1m, rows[1].GetValue<decimal?>(1));
        Assert.AreEqual(0L, rows[1].GetValue<long>(2));
        Assert.AreEqual(4m, rows[1].GetValue<decimal?>(3));

        var visitor = VisitGroupedTotals(rows);
        Assert.AreEqual(2.75m, visitor.Total);
        Assert.AreEqual(2, visitor.Values);
    }

    [TestMethod]
    public void AuthoringExample_WhenWindowed_ShouldExecuteVerbatimAndResetEachPartition()
    {
        var query = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Documentation", "windowed.sql"));
        using var compiled = CreateAndRunVirtualMachine<Invoice, InvoiceLibrary>(query, Rows());
        using var table = compiled.Run(TestContext.CancellationToken);
        var rows = table.ToArray();
        TableMaterializationTestHelper.AssertColumns(table, ("Position", typeof(int)), ("RunningTotal", typeof(decimal?)));
        Assert.HasCount(4, rows);
        CollectionAssert.AreEqual(new[] { 1, 2, 3, 4 },
            rows.Select(row => row.GetValue<int>(0)).ToArray());
        CollectionAssert.AreEqual(new decimal?[] { 1.25m, 3.75m, 3.75m, -1m },
            rows.Select(row => row.GetValue<decimal?>(1)).ToArray());
    }
}
