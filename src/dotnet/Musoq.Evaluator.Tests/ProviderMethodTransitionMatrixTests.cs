using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator.Tables;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Parser.Diagnostics;
using static Musoq.Evaluator.Tests.MusoqExceptionAssertions;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class ProviderMethodTransitionMatrixTests : BasicEntityTestBase
{
    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    public void RightOuterJoin_ShouldNullLiftProviderMethodsOnMissingLeftRows()
    {
        const string query = @"
select a.GetCountry() as LeftCountry, b.GetCountry() as RightCountry
from #A.entities() a
right outer join #B.entities() b on a.Id = b.Id";
        var sources = Sources(
            [],
            [new BasicEntity("Poland", "Warsaw") { Id = 2 }]);

        var table = Run(query, sources);

        TableMaterializationTestHelper.AssertRowsInOrder(table, [null, "Poland"]);
    }

    [TestMethod]
    public void LeftOuterJoin_WithOrdering_ShouldNullLiftProviderMethodsOnMissingRightRows()
    {
        const string query = @"
select a.Id, b.GetCountry() as RightCountry
from #A.entities() a
left outer join #B.entities() b on a.Id = b.Id
order by a.Id";
        var table = Run(query, Sources(
            [new BasicEntity { Id = 1 }, new BasicEntity("Poland", "Warsaw") { Id = 2 }],
            [new BasicEntity("Poland", "Warsaw") { Id = 2 }]));

        TableMaterializationTestHelper.AssertRowsInOrder(table, [1, null], [2, "Poland"]);
    }

    [TestMethod]
    public void OrderByProviderMethod_ShouldNullLiftMissingSourceBeforeSorting()
    {
        const string query = @"
select a.Id, b.GetCountry() as RightCountry
from #A.entities() a
left outer join #B.entities() b on a.Id = b.Id
order by b.GetCountry(), a.Id";
        var table = Run(query, Sources(
            [new BasicEntity { Id = 1 }, new BasicEntity("Poland", "Warsaw") { Id = 2 }],
            [new BasicEntity("Poland", "Warsaw") { Id = 2 }]));

        TableMaterializationTestHelper.AssertRowsInOrder(table, [1, null], [2, "Poland"]);
    }

    [TestMethod]
    public void FullOuterJoin_ShouldNullLiftProviderMethodsOnEitherMissingSide()
    {
        const string query = @"
select a.GetCountry() as LeftCountry, b.GetCountry() as RightCountry
from #A.entities() a
full outer join #B.entities() b on a.Id = b.Id";
        var sources = Sources(
            [new BasicEntity("France", "Paris") { Id = 1 }],
            [new BasicEntity("Poland", "Warsaw") { Id = 2 }]);

        var table = Run(query, sources);

        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["France", null],
            [null, "Poland"]);
    }

    [TestMethod]
    public void ChainedOuterJoins_ShouldPreserveProviderOwnersAcrossCompoundNullableSide()
    {
        const string query = @"
select a.GetCountry() as LeftCountry,
       b.GetCountry() as MiddleCountry,
       c.GetCountry() as RightCountry
from #A.entities() a
left outer join #B.entities() b on a.Id = b.Id
left outer join #C.entities() c on b.Id = c.Id
order by a.Id";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            ["#A"] = [new BasicEntity("France", "Paris") { Id = 1 }],
            ["#B"] = [],
            ["#C"] = [new BasicEntity("Poland", "Warsaw") { Id = 2 }]
        };

        var table = Run(query, sources);

        TableMaterializationTestHelper.AssertRowsInOrder(table, ["France", null, null]);
    }

    [TestMethod]
    public void AsOfLeftJoin_ShouldNullLiftProviderMethodsOnUnmatchedRows()
    {
        const string query = @"
select a.GetCountry() as LeftCountry, b.GetCountry() as RightCountry
from #A.entities() a
asof left join #B.entities() b on a.Population >= b.Population";
        var sources = Sources(
            [new BasicEntity("France", "Paris") { Population = 100 }],
            [new BasicEntity("Poland", "Warsaw") { Population = 200 }]);

        var table = Run(query, sources);

        TableMaterializationTestHelper.AssertRowsInOrder(table, ["France", null]);
    }

    [TestMethod]
    public void ProviderMethods_ShouldRemainBoundInJoinPredicatesAndFilters()
    {
        const string query = @"
select a.GetCountry() as LeftCountry, b.GetCountry() as RightCountry
from #A.entities() a
inner join #B.entities() b on a.GetCountry() = b.GetCountry()
where a.GetCountry() = 'Poland'";
        var sources = Sources(
            [new BasicEntity("Poland", "Warsaw") { Id = 1 }],
            [new BasicEntity("Poland", "Krakow") { Id = 2 }]);

        var table = Run(query, sources);

        TableMaterializationTestHelper.AssertRowsInOrder(table, ["Poland", "Poland"]);
    }

    [TestMethod]
    public void GuardedCase_ShouldEvaluateProviderMethodOnlyForPresentRows()
    {
        const string query = @"
select case when b is missing then 'missing' else b.GetCountry() end as Country
from #A.entities() a
left outer join #B.entities() b on a.Id = b.Id";
        var sources = Sources(
            [new BasicEntity { Id = 1 }],
            []);

        var table = Run(query, sources);

        TableMaterializationTestHelper.AssertRowsInOrder(table, ["missing"]);
    }

    [TestMethod]
    public void ProviderMethods_ShouldRemainBoundInAggregateArgumentsAndWindows()
    {
        const string aggregateQuery = @"
select a.GetCountry() as Country,
       Sum(a.GetPopulation()) as TotalPopulation,
       CustomRowCount() as RowCount
from #A.entities() a
group by a.GetCountry()
having a.GetCountry() = 'Poland'";
        var sources = Sources(
            [
                new BasicEntity("Poland", "Warsaw") { Population = 3 },
                new BasicEntity("Poland", "Krakow") { Population = 5 }
            ],
            []);

        var table = Run(aggregateQuery, sources);

        TableMaterializationTestHelper.AssertRowsInOrder(table, ["Poland", 8m, 2L]);

        const string windowQuery = @"
select a.GetCountry() as Country,
       RowNumber() over (order by a.GetPopulation()) as Rank,
       RunningProduct(a.GetPopulation()) over (order by a.GetPopulation()) as Product
from #A.entities() a";
        var windowTable = Run(windowQuery, sources);

        TableMaterializationTestHelper.AssertRowsInOrder(
            windowTable,
            ["Poland", 1L, 3m],
            ["Poland", 2L, 15m]);
    }

    [TestMethod]
    public void ProviderMethods_ShouldSupportExplicitAndGenericArguments()
    {
        const string query = @"
select a.GetCountryOrDefault('fallback') as ExplicitValue,
       a.GetCountryOrDefaultGeneric('generic') as GenericValue
from #A.entities() a";
        var table = Run(query, Sources([new BasicEntity { Id = 1 }], []));

        TableMaterializationTestHelper.AssertRowsInOrder(table, ["fallback", "generic"]);
    }

    [TestMethod]
    public void CrossApplyProviderMethod_ShouldBindInjectedSourceWithExplicitArguments()
    {
        const string query = @"
select b as Value
from #A.entities() a
cross apply a.MethodArrayOfStrings(a.GetCountry(), a.GetCity()) b";
        var table = Run(query, Sources(
            [new BasicEntity("Poland", "Warsaw")],
            []));

        TableMaterializationTestHelper.AssertRowsUnordered(table, ["Poland"], ["Warsaw"]);
    }

    [TestMethod]
    public void CrossApplyProviderMethod_WithOrdinality_ShouldPreserveInjectedSourceBinding()
    {
        const string query = @"
select b.Value, b.Ordinal
from #A.entities() a
cross apply a.MethodArrayOfStrings(a.GetCountry(), a.GetCity()) b with ordinality
order by b.Ordinal";
        var table = Run(query, Sources(
            [new BasicEntity("Poland", "Warsaw")],
            []));

        TableMaterializationTestHelper.AssertRowsInOrder(table, ["Poland", 0], ["Warsaw", 1]);
    }

    [TestMethod]
    public void OuterApplyPropertySource_ShouldBindProviderMethodWithOrdinality()
    {
        const string query = @"
select child.Name() as ChildName, child.Ordinal
from #A.entities() a
outer apply a.Children child with ordinality
order by child.Ordinal";
        var table = Run(query, Sources([new BasicEntity { Id = 1 }], []));

        TableMaterializationTestHelper.AssertRowsInOrder(table, ["child1", 0], ["child2", 1]);
    }

    [TestMethod]
    public void RowIndependentProviderMethod_ShouldRemainExecutableOnMissingAlias()
    {
        const string query = @"
select b.GetOne() as Value
from #A.entities() a
left outer join #B.entities() b on a.Id = b.Id";
        var table = Run(query, Sources([new BasicEntity { Id = 1 }], []));

        TableMaterializationTestHelper.AssertRowsInOrder(table, [1m]);
    }

    [TestMethod]
    public void ProviderMethodInsideCte_ShouldExecuteBeforeProviderNeutralBoundary()
    {
        const string query = @"
with countries as (
    select a.GetCountry() as Country
    from #A.entities() a
)
select Country from countries";
        var table = Run(query, Sources([new BasicEntity("Poland", "Warsaw")], []));

        TableMaterializationTestHelper.AssertRowsInOrder(table, ["Poland"]);
    }

    [TestMethod]
    public void ProviderMethodAfterCteBoundary_ShouldRemainUnavailable()
    {
        const string query = @"
with countries as (
    select a.GetCountry() as Country
    from #A.entities() a
)
select countries.GetCountry() from countries";

        var exception = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachine(query, Sources([new BasicEntity("Poland", "Warsaw")], [])));

        AssertErrorEnvelope(
            exception,
            DiagnosticCode.MQ3029_UnresolvableMethod,
            DiagnosticPhase.Bind,
            "GetCountry");
    }

    [TestMethod]
    public void ProviderMethodAfterValuesBoundary_ShouldRemainUnavailable()
    {
        const string query = @"
select values.GetCountry()
from values {
    { Name: 'Poland' }
} values";

        var exception = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachine(query, Sources([], [])));

        AssertErrorEnvelope(
            exception,
            DiagnosticCode.MQ3029_UnresolvableMethod,
            DiagnosticPhase.Bind,
            "GetCountry");
    }

    [TestMethod]
    public void ProviderMethodAfterUnpivotBoundary_ShouldRemainUnavailable()
    {
        const string query = @"
with unpivoted as (
    unpivot #A.entities() s
    on Metric in (s.Population as Population)
    using Amount
    keep s.Country as Country
)
select unpivoted.GetCountry()
from unpivoted";

        var exception = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachine(query, Sources([new BasicEntity { Country = "Poland", Population = 1m }], [])));

        AssertErrorEnvelope(
            exception,
            DiagnosticCode.MQ3029_UnresolvableMethod,
            DiagnosticPhase.Bind,
            "GetCountry");
    }

    [TestMethod]
    public void ProviderMethods_ShouldRemainBoundAcrossSetBranches()
    {
        const string query = @"
select a.GetCountry() as Country from #A.entities() a
union all
select b.GetCountry() as Country from #B.entities() b";
        var table = Run(query, Sources(
            [new BasicEntity("France", "Paris")],
            [new BasicEntity("Poland", "Warsaw")]));

        TableMaterializationTestHelper.AssertRowsUnordered(table, ["France"], ["Poland"]);
    }

    [TestMethod]
    public void SemiAndAntiJoins_ShouldPreserveLeftProviderMethods()
    {
        const string semiQuery = @"
select a.GetCountry() as Country
from #A.entities() a
semi join #B.entities() b on a.Id = b.Id";
        const string antiQuery = @"
select a.GetCountry() as Country
from #A.entities() a
anti join #B.entities() b on a.Id = b.Id";
        var sources = Sources(
            [new BasicEntity("France", "Paris") { Id = 1 }, new BasicEntity("Poland", "Warsaw") { Id = 2 }],
            [new BasicEntity("Poland", "Warsaw") { Id = 2 }]);

        var semi = Run(semiQuery, sources);
        var anti = Run(antiQuery, sources);

        TableMaterializationTestHelper.AssertRowsInOrder(semi, ["Poland"]);
        TableMaterializationTestHelper.AssertRowsInOrder(anti, ["France"]);
    }

    [TestMethod]
    public void BuiltInUtility_ShouldRemainCheapAndBoundAcrossJoinTransition()
    {
        const string query = @"
select a.ToDecimal(a.Id) as LeftId, b.ToDecimal(b.Id) as RightId
from #A.entities() a
left outer join #B.entities() b on a.Id = b.Id
order by a.Id";
        var table = Run(query, Sources(
            [new BasicEntity { Id = 1 }, new BasicEntity { Id = 2 }],
            [new BasicEntity { Id = 2 }]));

        TableMaterializationTestHelper.AssertRowsInOrder(table, [1m, null], [2m, 2m]);
    }

    private Table Run(string query, IDictionary<string, IEnumerable<BasicEntity>> sources)
    {
        return TableMaterializationTestHelper.Materialize(
            CreateAndRunVirtualMachine(query, sources).Run(TestContext.CancellationToken));
    }

    private static IDictionary<string, IEnumerable<BasicEntity>> Sources(
        IEnumerable<BasicEntity> left,
        IEnumerable<BasicEntity> right)
    {
        return new Dictionary<string, IEnumerable<BasicEntity>>
        {
            ["#A"] = left,
            ["#B"] = right
        };
    }
}
