using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Schema.Generic;
using Musoq.Evaluator.Tests.Schema.LibraryExecution;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed partial class LibraryExecutionProviderQueryTests : GenericEntityTestBase
{
    [TestMethod]
    public void QueryScopedProvider_WhenAggregated_ShouldKeepDecimalResultsAndLongCounts()
    {
        const string query = "select Sum(Amount) as Total, Avg(Amount) as Average, Count(Amount) as Present from #typedrows.rows()";
        using var compiled = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            new LibraryExecutionQueryRowProvider(), LoggerResolver);
        var table = compiled.Run(TestContext.CancellationToken);
        var rows = table.ToArray();
        TableMaterializationTestHelper.AssertColumns(table,
            ("Total", typeof(decimal?)), ("Average", typeof(decimal?)), ("Present", typeof(long)));
        Assert.HasCount(1, rows);
        Assert.AreEqual(3.75m, rows[0].GetValue<decimal?>(0));
        Assert.AreEqual(1.875m, rows[0].GetValue<decimal?>(1));
        Assert.AreEqual(2L, rows[0].GetValue<long>(2));
    }

    [TestMethod]
    public void QueryScopedProvider_WhenWindowedAndJoined_ShouldPreserveTypedValuesAcrossMaterialization()
    {
        const string query = "select a.Id, Sum(a.Amount) over (order by a.Id) as Total, " +
                             "RowNumber() over (order by a.Id) as Number from #typedrows.rows() a " +
                             "inner join #typedrows.rows() b on a.Id = b.Id order by a.Id";
        using var compiled = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            new LibraryExecutionQueryRowProvider(), LoggerResolver);
        var table = compiled.Run(TestContext.CancellationToken);
        var rows = table.ToArray();
        TableMaterializationTestHelper.AssertColumns(table,
            ("Id", typeof(int)), ("Total", typeof(decimal?)), ("Number", typeof(long)));
        Assert.HasCount(3, rows);
        CollectionAssert.AreEqual(new[] { (1, (decimal?)1.25m, 1L), (2, (decimal?)3.75m, 2L), (3, (decimal?)3.75m, 3L) },
            rows.Select(row => (row.GetValue<int>(0), row.GetValue<decimal?>(1), row.GetValue<long>(2))).ToArray());
    }
}
