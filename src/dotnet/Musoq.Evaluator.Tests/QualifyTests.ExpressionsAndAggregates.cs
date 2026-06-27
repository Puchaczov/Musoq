using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class QualifyTests
{
    [TestMethod]
    public void WhenQualifyWithCountWindow_ShouldFilterOnPartitionCount()
    {
        var query = @"
            select Name, City, Count(Name) over (partition by City) as CityCount
            from #A.Entities()
            qualify Count(Name) over (partition by City) >= 2";

        var sources = CreateSingleSource(
            new BasicEntity("Alice") { City = "NYC" },
            new BasicEntity("Bob") { City = "LA" },
            new BasicEntity("Charlie") { City = "NYC" },
            new BasicEntity("Diana") { City = "SF" });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        Assert.IsTrue(table.All(r => (string)r.Values[1] == "NYC"));
    }

    [TestMethod]
    public void WhenQualifyWithAvgWindow_ShouldFilterOnRunningAvg()
    {
        var query = @"
            select Name, Population, Avg(Population) over (order by Name) as RunAvg
            from #A.Entities()
            qualify Avg(Population) over (order by Name) > 200";

        var sources = CreateSingleSource(
            new BasicEntity("Alice") { Population = 100m },
            new BasicEntity("Bob") { Population = 500m },
            new BasicEntity("Charlie") { Population = 300m });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsGreaterThan(0, table.Count);
        Assert.IsTrue(table.All(r => (decimal)r.Values[2] > 200m));
    }

    [TestMethod]
    public void WhenQualifyWithCaseWhen_ShouldFilterOnCaseResult()
    {
        var query = @"
            select Name, City,
                   RowNumber() over (partition by City order by Name) as rn,
                   case when RowNumber() over (partition by City order by Name) = 1 then 'first' else 'other' end as Label
            from #A.Entities()
            qualify RowNumber() over (partition by City order by Name) = 1";

        var sources = CreateSingleSource(
            new BasicEntity("Alice") { City = "NYC" },
            new BasicEntity("Bob") { City = "NYC" },
            new BasicEntity("Charlie") { City = "LA" },
            new BasicEntity("Diana") { City = "LA" });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        Assert.IsTrue(table.All(r => (long)r.Values[2] == 1L));
    }

    [TestMethod]
    public void WhenQualifyWithLike_ShouldCombineStringFilterAndWindowFilter()
    {
        var query = @"
            select Name, City, RowNumber() over (order by Name) as rn
            from #A.Entities()
            where Name like '%li%'
            qualify RowNumber() over (order by Name) <= 1";

        var sources = CreateSingleSource(
            new BasicEntity("Alice") { City = "NYC" },
            new BasicEntity("Bob") { City = "LA" },
            new BasicEntity("Charlie") { City = "SF" },
            new BasicEntity("Julia") { City = "NYC" });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Alice", table[0].Values[0]);
    }

    [TestMethod]
    public void WhenQualifyWithIn_ShouldCombineInAndWindowFilter()
    {
        var query = @"
            select Name, City, RowNumber() over (partition by City order by Name) as rn
            from #A.Entities()
            where City in ('NYC', 'LA')
            qualify RowNumber() over (partition by City order by Name) = 1";

        var sources = CreateSingleSource(
            new BasicEntity("Alice") { City = "NYC" },
            new BasicEntity("Bob") { City = "LA" },
            new BasicEntity("Charlie") { City = "NYC" },
            new BasicEntity("Diana") { City = "SF" },
            new BasicEntity("Eve") { City = "LA" });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        Assert.IsTrue(table.All(r => (long)r.Values[2] == 1L));
    }

    [TestMethod]
    public void WhenQualifyWithGroupByViaCte_ShouldFilterAggregatedWindowed()
    {
        var query = @"
            with grouped as (
                select City, Count(City) as CityCount, Sum(Population) as TotalPop
                from #A.Entities()
                group by City
            )
            select City, CityCount, TotalPop,
                   RowNumber() over (order by TotalPop desc) as PopRank
            from grouped
            qualify RowNumber() over (order by TotalPop desc) <= 2";

        var sources = CreateSingleSource(
            new BasicEntity("Alice") { City = "NYC", Population = 100m },
            new BasicEntity("Bob") { City = "LA", Population = 200m },
            new BasicEntity("Charlie") { City = "NYC", Population = 300m },
            new BasicEntity("Diana") { City = "SF", Population = 50m });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        Assert.IsTrue(table.All(r => (long)r.Values[3] <= 2L));
    }

}
