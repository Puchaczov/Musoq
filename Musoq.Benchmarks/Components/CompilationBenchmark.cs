using BenchmarkDotNet.Attributes;
using Musoq.Benchmarks.Helpers;
using Musoq.Benchmarks.Schema.Country;
using Musoq.Benchmarks.Schema.Profiles;
using Musoq.Converter;
using Musoq.Evaluator;
using System.Diagnostics;

namespace Musoq.Benchmarks.Components;

/// <summary>
/// Benchmarks for compilation and build performance - measuring how fast Musoq can parse SQL and compile to C#
/// </summary>
[MemoryDiagnoser]
public class CompilationBenchmark : BenchmarkBase
{
    private readonly List<ProfileEntity> _profileData;
    private readonly List<CountryEntity> _countryData;
    private readonly string[] _simpleQueries;
    private readonly string[] _complexQueries;

    public CompilationBenchmark()
    {
        // Load test data
        var profileContentPath = Path.Combine(AppContext.BaseDirectory, "Data", "profiles.csv");
        var countryContentPath = Path.Combine(AppContext.BaseDirectory, "Data", "countries.json");
        
        _profileData = DataHelpers.ReadProfiles(profileContentPath);
        _countryData = DataHelpers.ParseCountryData(countryContentPath);

        // Define queries of varying complexity for testing
        _simpleQueries = new[]
        {
            "select FirstName, LastName from #A.Entities()",
            "select City, Country from #A.Entities()",
            "select * from #A.Entities()"
        };

        _complexQueries = new[]
        {
            "select FirstName, LastName, Email from #A.Entities() where Email like '%.com' and Gender = 'Male' order by LastName",
            "select Gender, Count() as Total from #A.Entities() group by Gender having Count() > 10",
            "select City, Country, Population from #A.Entities() where Population > 100000 order by Population desc"
        };
    }

    // Parsing Performance Tests - SQL to AST conversion
    [Benchmark]
    public void ParseSimpleQuery_Profiles()
    {
        foreach (var query in _simpleQueries)
        {
            MeasureParsingTime(query);
        }
    }

    [Benchmark]
    public void ParseComplexQuery_Profiles()
    {
        foreach (var query in _complexQueries)
        {
            MeasureParsingTime(query);
        }
    }

    // Compilation Performance Tests - Full SQL to executable assembly
    [Benchmark]
    public CompiledQuery CompileSimpleQuery_Profiles()
    {
        var query = "select FirstName, LastName, Email from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<ProfileEntity>>
        {
            {"#A", _profileData.Take(1000)} // Smaller dataset for compilation benchmarks
        };
        
        return CreateForProfilesWithOptions(query, sources, new CompilationOptions(ParallelizationMode.Full));
    }

    [Benchmark]
    public CompiledQuery CompileComplexQuery_Profiles()
    {
        var query = "select Gender, Count() as Total from #A.Entities() group by Gender having Count() > 10";
        var sources = new Dictionary<string, IEnumerable<ProfileEntity>>
        {
            {"#A", _profileData.Take(1000)}
        };
        
        return CreateForProfilesWithOptions(query, sources, new CompilationOptions(ParallelizationMode.Full));
    }

    [Benchmark]
    public CompiledQuery CompileAggregationQuery_Profiles()
    {
        var query = "select Gender, Count(*) as Count, FirstName from #A.Entities() where Gender = 'Male' group by Gender, FirstName order by Count desc";
        var sources = new Dictionary<string, IEnumerable<ProfileEntity>>
        {
            {"#A", _profileData.Take(1000)}
        };
        
        return CreateForProfilesWithOptions(query, sources, new CompilationOptions(ParallelizationMode.Full));
    }

    [Benchmark]
    public CompiledQuery CompileSimpleQuery_Countries()
    {
        var query = "select City, Country, Population from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<CountryEntity>>
        {
            {"#A", _countryData.Take(500)}
        };
        
        return CreateForCountryWithOptions(query, sources, new CompilationOptions(ParallelizationMode.Full));
    }

    [Benchmark]
    public CompiledQuery CompileComplexQuery_Countries()
    {
        var query = "select Country, Count(*) as Cities, Avg(Population) as AvgPop from #A.Entities() where Population > 100000 group by Country order by Cities desc";
        var sources = new Dictionary<string, IEnumerable<CountryEntity>>
        {
            {"#A", _countryData.Take(500)}
        };
        
        return CreateForCountryWithOptions(query, sources, new CompilationOptions(ParallelizationMode.Full));
    }

    // Build Performance Tests - Different parallelization modes
    [Benchmark]
    public CompiledQuery CompileWithParallelization_Full()
    {
        var query = "select FirstName, LastName from #A.Entities() where Gender = 'Male'";
        var sources = new Dictionary<string, IEnumerable<ProfileEntity>>
        {
            {"#A", _profileData.Take(1000)}
        };
        
        return CreateForProfilesWithOptions(query, sources, new CompilationOptions(ParallelizationMode.Full));
    }

    [Benchmark]
    public CompiledQuery CompileWithParallelization_None()
    {
        var query = "select FirstName, LastName from #A.Entities() where Gender = 'Male'";
        var sources = new Dictionary<string, IEnumerable<ProfileEntity>>
        {
            {"#A", _profileData.Take(1000)}
        };
        
        return CreateForProfilesWithOptions(query, sources, new CompilationOptions(ParallelizationMode.None));
    }

    private void MeasureParsingTime(string query)
    {
        // This is a simplified parsing measurement - just validates the query compiles
        // The actual parsing time is captured by BenchmarkDotNet through the compilation process
        try
        {
            var sources = new Dictionary<string, IEnumerable<ProfileEntity>>
            {
                {"#A", _profileData.Take(10)} // Minimal data for parsing tests
            };
            var compiled = CreateForProfilesWithOptions(query, sources, new CompilationOptions(ParallelizationMode.None));
            // Don't run the query, just ensure it parses and compiles
        }
        catch
        {
            // Parsing/compilation failed - this would be tracked by BenchmarkDotNet
            throw;
        }
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        TokenSource?.Dispose();
    }
}