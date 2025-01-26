using BenchmarkDotNet.Attributes;
using Musoq.Benchmarks.Helpers;
using Musoq.Benchmarks.Schema.Country;
using Musoq.Benchmarks.Schema.Profiles;
using Musoq.Evaluator;
using Musoq.Evaluator.Tables;

namespace Musoq.Benchmarks.Components;

public class ExecutionBenchmark : BenchmarkBase
{
    private readonly CompiledQuery _queryForComputeCountriesWithParallelization;
    private readonly CompiledQuery _queryForComputeCountriesWithoutParallelization;
    private readonly CompiledQuery _queryForComputeProfilesWithParallelization;
    private readonly CompiledQuery _queryForComputeProfilesWithoutParallelization;
    
    public ExecutionBenchmark()
    {
        _queryForComputeCountriesWithParallelization = CreateCompiledQueryWithOptions(new CompilationOptions(ParallelizationMode.Full));
        _queryForComputeCountriesWithoutParallelization = CreateCompiledQueryWithOptions(new CompilationOptions(ParallelizationMode.None));
        _queryForComputeProfilesWithParallelization = ComputeProfilesWithOptions(new CompilationOptions(ParallelizationMode.Full));
        _queryForComputeProfilesWithoutParallelization = ComputeProfilesWithOptions(new CompilationOptions(ParallelizationMode.None));
    }

    // [Benchmark]
    // public Table ComputeSimpleSelect_WithParallelization_1MbOfData_Countries()
    // {
    //     return _queryForComputeCountriesWithParallelization.Run();
    // }
    //
    // [Benchmark]
    // public Table ComputeSimpleSelect_WithoutParallelization_1MbOfData_Countries()
    // {
    //     return _queryForComputeCountriesWithoutParallelization.Run();
    // }
    
    [Benchmark]
    public Table ComputeSimpleSelect_WithParallelization_10MbOfData_Profiles()
    {
        return _queryForComputeProfilesWithParallelization.Run();
    }
    
    [Benchmark]
    public Table ComputeSimpleSelect_WithoutParallelization_10MbOfData_Profiles()
    {
        return _queryForComputeProfilesWithoutParallelization.Run();
    }
    
    [GlobalCleanup]
    public void Cleanup()
    {
    }

    private CompiledQuery CreateCompiledQueryWithOptions(CompilationOptions compilationOptions)
    {
        var script = "select City, Country, Population from #A.Entities() where Population > 500000";
        var contentPath = Path.Combine(AppContext.BaseDirectory, "Data", "countries.json");
        var data = DataHelpers.ParseCountryData(contentPath);
        var sources = new Dictionary<string, IEnumerable<CountryEntity>>
        {
            {"#A", data}
        };
        
        return CreateForCountryWithOptions(script, sources, compilationOptions);
    }

    private CompiledQuery ComputeProfilesWithOptions(CompilationOptions compilationOptions)
    {
        const string script = "select FirstName, LastName, Email, Gender, IpAddress, Date, Image, Animal, Avatar from #A.Entities() where Email like '%.co.uk'";
        var contentPath = Path.Combine(AppContext.BaseDirectory, "Data", "profiles.csv");
        var data = DataHelpers.ReadProfiles(contentPath);
        var sources = new Dictionary<string, IEnumerable<ProfileEntity>>
        {
            {"#A", data}
        };
        
        return CreateForProfilesWithOptions(script, sources, compilationOptions);
    }
}