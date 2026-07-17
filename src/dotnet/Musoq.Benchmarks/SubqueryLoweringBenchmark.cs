using BenchmarkDotNet.Attributes;
using Musoq.Benchmarks.Components;
using Musoq.Benchmarks.Helpers;
using Musoq.Benchmarks.Schema.Country;
using Musoq.Benchmarks.Schema.Profiles;
using Musoq.Evaluator;
using Musoq.Evaluator.Tables;

namespace Musoq.Benchmarks;

[MemoryDiagnoser]
[ShortRunJob]
public class SubqueryLoweringBenchmark : BenchmarkBase
{
    private IReadOnlyList<ProfileEntity> _left = null!;
    private IReadOnlyList<ProfileEntity> _right = null!;
    private IReadOnlyList<ProfileEntity> _third = null!;
    private IReadOnlyList<CountryEntity> _countryLeft = null!;
    private IReadOnlyList<CountryEntity> _countryRight = null!;
    private IReadOnlyList<CountryEntity> _countryThird = null!;
    private CompiledQuery _existingIn = null!;
    private CompiledQuery _correlatedIn = null!;
    private CompiledQuery _correlatedNotIn = null!;
    private CompiledQuery _correlatedExists = null!;
    private CompiledQuery _correlatedNotExists = null!;
    private CompiledQuery _scalarAggregate = null!;
    private CompiledQuery _scalarNonAggregate = null!;
    private CompiledQuery _scalarPartitionedTopOffset = null!;
    private CompiledQuery _scalarGrouped = null!;
    private CompiledQuery _scalarCorrelatedSetOperator = null!;
    private CompiledQuery _scalarSubqueryJoinOn = null!;
    private CompiledQuery _quantifiedAll = null!;
    private CompiledQuery _derivedCrossApply = null!;
    private CompiledQuery _derivedCrossApplySelective = null!;
    private CompiledQuery _compositeStringKey = null!;
    private CompiledQuery _compositeStringDecimalKey = null!;
    private CompiledQuery _predicateExpressionFallback = null!;
    private CompiledQuery _predicateRangeMark = null!;
    private CompiledQuery _predicatePartitionedRangeMark = null!;
    private CompiledQuery _predicateCompositeRangeMark = null!;
    private CompiledQuery _correlatedApplySetOperator = null!;

    [GlobalSetup]
    public void Setup()
    {
        var contentPath = Path.Combine(AppContext.BaseDirectory, "Data", "profiles.csv");
        var data = DataHelpers.ReadProfiles(contentPath).Take(10000).ToArray();

        _left = data;
        _right = data.Where((_, index) => index % 2 == 0).ToArray();
        _third = data.Where((_, index) => index % 3 == 0).ToArray();

        var countryPath = Path.Combine(AppContext.BaseDirectory, "Data", "countries.json");
        var countries = DataHelpers.ParseCountryData(countryPath).ToArray();
        _countryLeft = countries;
        _countryRight = countries.Where((_, index) => index % 2 == 0).ToArray();
        _countryThird = countries.Where((_, index) => index % 3 == 0).ToArray();

        _existingIn = CreateProfileQuery("""
            select a.FirstName, a.Animal
            from #A.Entities() a
            where a.Animal in (
                select b.Animal from #B.Entities() b
            )
            """);

        _correlatedIn = CreateProfileQuery("""
            select a.FirstName, a.Animal
            from #A.Entities() a
            where a.Animal in (
                select b.Animal from #B.Entities() b
                where b.Gender = a.Gender
            )
            """);

        _correlatedNotIn = CreateProfileQuery("""
            select a.FirstName, a.Animal
            from #A.Entities() a
            where a.Animal not in (
                select b.Animal from #B.Entities() b
                where b.Gender = a.Gender
            )
            """);

        _correlatedExists = CreateProfileQuery("""
            select a.FirstName, a.Animal
            from #A.Entities() a
            where exists (
                select b.Email from #B.Entities() b
                where b.Animal = a.Animal
            )
            """);

        _correlatedNotExists = CreateProfileQuery("""
            select a.FirstName, a.Animal
            from #A.Entities() a
            where not exists (
                select b.Email from #B.Entities() b
                where b.Animal = a.Animal
            )
            """);

        _scalarAggregate = CreateProfileQuery("""
            select a.FirstName,
                   (
                       select Count(b.Email) from #B.Entities() b
                       where b.Animal = a.Animal
                   ) as AnimalCount
            from #A.Entities() a
            """);

        _scalarNonAggregate = CreateProfileQuery("""
            select a.FirstName,
                   (
                       select b.FirstName from #B.Entities() b
                       where b.Email = a.Email
                   ) as MatchedFirstName
            from #A.Entities() a
            """);

        _scalarPartitionedTopOffset = CreateProfileQuery("""
            select a.FirstName,
                   (
                       select b.FirstName from #B.Entities() b
                       where b.Animal = a.Animal
                       order by b.Date desc
                       take 1
                   ) as LatestFirstName
            from #A.Entities() a
            """);

        _scalarGrouped = CreateProfileQuery("""
            select a.FirstName,
                   (
                       select Count(b.Email) from #B.Entities() b
                       where b.Animal = a.Animal
                       group by b.Animal
                   ) as AnimalCount
            from #A.Entities() a
            """);

        _scalarCorrelatedSetOperator = CreateProfileQuery("""
            select a.FirstName,
                   (
                       select b.Email from #B.Entities() b
                       where b.Email = a.Email
                       union (Email)
                       select c.Email from #C.Entities() c
                       where c.Email = a.Email
                   ) as MatchedEmail
            from #A.Entities() a
            """);

        _scalarSubqueryJoinOn = CreateCountryQuery("""
            select a.City, b.City
            from #A.Entities() a
            inner join #B.Entities() b on a.Country = b.Country
                and b.Population = (
                    select Max(c.Population) from #C.Entities() c
                    where c.Country = a.Country
                )
            """);

        _quantifiedAll = CreateCountryQuery("""
            select a.City, a.Country
            from #A.Entities() a
            where a.Population > all (
                select b.Population from #B.Entities() b
                where b.Country = a.Country
            )
            """);

        _derivedCrossApply = CreateCountryQuery("""
            select a.City, d.City
            from #A.Entities() a
            cross apply (
                select b.City, b.Country from #B.Entities() b
                where b.Country = a.Country
            ) d
            """);

        _derivedCrossApplySelective = CreateCountryQuery("""
            select a.City, d.City
            from #A.Entities() a
            cross apply (
                select b.City, b.Country from #B.Entities() b
                where b.Country = a.Country
                  and b.City = a.City
            ) d
            """);

        _compositeStringKey = CreateCountryQuery("""
            select a.City, a.Country
            from #A.Entities() a
            where a.City in (
                select b.City from #B.Entities() b
                where b.Country = a.Country
            )
            """);

        _compositeStringDecimalKey = CreateCountryQuery("""
            select a.City, a.Country
            from #A.Entities() a
            where exists (
                select b.City from #B.Entities() b
                where b.Country = a.Country
                  and b.Population = a.Population
            )
            """);

        _predicateExpressionFallback = CreateProfileQuery("""
            select a.FirstName,
                   case
                       when exists (
                           select b.Email from #B.Entities() b
                           where b.Animal = a.Animal
                       )
                       then 'Y'
                       else 'N'
                   end as HasAnimalMatch
            from #A.Entities() a
            """);

        _predicateRangeMark = CreateProfileQuery("""
            select a.FirstName,
                   case
                       when exists (
                           select b.Email from #B.Entities() b
                           where b.Date < a.Date
                       )
                       then 'Y'
                       else 'N'
                   end as HasEarlierMatch
            from #A.Entities() a
            """);

        _predicatePartitionedRangeMark = CreateProfileQuery("""
            select a.FirstName,
                   case
                       when exists (
                           select b.Email from #B.Entities() b
                           where b.Animal = a.Animal
                             and b.Date < a.Date
                       )
                       then 'Y'
                       else 'N'
                   end as HasEarlierAnimalMatch
            from #A.Entities() a
            """);

        _predicateCompositeRangeMark = CreateProfileQuery("""
            select a.FirstName,
                   case
                       when exists (
                           select b.Email from #B.Entities() b
                           where b.Animal = a.Animal
                             and b.Gender = a.Gender
                             and b.Date < a.Date
                       )
                       then 'Y'
                       else 'N'
                   end as HasEarlierPeerMatch
            from #A.Entities() a
            """);

        _correlatedApplySetOperator = CreateProfileQuery("""
            select a.FirstName, d.Email
            from #A.Entities() a
            cross apply (
                select b.Email, b.Animal from #B.Entities() b
                where b.Animal = a.Animal
                union (Email, Animal)
                select c.Email, c.Animal from #C.Entities() c
                where c.Animal = a.Animal
            ) d
            """);
    }

    [Benchmark(Baseline = true)]
    public Table ExistingIn()
    {
        return _existingIn.Run();
    }

    [Benchmark]
    public Table CorrelatedInSemiJoin()
    {
        return _correlatedIn.Run();
    }

    [Benchmark]
    public Table CorrelatedNotInAntiSemiJoin()
    {
        return _correlatedNotIn.Run();
    }

    [Benchmark]
    public Table CorrelatedExistsSemiJoin()
    {
        return _correlatedExists.Run();
    }

    [Benchmark]
    public Table CorrelatedNotExistsAntiSemiJoin()
    {
        return _correlatedNotExists.Run();
    }

    [Benchmark]
    public Table ScalarAggregateDecorrelated()
    {
        return _scalarAggregate.Run();
    }

    [Benchmark]
    public Table ScalarNonAggregateHashSingle()
    {
        return _scalarNonAggregate.Run();
    }

    [Benchmark]
    public Table ScalarPartitionedTopOffset()
    {
        return _scalarPartitionedTopOffset.Run();
    }

    [Benchmark]
    public Table ScalarGrouped()
    {
        return _scalarGrouped.Run();
    }

    [Benchmark]
    public Table ScalarCorrelatedSetOperator()
    {
        return _scalarCorrelatedSetOperator.Run();
    }

    [Benchmark]
    public Table ScalarSubqueryJoinOn()
    {
        return _scalarSubqueryJoinOn.Run();
    }

    [Benchmark]
    public Table QuantifiedAllAntiSemiJoin()
    {
        return _quantifiedAll.Run();
    }

    [Benchmark]
    public Table DerivedCrossApplyJoin()
    {
        return _derivedCrossApply.Run();
    }

    [Benchmark]
    public Table DerivedCrossApplySelectiveJoin()
    {
        return _derivedCrossApplySelective.Run();
    }

    [Benchmark]
    public Table CompositeStringKeySemiJoin()
    {
        return _compositeStringKey.Run();
    }

    [Benchmark]
    public Table CompositeStringDecimalKeySemiJoin()
    {
        return _compositeStringDecimalKey.Run();
    }

    [Benchmark]
    public Table PredicateExpressionFallback()
    {
        return _predicateExpressionFallback.Run();
    }

    [Benchmark]
    public Table PredicateRangeMark()
    {
        return _predicateRangeMark.Run();
    }

    [Benchmark]
    public Table PredicatePartitionedRangeMark()
    {
        return _predicatePartitionedRangeMark.Run();
    }

    [Benchmark]
    public Table PredicateCompositeRangeMark()
    {
        return _predicateCompositeRangeMark.Run();
    }

    [Benchmark]
    public Table CorrelatedApplySetOperator()
    {
        return _correlatedApplySetOperator.Run();
    }

    private CompiledQuery CreateProfileQuery(string script)
    {
        var sources = new Dictionary<string, IEnumerable<ProfileEntity>>
        {
            { "#A", _left },
            { "#B", _right },
            { "#C", _third }
        };

        return CreateForProfilesWithOptions(script, sources, new CompilationOptions(ParallelizationMode.None));
    }

    private CompiledQuery CreateCountryQuery(string script)
    {
        var sources = new Dictionary<string, IEnumerable<CountryEntity>>
        {
            { "#A", _countryLeft },
            { "#B", _countryRight },
            { "#C", _countryThird }
        };

        return CreateForCountryWithOptions(script, sources, new CompilationOptions(ParallelizationMode.None));
    }
}
