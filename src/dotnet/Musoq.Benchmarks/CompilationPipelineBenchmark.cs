using BenchmarkDotNet.Attributes;
using Musoq.Benchmarks.Components;
using Musoq.Benchmarks.Schema.Country;
using Musoq.Benchmarks.Helpers;
using Musoq.Converter;
using Musoq.Evaluator;

namespace Musoq.Benchmarks;

/// <summary>
///     Benchmarks the full query compilation pipeline (parse → transform → code gen → Roslyn emit).
/// </summary>
public class CompilationPipelineBenchmark : BenchmarkBase
{
    private const string SimpleQuery =
        "select City, Country, Population from #A.Entities() where Population > 500000";

    private const string ComplexQuery =
        "select City, Country, Population, City + ' (' + Country + ')' as CityCountry from #A.Entities() where Population > 500000 group by City, Country, Population having Count(City) > 0 order by Population desc";

    private IDictionary<string, IEnumerable<CountryEntity>> _sources;

    [GlobalSetup]
    public void Setup()
    {
        var contentPath = Path.Combine(AppContext.BaseDirectory, "Data", "countries.json");
        var data = DataHelpers.ParseCountryData(contentPath);
        _sources = new Dictionary<string, IEnumerable<CountryEntity>>
        {
            { "#A", data }
        };

        // Warm up RuntimeLibraries so first-call overhead doesn't skew results
        CreateForCountryWithOptions(SimpleQuery, _sources, new CompilationOptions());
    }

    [Benchmark(Description = "Simple query compile")]
    public CompiledQuery CompileSimpleQuery_Cold()
    {
        return CreateForCountryWithOptions(SimpleQuery, _sources, new CompilationOptions());
    }

    [Benchmark(Description = "Simple query compile (repeat)")]
    public CompiledQuery CompileSimpleQuery_Warm()
    {
        CreateForCountryWithOptions(SimpleQuery, _sources, new CompilationOptions());
        return CreateForCountryWithOptions(SimpleQuery, _sources, new CompilationOptions());
    }

    [Benchmark(Description = "Complex query compile")]
    public CompiledQuery CompileComplexQuery_Cold()
    {
        return CreateForCountryWithOptions(ComplexQuery, _sources, new CompilationOptions());
    }

    [Benchmark(Description = "Complex query compile (repeat)")]
    public CompiledQuery CompileComplexQuery_Warm()
    {
        CreateForCountryWithOptions(ComplexQuery, _sources, new CompilationOptions());
        return CreateForCountryWithOptions(ComplexQuery, _sources, new CompilationOptions());
    }
}
