using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class PivotAggregateFilterSemanticsTests : BasicEntityTestBase
{
    [TestMethod]
    public void Pivot_WithCountFormsAndCustomArgumentlessAggregates_ShouldUseAggregateFilters()
    {
        const string query = """
                             pivot #A.Entities()
                             on Month in ('Jan' as Jan, 'Feb' as Feb)
                             using Count(*) as CountStar,
                                   Count() as CountNoArgs,
                                   CustomRowCount() as CustomNoArgs,
                                   CustomRowCount(*) as CustomStar
                             group by City
                             order by City
                             """;

        var table = CreateAndRunVirtualMachine(query, CreateCountSources()).Run();

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("City", typeof(string)),
            ("Jan_CountStar", typeof(long)),
            ("Jan_CountNoArgs", typeof(long)),
            ("Jan_CustomNoArgs", typeof(long)),
            ("Jan_CustomStar", typeof(long)),
            ("Feb_CountStar", typeof(long)),
            ("Feb_CountNoArgs", typeof(long)),
            ("Feb_CustomNoArgs", typeof(long)),
            ("Feb_CustomStar", typeof(long)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["LA", 1L, 1L, 1L, 1L, 0L, 0L, 0L, 0L],
            ["NY", 2L, 2L, 2L, 2L, 1L, 1L, 1L, 1L]);
    }

    [TestMethod]
    public void Pivot_WithExplicitMeasureFilter_ShouldCombineFilterWithPivotPredicate()
    {
        const string query = """
                             pivot #A.Entities()
                             on Month in ('Jan' as Jan, 'Feb' as Feb)
                             using Sum(Money) filter (where Money > 0) as PositiveSales
                             group by City
                             order by City
                             """;

        var table = CreateAndRunVirtualMachine(query, CreateFilteredMeasureSources()).Run();

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("City", typeof(string)),
            ("Jan", typeof(decimal?)),
            ("Feb", typeof(decimal?)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            new object?[] { "LA", null, 7m },
            new object?[] { "NY", 10m, null });
    }

    private static IDictionary<string, IEnumerable<BasicEntity>> CreateCountSources()
    {
        return CreateSingleSource(
            new BasicEntity { City = "NY", Month = "Jan" },
            new BasicEntity { City = "NY", Month = "Jan" },
            new BasicEntity { City = "NY", Month = "Feb" },
            new BasicEntity { City = "LA", Month = "Jan" });
    }

    private static IDictionary<string, IEnumerable<BasicEntity>> CreateFilteredMeasureSources()
    {
        return CreateSingleSource(
            new BasicEntity { City = "NY", Month = "Jan", Money = 10m },
            new BasicEntity { City = "NY", Month = "Jan", Money = -5m },
            new BasicEntity { City = "NY", Month = "Feb", Money = -2m },
            new BasicEntity { City = "LA", Month = "Jan", Money = -3m },
            new BasicEntity { City = "LA", Month = "Feb", Money = 7m });
    }
}
