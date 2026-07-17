using BenchmarkDotNet.Attributes;
using Musoq.Benchmarks.Components;
using Musoq.Benchmarks.Helpers;
using Musoq.Benchmarks.Schema.Profiles;
using Musoq.Evaluator;

namespace Musoq.Benchmarks;

[MemoryDiagnoser]
[ShortRunJob]
public class SubqueryCompilationBenchmark : BenchmarkBase
{
    private const string CorrelatedInQuery = """
        select a.FirstName, a.Animal
        from #A.Entities() a
        where a.Animal in (
            select b.Animal from #B.Entities() b
            where b.Gender = a.Gender
        )
        """;

    private const string ScalarAggregateQuery = """
        select a.FirstName,
               (select Count(b.Email) from #B.Entities() b where b.Animal = a.Animal) as AnimalCount
        from #A.Entities() a
        """;

    private const string PredicateExpressionQuery = """
        select a.FirstName,
               case when exists (
                   select b.Email from #B.Entities() b where b.Animal = a.Animal
               ) then 'Y' else 'N' end as HasAnimalMatch
        from #A.Entities() a
        """;

    private const string ApplySetQuery = """
        select a.FirstName, d.Email
        from #A.Entities() a
        cross apply (
            select b.Email, b.Animal from #B.Entities() b where b.Animal = a.Animal
            union (Email, Animal)
            select c.Email, c.Animal from #C.Entities() c where c.Animal = a.Animal
        ) d
        """;

    private IDictionary<string, IEnumerable<ProfileEntity>> _sources = null!;

    [GlobalSetup]
    public void Setup()
    {
        var contentPath = Path.Combine(AppContext.BaseDirectory, "Data", "profiles.csv");
        var data = DataHelpers.ReadProfiles(contentPath).Take(100).ToArray();
        _sources = new Dictionary<string, IEnumerable<ProfileEntity>>
        {
            { "#A", data },
            { "#B", data.Where((_, index) => index % 2 == 0).ToArray() },
            { "#C", data.Where((_, index) => index % 3 == 0).ToArray() }
        };

        Compile(CorrelatedInQuery);
    }

    [Benchmark(Baseline = true)]
    public CompiledQuery CorrelatedIn() => Compile(CorrelatedInQuery);

    [Benchmark]
    public CompiledQuery ScalarAggregate() => Compile(ScalarAggregateQuery);

    [Benchmark]
    public CompiledQuery PredicateExpression() => Compile(PredicateExpressionQuery);

    [Benchmark]
    public CompiledQuery ApplySetOperator() => Compile(ApplySetQuery);

    private CompiledQuery Compile(string query) =>
        CreateForProfilesWithOptions(query, _sources, new CompilationOptions(ParallelizationMode.None));
}
