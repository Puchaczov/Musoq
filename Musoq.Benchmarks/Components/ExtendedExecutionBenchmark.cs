using BenchmarkDotNet.Attributes;
using Musoq.Benchmarks.Schema.Country;
using Musoq.Benchmarks.Schema.Profiles;
using Musoq.Evaluator;
using Musoq.Evaluator.Tables;
using Musoq.Benchmarks.Helpers;

namespace Musoq.Benchmarks.Components;

[MemoryDiagnoser]
public class ExtendedExecutionBenchmark : BenchmarkBase
{
    private readonly CompiledQuery _simpleSelectProfiles;
    private readonly CompiledQuery _complexJoinProfiles;
    private readonly CompiledQuery _aggregationProfiles;
    private readonly CompiledQuery _filteringProfiles;
    private readonly CompiledQuery _sortingProfiles;
    private readonly CompiledQuery _simpleSelectCountries;
    private readonly CompiledQuery _complexFilterCountries;

    public ExtendedExecutionBenchmark()
    {
        // Profile benchmarks
        var profileData = LoadProfileData();
        _simpleSelectProfiles = CreateProfileQuery(
            "select FirstName, LastName, Email from #A.Entities()",
            profileData);
            
        _complexJoinProfiles = CreateProfileQuery(
            "select FirstName, LastName, Email, Gender from #A.Entities() where Email like '%.com' and Gender = 'Male'",
            profileData);
            
        _aggregationProfiles = CreateProfileQuery(
            "select Gender, Count(*) as Total from #A.Entities() group by Gender",
            profileData);
            
        _filteringProfiles = CreateProfileQuery(
            "select FirstName, LastName from #A.Entities() where Email like '%.uk' or Email like '%.org'",
            profileData);
            
        _sortingProfiles = CreateProfileQuery(
            "select FirstName, LastName, Email from #A.Entities() order by LastName, FirstName",
            profileData);

        // Country benchmarks  
        var countryData = LoadCountryData();
        _simpleSelectCountries = CreateCountryQuery(
            "select City, Country, Population from #A.Entities()",
            countryData);
            
        _complexFilterCountries = CreateCountryQuery(
            "select City, Country, Population from #A.Entities() where Population > 1000000 and Country like '%United%'",
            countryData);
    }

    [Benchmark]
    public Table SimpleSelect_Profiles()
    {
        return _simpleSelectProfiles.Run();
    }

    [Benchmark]
    public Table ComplexFilter_Profiles()
    {
        return _complexJoinProfiles.Run();
    }

    [Benchmark]
    public Table Aggregation_Profiles()
    {
        return _aggregationProfiles.Run();
    }

    [Benchmark]
    public Table MultiConditionFilter_Profiles()
    {
        return _filteringProfiles.Run();
    }

    [Benchmark]
    public Table Sorting_Profiles()
    {
        return _sortingProfiles.Run();
    }

    [Benchmark]
    public Table SimpleSelect_Countries()
    {
        return _simpleSelectCountries.Run();
    }

    [Benchmark]
    public Table ComplexFilter_Countries()
    {
        return _complexFilterCountries.Run();
    }

    private List<ProfileEntity> LoadProfileData()
    {
        var contentPath = Path.Combine(AppContext.BaseDirectory, "Data", "profiles.csv");
        return DataHelpers.ReadProfiles(contentPath);
    }

    private List<CountryEntity> LoadCountryData()
    {
        var contentPath = Path.Combine(AppContext.BaseDirectory, "Data", "countries.json");
        return DataHelpers.ParseCountryData(contentPath);
    }

    private CompiledQuery CreateProfileQuery(string script, List<ProfileEntity> data)
    {
        var sources = new Dictionary<string, IEnumerable<ProfileEntity>>
        {
            {"#A", data}
        };
        return CreateForProfilesWithOptions(script, sources, new CompilationOptions(ParallelizationMode.Full));
    }

    private CompiledQuery CreateCountryQuery(string script, List<CountryEntity> data)
    {
        var sources = new Dictionary<string, IEnumerable<CountryEntity>>
        {
            {"#A", data}
        };
        return CreateForCountryWithOptions(script, sources, new CompilationOptions(ParallelizationMode.Full));
    }
}