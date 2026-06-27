using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tables;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class CteSidecarIndexExecutionTests : BasicEntityTestBase
{
    private static readonly CompilationOptions BaselineOptions = new(
        parallelizationMode: ParallelizationMode.None,
        useHashJoin: true,
        useSortMergeJoin: false,
        usePrimitiveTypeValidation: false);

    private static readonly CompilationOptions SidecarOptions = new(
        parallelizationMode: ParallelizationMode.None,
        useHashJoin: true,
        useSortMergeJoin: false,
        usePrimitiveTypeValidation: false,
        useCteSidecarIndexes: true);

    private static readonly CompilationOptions ParallelSidecarOptions = new(
        parallelizationMode: ParallelizationMode.Full,
        useHashJoin: true,
        useSortMergeJoin: false,
        usePrimitiveTypeValidation: false,
        useCteParallelization: true,
        useCteSidecarIndexes: true);

    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    public void InnerCteHashJoin_WithDuplicates_ShouldMatchBaseline()
    {
        const string query = @"
with rightCte as (
    select b.Id as Id, b.Name as Name
    from #B.entities() b
)
select a.Name, r.Name
from #A.entities() a
inner join rightCte r on a.Id = r.Id";

        var sources = CreateJoinSources();

        AssertSameRows(query, sources);
    }

    [TestMethod]
    public void LeftOuterCteHashJoin_ShouldMatchBaseline()
    {
        const string query = @"
with rightCte as (
    select b.Id as Id, b.Name as Name
    from #B.entities() b
)
select a.Name, r.Name
from #A.entities() a
left outer join rightCte r on a.Id = r.Id";

        var sources = CreateJoinSources();

        AssertSameRows(query, sources);
    }

    [TestMethod]
    public void RightOuterCteHashJoin_WhenCteIsPhysicalBuildSide_ShouldMatchBaseline()
    {
        const string query = @"
with leftCte as (
    select a.Id as Id, a.Name as Name
    from #A.entities() a
)
select l.Name, b.Name
from leftCte l
right outer join #B.entities() b on l.Id = b.Id";

        var sources = CreateJoinSources();

        AssertSameRows(query, sources);
    }

    [TestMethod]
    public void RepeatedCteSelfJoin_WithDuplicates_ShouldMatchBaseline()
    {
        const string query = @"
with indexed as (
    select a.Id as Id, a.Name as Name
    from #A.entities() a
)
select l.Name, r.Name
from indexed l
inner join indexed r on l.Id = r.Id";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("A1") { Id = 1 },
                    new BasicEntity("A1Duplicate") { Id = 1 },
                    new BasicEntity("A2") { Id = 2 }
                ]
            }
        };

        AssertSameRows(query, sources);
    }

    [TestMethod]
    public void CompositeCteHashJoin_ShouldMatchBaseline()
    {
        const string query = @"
with rightCte as (
    select b.Id as Id, b.Country as Country, b.Name as Name
    from #B.entities() b
)
select a.Name, r.Name
from #A.entities() a
inner join rightCte r on a.Id = r.Id and a.Country = r.Country";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "A1", Id = 1, Country = "PL" },
                    new BasicEntity { Name = "A2", Id = 1, Country = "DE" },
                    new BasicEntity { Name = "A3", Id = 2, Country = "PL" }
                ]
            },
            {
                "#B", [
                    new BasicEntity { Name = "B1", Id = 1, Country = "PL" },
                    new BasicEntity { Name = "B2", Id = 1, Country = "FR" },
                    new BasicEntity { Name = "B3", Id = 2, Country = "PL" }
                ]
            }
        };

        AssertSameRows(query, sources);
    }

    [TestMethod]
    public void SameCteConsumedOnDifferentKeys_ShouldMatchBaseline()
    {
        const string query = @"
with indexed as (
    select b.Id as Id, b.Country as Country, b.Name as Name
    from #B.entities() b
)
select a.Name, r.Name, s.Name
from #A.entities() a
inner join indexed r on a.Id = r.Id
inner join indexed s on a.Country = s.Country";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "A1", Id = 1, Country = "PL" },
                    new BasicEntity { Name = "A2", Id = 2, Country = "DE" }
                ]
            },
            {
                "#B", [
                    new BasicEntity { Name = "B1", Id = 1, Country = "PL" },
                    new BasicEntity { Name = "B2", Id = 2, Country = "FR" },
                    new BasicEntity { Name = "B3", Id = 3, Country = "DE" }
                ]
            }
        };

        AssertSameRows(query, sources);
    }

    [TestMethod]
    public void SameCteConsumedByHashAndKeySet_ShouldMatchBaseline()
    {
        const string query = @"
with indexed as (
    select b.Id as Id, b.Name as Name
    from #B.entities() b
)
select a.Name, r.Name
from #A.entities() a
inner join indexed r on a.Id = r.Id
semi join indexed s on a.Id = s.Id";

        AssertSameRows(query, CreateJoinSources());
    }

    [TestMethod]
    public void CteConsumedByLaterCteAndOuterQuery_ShouldMatchBaseline()
    {
        const string query = @"
with indexed as (
    select b.Id as Id, b.Name as Name
    from #B.entities() b
),
later as (
    select a.Id as Id
    from #A.entities() a
    inner join indexed r on a.Id = r.Id
)
select l.Id, r.Name
from later l
inner join indexed r on l.Id = r.Id";

        AssertSameRows(query, CreateJoinSources());
    }

    [TestMethod]
    public void NullableCteHashJoinKeys_ShouldPreserveBaselineNullBehavior()
    {
        const string query = @"
with rightCte as (
    select b.NullableValue as NullableValue, b.Name as Name
    from #B.entities() b
)
select a.Name, r.Name
from #A.entities() a
left outer join rightCte r on a.NullableValue = r.NullableValue";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("A-null") { NullableValue = null },
                    new BasicEntity("A-one") { NullableValue = 1 }
                ]
            },
            {
                "#B", [
                    new BasicEntity("B-null") { NullableValue = null },
                    new BasicEntity("B-one") { NullableValue = 1 }
                ]
            }
        };

        AssertSameRows(query, sources);
    }

    [TestMethod]
    public void NullableCompositeCteHashJoinKeys_ShouldPreserveBaselineBehavior()
    {
        const string query = @"
with rightCte as (
    select b.NullableValue as NullableValue, b.Country as Country, b.Name as Name
    from #B.entities() b
)
select a.Name, r.Name
from #A.entities() a
left outer join rightCte r on a.NullableValue = r.NullableValue and a.Country = r.Country";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("A-null-pl") { NullableValue = null, Country = "PL" },
                    new BasicEntity("A-one-pl") { NullableValue = 1, Country = "PL" },
                    new BasicEntity("A-one-de") { NullableValue = 1, Country = "DE" }
                ]
            },
            {
                "#B", [
                    new BasicEntity("B-null-pl") { NullableValue = null, Country = "PL" },
                    new BasicEntity("B-one-pl") { NullableValue = 1, Country = "PL" },
                    new BasicEntity("B-one-fr") { NullableValue = 1, Country = "FR" }
                ]
            }
        };

        AssertSameRows(query, sources);
    }

    [TestMethod]
    public void SemiAndAntiCteKeySetConsumers_ShouldMatchBaseline()
    {
        const string semiQuery = @"
with rightCte as (
    select b.Id as Id
    from #B.entities() b
)
select a.Name
from #A.entities() a
semi join rightCte r on a.Id = r.Id";

        const string antiQuery = @"
with rightCte as (
    select b.Id as Id
    from #B.entities() b
)
select a.Name
from #A.entities() a
anti join rightCte r on a.Id = r.Id";

        var sources = CreateJoinSources();

        AssertSameRows(semiQuery, sources);
        AssertSameRows(antiQuery, sources);
    }

    [TestMethod]
    public void DuplicateHeavySemiAndAntiCteKeySetConsumers_ShouldMatchBaseline()
    {
        const string semiQuery = @"
with rightCte as (
    select b.Id as Id
    from #B.entities() b
)
select a.Name
from #A.entities() a
semi join rightCte r on a.Id = r.Id";

        const string antiQuery = @"
with rightCte as (
    select b.Id as Id
    from #B.entities() b
)
select a.Name
from #A.entities() a
anti join rightCte r on a.Id = r.Id";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("A1") { Id = 1 },
                    new BasicEntity("A1Again") { Id = 1 },
                    new BasicEntity("A2") { Id = 2 },
                    new BasicEntity("A3") { Id = 3 }
                ]
            },
            {
                "#B", [
                    new BasicEntity("B1") { Id = 1 },
                    new BasicEntity("B1Duplicate") { Id = 1 },
                    new BasicEntity("B1Triplicate") { Id = 1 },
                    new BasicEntity("B3") { Id = 3 }
                ]
            }
        };

        AssertSameRows(semiQuery, sources);
        AssertSameRows(antiQuery, sources);
    }

    [TestMethod]
    public void FanoutThreeHashSidecars_WhenPipelinedWithFinalFilter_ShouldMatchBaseline()
    {
        const string query = @"
with names as (
    select a.Id as Id, a.Name as Name
    from #A.entities() a
),
cities as (
    select a.Id as Id, a.City as City
    from #A.entities() a
),
countries as (
    select a.Id as Id, a.Country as Country
    from #A.entities() a
)
select b.Name, n.Name, c.City, co.Country, b.Id + co.Id
from #B.entities() b
inner join names n on b.Id = n.Id
inner join cities c on b.Id = c.Id
inner join countries co on b.Id = co.Id
where c.City = 'Warsaw' or co.Country = 'FR'";

        AssertSameRows(query, CreateFanoutSources());
    }

    [TestMethod]
    public void StagedGraphMixedSidecars_WhenPipelinedThroughFinalSemiJoin_ShouldMatchBaseline()
    {
        const string query = @"
with raw as (
    select a.Id as Id, a.Name as Name, a.City as City, a.Country as Country, a.Population as Population
    from #A.entities() a
),
names as (
    select Id, Name
    from raw
),
cities as (
    select Id, City
    from raw
),
eligible as (
    select Id
    from raw
    where Population > 0
),
joined as (
    select b.Id as Id, n.Name as Name, c.City as City
    from #B.entities() b
    inner join names n on b.Id = n.Id
    inner join cities c on b.Id = c.Id
)
select j.Id + 100, j.Name, j.City
from joined j
semi join eligible e on j.Id = e.Id
where j.City = 'Warsaw' or j.Name = 'Cat'";

        AssertSameRows(query, CreateFanoutSources());
    }

    [TestMethod]
    public void FanoutThreeHashSidecars_WhenParallelized_ShouldMatchBaseline()
    {
        const string query = @"
with names as (
    select a.Id as Id, a.Name as Name
    from #A.entities() a
),
cities as (
    select a.Id as Id, a.City as City
    from #A.entities() a
),
countries as (
    select a.Id as Id, a.Country as Country
    from #A.entities() a
)
select b.Name, n.Name, c.City, co.Country
from #B.entities() b
inner join names n on b.Id = n.Id
inner join cities c on b.Id = c.Id
inner join countries co on b.Id = co.Id";

        AssertParallelSameRows(query, CreateFanoutSources());
    }

    [TestMethod]
    public void StagedGraphMixedSidecars_WhenParallelized_ShouldMatchBaseline()
    {
        const string query = @"
with raw as (
    select a.Id as Id, a.Name as Name, a.City as City, a.Country as Country, a.Population as Population
    from #A.entities() a
),
names as (
    select Id, Name
    from raw
),
cities as (
    select Id, City
    from raw
),
eligible as (
    select Id
    from raw
    where Population > 0
),
joined as (
    select b.Id as Id, n.Name as Name, c.City as City
    from #B.entities() b
    inner join names n on b.Id = n.Id
    inner join cities c on b.Id = c.Id
)
select j.Id, j.Name, j.City
from joined j
semi join eligible e on j.Id = e.Id";

        AssertParallelSameRows(query, CreateFanoutSources());
    }

    private void AssertSameRows(
        string query,
        IDictionary<string, IEnumerable<BasicEntity>> sources)
    {
        var baseline = CreateAndRunVirtualMachine(query, sources, BaselineOptions).Run(TestContext.CancellationToken);
        var optimized = CreateAndRunVirtualMachine(query, sources, SidecarOptions).Run(TestContext.CancellationToken);

        CollectionAssert.AreEqual(SerializeRows(baseline), SerializeRows(optimized));
    }

    private void AssertParallelSameRows(
        string query,
        IDictionary<string, IEnumerable<BasicEntity>> sources)
    {
        var baseline = CreateAndRunVirtualMachine(query, sources, BaselineOptions).Run(TestContext.CancellationToken);
        var optimized = CreateAndRunVirtualMachine(query, sources, ParallelSidecarOptions).Run(TestContext.CancellationToken);

        CollectionAssert.AreEqual(SerializeRows(baseline), SerializeRows(optimized));
    }

    private static string[] SerializeRows(Table table)
    {
        return table
            .Select(row => string.Join("|", row.Values.Select(static value => value?.ToString() ?? "<null>")))
            .OrderBy(static value => value, StringComparer.Ordinal)
            .ToArray();
    }

    private static Dictionary<string, IEnumerable<BasicEntity>> CreateJoinSources()
    {
        return new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("A1") { Id = 1 },
                    new BasicEntity("A2") { Id = 2 },
                    new BasicEntity("A3") { Id = 3 }
                ]
            },
            {
                "#B", [
                    new BasicEntity("B1") { Id = 1 },
                    new BasicEntity("B1Duplicate") { Id = 1 },
                    new BasicEntity("B3") { Id = 3 },
                    new BasicEntity("B4") { Id = 4 }
                ]
            }
        };
    }

    private static Dictionary<string, IEnumerable<BasicEntity>> CreateFanoutSources()
    {
        return new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Id = 1, Name = "Ada", City = "Warsaw", Country = "PL", Population = 100 },
                    new BasicEntity { Id = 2, Name = "Ben", City = "Berlin", Country = "DE", Population = 0 },
                    new BasicEntity { Id = 3, Name = "Cat", City = "Paris", Country = "FR", Population = 50 },
                    new BasicEntity { Id = 3, Name = "Cat2", City = "Lyon", Country = "FR", Population = 70 }
                ]
            },
            {
                "#B", [
                    new BasicEntity { Id = 1, Name = "Target1" },
                    new BasicEntity { Id = 2, Name = "Target2" },
                    new BasicEntity { Id = 3, Name = "Target3" },
                    new BasicEntity { Id = 4, Name = "Target4" }
                ]
            }
        };
    }
}
