using BenchmarkDotNet.Attributes;
using Musoq.Benchmarks.Components;
using Musoq.Benchmarks.Helpers;
using Musoq.Benchmarks.Schema.Profiles;
using Musoq.Evaluator;
using Musoq.Evaluator.Tables;

namespace Musoq.Benchmarks;

[MemoryDiagnoser]
public class SetOperationBenchmark : BenchmarkBase
{
    private readonly CompiledQuery _exceptSingleKey;
    private readonly CompiledQuery _intersectSingleKey;
    private readonly CompiledQuery _unionAllSingleKey;
    private readonly CompiledQuery _unionCompositeKey;
    private readonly CompiledQuery _unionSingleKey;

    public SetOperationBenchmark()
    {
        var contentPath = Path.Combine(AppContext.BaseDirectory, "Data", "profiles.csv");
        var profiles = DataHelpers.ReadProfiles(contentPath).ToList();
        var firstSource = profiles.Take(6000).ToList();
        var secondSource = profiles.Skip(3000).Take(6000).ToList();
        var sources = new Dictionary<string, IEnumerable<ProfileEntity>>
        {
            { "#A", firstSource },
            { "#B", secondSource }
        };
        var options = new CompilationOptions();

        _unionAllSingleKey = CreateForProfilesWithOptions(
            "select Email from #A.Entities() union all (Email) select Email from #B.Entities()",
            sources,
            options);

        _unionSingleKey = CreateForProfilesWithOptions(
            "select Email from #A.Entities() union (Email) select Email from #B.Entities()",
            sources,
            options);

        _unionCompositeKey = CreateForProfilesWithOptions(
            "select Gender, Animal from #A.Entities() union (Gender, Animal) select Gender, Animal from #B.Entities()",
            sources,
            options);

        _exceptSingleKey = CreateForProfilesWithOptions(
            "select Email from #A.Entities() except (Email) select Email from #B.Entities()",
            sources,
            options);

        _intersectSingleKey = CreateForProfilesWithOptions(
            "select Email from #A.Entities() intersect (Email) select Email from #B.Entities()",
            sources,
            options);
    }

    [Benchmark]
    public Table UnionAllSingleKey()
    {
        return _unionAllSingleKey.Run();
    }

    [Benchmark]
    public Table UnionSingleKey()
    {
        return _unionSingleKey.Run();
    }

    [Benchmark]
    public Table UnionCompositeKey()
    {
        return _unionCompositeKey.Run();
    }

    [Benchmark]
    public Table ExceptSingleKey()
    {
        return _exceptSingleKey.Run();
    }

    [Benchmark]
    public Table IntersectSingleKey()
    {
        return _intersectSingleKey.Run();
    }
}
