using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tables;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[global::Musoq.Tests.Common.CultureTestClass]
public sealed class CteSidecarIndexExecutionTests : BasicEntityTestBase
{
    private static readonly CompilationOptions BaselineOptions = new(
        parallelizationMode: ParallelizationMode.None,
        useHashJoin: true,
        useSortMergeJoin: false,
        usePrimitiveTypeValidation: false
    );

    private static readonly CompilationOptions SidecarOptions = new(
        parallelizationMode: ParallelizationMode.None,
        useHashJoin: true,
        useSortMergeJoin: false,
        usePrimitiveTypeValidation: false,
        useCteSidecarIndexes: true
    );

    private static readonly CompilationOptions ParallelSidecarOptions = new(
        parallelizationMode: ParallelizationMode.Full,
        useHashJoin: true,
        useSortMergeJoin: false,
        usePrimitiveTypeValidation: false,
        useCteParallelization: true,
        useCteSidecarIndexes: true
    );

    [TestMethod]
    public void InnerCteHashJoin_WithDuplicates_ShouldMatchBaseline()
    {
        const string query = (
            "\n"
            + "with rightCte as (\n"
            + "    select b.Id as Id, b.Name as Name\n"
            + "    from #B.entities() b\n"
            + ")\n"
            + "select a.Name, r.Name\n"
            + "from #A.entities() a\n"
            + "inner join rightCte r on a.Id = r.Id"
        );

        var sources = CreateJoinSources();

        AssertSameRows(query, sources);
    }

    [TestMethod]
    public void LeftOuterCteHashJoin_ShouldMatchBaseline()
    {
        const string query = (
            "\n"
            + "with rightCte as (\n"
            + "    select b.Id as Id, b.Name as Name\n"
            + "    from #B.entities() b\n"
            + ")\n"
            + "select a.Name, r.Name\n"
            + "from #A.entities() a\n"
            + "left outer join rightCte r on a.Id = r.Id"
        );

        var sources = CreateJoinSources();

        AssertSameRows(query, sources);
    }

    [TestMethod]
    public void RightOuterCteHashJoin_WhenCteIsPhysicalBuildSide_ShouldMatchBaseline()
    {
        const string query = (
            "\n"
            + "with leftCte as (\n"
            + "    select a.Id as Id, a.Name as Name\n"
            + "    from #A.entities() a\n"
            + ")\n"
            + "select l.Name, b.Name\n"
            + "from leftCte l\n"
            + "right outer join #B.entities() b on l.Id = b.Id"
        );

        var sources = CreateJoinSources();

        AssertSameRows(query, sources);
    }

    [TestMethod]
    public void RepeatedCteSelfJoin_WithDuplicates_ShouldMatchBaseline()
    {
        const string query = (
            "\n"
            + "with indexed as (\n"
            + "    select a.Id as Id, a.Name as Name\n"
            + "    from #A.entities() a\n"
            + ")\n"
            + "select l.Name, r.Name\n"
            + "from indexed l\n"
            + "inner join indexed r on l.Id = r.Id"
        );

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("A1") { Id = 1 },
                    new BasicEntity("A1Duplicate") { Id = 1 },
                    new BasicEntity("A2") { Id = 2 },
                ]
            },
        };

        AssertSameRows(query, sources);
    }

    [TestMethod]
    public void CompositeCteHashJoin_ShouldMatchBaseline()
    {
        const string query = (
            "\n"
            + "with rightCte as (\n"
            + "    select b.Id as Id, b.Country as Country, b.Name as Name\n"
            + "    from #B.entities() b\n"
            + ")\n"
            + "select a.Name, r.Name\n"
            + "from #A.entities() a\n"
            + "inner join rightCte r on a.Id = r.Id and a.Country = r.Country"
        );

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity
                    {
                        Name = "A1",
                        Id = 1,
                        Country = "PL",
                    },
                    new BasicEntity
                    {
                        Name = "A2",
                        Id = 1,
                        Country = "DE",
                    },
                    new BasicEntity
                    {
                        Name = "A3",
                        Id = 2,
                        Country = "PL",
                    },
                ]
            },
            {
                "#B",
                [
                    new BasicEntity
                    {
                        Name = "B1",
                        Id = 1,
                        Country = "PL",
                    },
                    new BasicEntity
                    {
                        Name = "B2",
                        Id = 1,
                        Country = "FR",
                    },
                    new BasicEntity
                    {
                        Name = "B3",
                        Id = 2,
                        Country = "PL",
                    },
                ]
            },
        };

        AssertSameRows(query, sources);
    }

    [TestMethod]
    public void SameCteConsumedOnDifferentKeys_ShouldMatchBaseline()
    {
        const string query = (
            "\n"
            + "with indexed as (\n"
            + "    select b.Id as Id, b.Country as Country, b.Name as Name\n"
            + "    from #B.entities() b\n"
            + ")\n"
            + "select a.Name, r.Name, s.Name\n"
            + "from #A.entities() a\n"
            + "inner join indexed r on a.Id = r.Id\n"
            + "inner join indexed s on a.Country = s.Country"
        );

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity
                    {
                        Name = "A1",
                        Id = 1,
                        Country = "PL",
                    },
                    new BasicEntity
                    {
                        Name = "A2",
                        Id = 2,
                        Country = "DE",
                    },
                ]
            },
            {
                "#B",
                [
                    new BasicEntity
                    {
                        Name = "B1",
                        Id = 1,
                        Country = "PL",
                    },
                    new BasicEntity
                    {
                        Name = "B2",
                        Id = 2,
                        Country = "FR",
                    },
                    new BasicEntity
                    {
                        Name = "B3",
                        Id = 3,
                        Country = "DE",
                    },
                ]
            },
        };

        AssertSameRows(query, sources);
    }

    [TestMethod]
    public void SameCteConsumedByHashAndKeySet_ShouldMatchBaseline()
    {
        const string query = (
            "\n"
            + "with indexed as (\n"
            + "    select b.Id as Id, b.Name as Name\n"
            + "    from #B.entities() b\n"
            + ")\n"
            + "select a.Name, r.Name\n"
            + "from #A.entities() a\n"
            + "inner join indexed r on a.Id = r.Id\n"
            + "semi join indexed s on a.Id = s.Id"
        );

        AssertSameRows(query, CreateJoinSources());
    }

    [TestMethod]
    public void CteConsumedByLaterCteAndOuterQuery_ShouldMatchBaseline()
    {
        const string query = (
            "\n"
            + "with indexed as (\n"
            + "    select b.Id as Id, b.Name as Name\n"
            + "    from #B.entities() b\n"
            + "),\n"
            + "later as (\n"
            + "    select a.Id as Id\n"
            + "    from #A.entities() a\n"
            + "    inner join indexed r on a.Id = r.Id\n"
            + ")\n"
            + "select l.Id, r.Name\n"
            + "from later l\n"
            + "inner join indexed r on l.Id = r.Id"
        );

        AssertSameRows(query, CreateJoinSources());
    }

    [TestMethod]
    public void NullableCteHashJoinKeys_ShouldPreserveBaselineNullBehavior()
    {
        const string query = (
            "\n"
            + "with rightCte as (\n"
            + "    select b.NullableValue as NullableValue, b.Name as Name\n"
            + "    from #B.entities() b\n"
            + ")\n"
            + "select a.Name, r.Name\n"
            + "from #A.entities() a\n"
            + "left outer join rightCte r on a.NullableValue = r.NullableValue"
        );

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [new BasicEntity("A-null") { NullableValue = null }, new BasicEntity("A-one") { NullableValue = 1 }]
            },
            {
                "#B",
                [new BasicEntity("B-null") { NullableValue = null }, new BasicEntity("B-one") { NullableValue = 1 }]
            },
        };

        AssertSameRows(query, sources);
    }

    [TestMethod]
    public void NullableCompositeCteHashJoinKeys_ShouldPreserveBaselineBehavior()
    {
        const string query = (
            "\n"
            + "with rightCte as (\n"
            + "    select b.NullableValue as NullableValue, b.Country as Country, b.Name as Name\n"
            + "    from #B.entities() b\n"
            + ")\n"
            + "select a.Name, r.Name\n"
            + "from #A.entities() a\n"
            + "left outer join rightCte r on a.NullableValue = r.NullableValue and a.Country = r.Country"
        );

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("A-null-pl") { NullableValue = null, Country = "PL" },
                    new BasicEntity("A-one-pl") { NullableValue = 1, Country = "PL" },
                    new BasicEntity("A-one-de") { NullableValue = 1, Country = "DE" },
                ]
            },
            {
                "#B",
                [
                    new BasicEntity("B-null-pl") { NullableValue = null, Country = "PL" },
                    new BasicEntity("B-one-pl") { NullableValue = 1, Country = "PL" },
                    new BasicEntity("B-one-fr") { NullableValue = 1, Country = "FR" },
                ]
            },
        };

        AssertSameRows(query, sources);
    }

    [TestMethod]
    public void SemiAndAntiCteKeySetConsumers_ShouldMatchBaseline()
    {
        const string semiQuery = (
            "\n"
            + "with rightCte as (\n"
            + "    select b.Id as Id\n"
            + "    from #B.entities() b\n"
            + ")\n"
            + "select a.Name\n"
            + "from #A.entities() a\n"
            + "semi join rightCte r on a.Id = r.Id"
        );

        const string antiQuery = (
            "\n"
            + "with rightCte as (\n"
            + "    select b.Id as Id\n"
            + "    from #B.entities() b\n"
            + ")\n"
            + "select a.Name\n"
            + "from #A.entities() a\n"
            + "anti join rightCte r on a.Id = r.Id"
        );

        var sources = CreateJoinSources();

        AssertSameRows(semiQuery, sources);
        AssertSameRows(antiQuery, sources);
    }

    [TestMethod]
    public void DuplicateHeavySemiAndAntiCteKeySetConsumers_ShouldMatchBaseline()
    {
        const string semiQuery = (
            "\n"
            + "with rightCte as (\n"
            + "    select b.Id as Id\n"
            + "    from #B.entities() b\n"
            + ")\n"
            + "select a.Name\n"
            + "from #A.entities() a\n"
            + "semi join rightCte r on a.Id = r.Id"
        );

        const string antiQuery = (
            "\n"
            + "with rightCte as (\n"
            + "    select b.Id as Id\n"
            + "    from #B.entities() b\n"
            + ")\n"
            + "select a.Name\n"
            + "from #A.entities() a\n"
            + "anti join rightCte r on a.Id = r.Id"
        );

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("A1") { Id = 1 },
                    new BasicEntity("A1Again") { Id = 1 },
                    new BasicEntity("A2") { Id = 2 },
                    new BasicEntity("A3") { Id = 3 },
                ]
            },
            {
                "#B",
                [
                    new BasicEntity("B1") { Id = 1 },
                    new BasicEntity("B1Duplicate") { Id = 1 },
                    new BasicEntity("B1Triplicate") { Id = 1 },
                    new BasicEntity("B3") { Id = 3 },
                ]
            },
        };

        AssertSameRows(semiQuery, sources);
        AssertSameRows(antiQuery, sources);
    }

    [TestMethod]
    public void FanoutThreeHashSidecars_WhenPipelinedWithFinalFilter_ShouldMatchBaseline()
    {
        const string query = (
            "\n"
            + "with names as (\n"
            + "    select a.Id as Id, a.Name as Name\n"
            + "    from #A.entities() a\n"
            + "),\n"
            + "cities as (\n"
            + "    select a.Id as Id, a.City as City\n"
            + "    from #A.entities() a\n"
            + "),\n"
            + "countries as (\n"
            + "    select a.Id as Id, a.Country as Country\n"
            + "    from #A.entities() a\n"
            + ")\n"
            + "select b.Name, n.Name, c.City, co.Country, b.Id + co.Id\n"
            + "from #B.entities() b\n"
            + "inner join names n on b.Id = n.Id\n"
            + "inner join cities c on b.Id = c.Id\n"
            + "inner join countries co on b.Id = co.Id\n"
            + "where c.City = 'Warsaw' or co.Country = 'FR'"
        );

        AssertSameRows(query, CreateFanoutSources());
    }

    [TestMethod]
    public void StagedGraphMixedSidecars_WhenPipelinedThroughFinalSemiJoin_ShouldMatchBaseline()
    {
        const string query = (
            "\n"
            + "with raw as (\n"
            + "    select a.Id as Id, a.Name as Name, a.City as City, a.Country as Country, a.Population as Population\n"
            + "    from #A.entities() a\n"
            + "),\n"
            + "names as (\n"
            + "    select Id, Name\n"
            + "    from raw\n"
            + "),\n"
            + "cities as (\n"
            + "    select Id, City\n"
            + "    from raw\n"
            + "),\n"
            + "eligible as (\n"
            + "    select Id\n"
            + "    from raw\n"
            + "    where Population > 0\n"
            + "),\n"
            + "joined as (\n"
            + "    select b.Id as Id, n.Name as Name, c.City as City\n"
            + "    from #B.entities() b\n"
            + "    inner join names n on b.Id = n.Id\n"
            + "    inner join cities c on b.Id = c.Id\n"
            + ")\n"
            + "select j.Id + 100, j.Name, j.City\n"
            + "from joined j\n"
            + "semi join eligible e on j.Id = e.Id\n"
            + "where j.City = 'Warsaw' or j.Name = 'Cat'"
        );

        AssertSameRows(query, CreateFanoutSources());
    }

    [TestMethod]
    public void FanoutThreeHashSidecars_WhenParallelized_ShouldMatchBaseline()
    {
        const string query = (
            "\n"
            + "with names as (\n"
            + "    select a.Id as Id, a.Name as Name\n"
            + "    from #A.entities() a\n"
            + "),\n"
            + "cities as (\n"
            + "    select a.Id as Id, a.City as City\n"
            + "    from #A.entities() a\n"
            + "),\n"
            + "countries as (\n"
            + "    select a.Id as Id, a.Country as Country\n"
            + "    from #A.entities() a\n"
            + ")\n"
            + "select b.Name, n.Name, c.City, co.Country\n"
            + "from #B.entities() b\n"
            + "inner join names n on b.Id = n.Id\n"
            + "inner join cities c on b.Id = c.Id\n"
            + "inner join countries co on b.Id = co.Id"
        );

        AssertParallelSameRows(query, CreateFanoutSources());
    }

    [TestMethod]
    public void StagedGraphMixedSidecars_WhenParallelized_ShouldMatchBaseline()
    {
        const string query = (
            "\n"
            + "with raw as (\n"
            + "    select a.Id as Id, a.Name as Name, a.City as City, a.Country as Country, a.Population as Population\n"
            + "    from #A.entities() a\n"
            + "),\n"
            + "names as (\n"
            + "    select Id, Name\n"
            + "    from raw\n"
            + "),\n"
            + "cities as (\n"
            + "    select Id, City\n"
            + "    from raw\n"
            + "),\n"
            + "eligible as (\n"
            + "    select Id\n"
            + "    from raw\n"
            + "    where Population > 0\n"
            + "),\n"
            + "joined as (\n"
            + "    select b.Id as Id, n.Name as Name, c.City as City\n"
            + "    from #B.entities() b\n"
            + "    inner join names n on b.Id = n.Id\n"
            + "    inner join cities c on b.Id = c.Id\n"
            + ")\n"
            + "select j.Id, j.Name, j.City\n"
            + "from joined j\n"
            + "semi join eligible e on j.Id = e.Id"
        );

        AssertParallelSameRows(query, CreateFanoutSources());
    }

    private void AssertSameRows(string query, IDictionary<string, IEnumerable<BasicEntity>> sources)
    {
        var baseline = CreateAndRunVirtualMachine(query, sources, BaselineOptions).Run(TestContext.CancellationToken);
        var optimized = CreateAndRunVirtualMachine(query, sources, SidecarOptions).Run(TestContext.CancellationToken);

        CollectionAssert.AreEqual(SerializeRows(baseline), SerializeRows(optimized));
    }

    private void AssertParallelSameRows(string query, IDictionary<string, IEnumerable<BasicEntity>> sources)
    {
        var baseline = CreateAndRunVirtualMachine(query, sources, BaselineOptions).Run(TestContext.CancellationToken);
        var optimized = CreateAndRunVirtualMachine(query, sources, ParallelSidecarOptions)
            .Run(TestContext.CancellationToken);

        CollectionAssert.AreEqual(SerializeRows(baseline), SerializeRows(optimized));
    }

    private static string[] SerializeRows(Table table)
    {
        return table
            .Select(row => string.Join("|", row.ReadValues().Select(static value => value?.ToString() ?? "<null>")))
            .OrderBy(static value => value, StringComparer.Ordinal)
            .ToArray();
    }

    private static Dictionary<string, IEnumerable<BasicEntity>> CreateJoinSources()
    {
        return new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [new BasicEntity("A1") { Id = 1 }, new BasicEntity("A2") { Id = 2 }, new BasicEntity("A3") { Id = 3 }]
            },
            {
                "#B",
                [
                    new BasicEntity("B1") { Id = 1 },
                    new BasicEntity("B1Duplicate") { Id = 1 },
                    new BasicEntity("B3") { Id = 3 },
                    new BasicEntity("B4") { Id = 4 },
                ]
            },
        };
    }

    private static Dictionary<string, IEnumerable<BasicEntity>> CreateFanoutSources()
    {
        return new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity
                    {
                        Id = 1,
                        Name = "Ada",
                        City = "Warsaw",
                        Country = "PL",
                        Population = 100,
                    },
                    new BasicEntity
                    {
                        Id = 2,
                        Name = "Ben",
                        City = "Berlin",
                        Country = "DE",
                        Population = 0,
                    },
                    new BasicEntity
                    {
                        Id = 3,
                        Name = "Cat",
                        City = "Paris",
                        Country = "FR",
                        Population = 50,
                    },
                    new BasicEntity
                    {
                        Id = 3,
                        Name = "Cat2",
                        City = "Lyon",
                        Country = "FR",
                        Population = 70,
                    },
                ]
            },
            {
                "#B",
                [
                    new BasicEntity { Id = 1, Name = "Target1" },
                    new BasicEntity { Id = 2, Name = "Target2" },
                    new BasicEntity { Id = 3, Name = "Target3" },
                    new BasicEntity { Id = 4, Name = "Target4" },
                ]
            },
        };
    }
}
