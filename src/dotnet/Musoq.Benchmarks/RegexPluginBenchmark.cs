using BenchmarkDotNet.Attributes;
using Musoq.Benchmarks.Components;
using Musoq.Converter;
using Musoq.Evaluator;

namespace Musoq.Benchmarks;

/// <summary>
///     Benchmark to measure performance of regex-based plugin methods: Match, RegexReplace, RegexMatches.
///     Tests the impact of regex caching for repeated pattern usage.
/// </summary>
[ShortRunJob]
[MemoryDiagnoser]
public class RegexPluginBenchmark
{
    private readonly ILoggerResolver _loggerResolver = new BenchmarkLoggerResolver();
    private CompiledQuery _baselineQuery = null!;
    private CompiledQuery _matchQuery = null!;
    private CompiledQuery _regexMatchesQuery = null!;
    private CompiledQuery _regexReplaceQuery = null!;

    [GlobalSetup]
    public void Setup()
    {
        var testData = Enumerable.Range(0, 1000).Select(i => new TestEntity
        {
            Id = i,
            Name = $"User{i}",
            City = $"City{i % 50}",
            Email = $"user{i}@example.com",
            Description = $"This is item number {i} with value {i * 10} and code ABC-{i:D4}"
        }).ToList();

        var schemaProvider = new TestSchemaProvider(testData);


        _baselineQuery = InstanceCreator.CompileForExecution(
            @"select Name from #test.entities() where Name = 'User500'",
            Guid.NewGuid().ToString(),
            schemaProvider,
            _loggerResolver);


        _matchQuery = InstanceCreator.CompileForExecution(
            @"select Name from #test.entities() where Match('\d{3}', Description)",
            Guid.NewGuid().ToString(),
            schemaProvider,
            _loggerResolver);


        _regexReplaceQuery = InstanceCreator.CompileForExecution(
            @"select RegexReplace(Description, 'ABC-\d{4}', 'CODE-XXXX') from #test.entities()",
            Guid.NewGuid().ToString(),
            schemaProvider,
            _loggerResolver);


        _regexMatchesQuery = InstanceCreator.CompileForExecution(
            @"select RegexMatches('\d+', Description) from #test.entities()",
            Guid.NewGuid().ToString(),
            schemaProvider,
            _loggerResolver);
    }

    [Benchmark(Baseline = true)]
    public void Baseline_EqualityFilter_1000Rows()
    {
        _baselineQuery.Run();
    }

    [Benchmark]
    public void Match_PatternMatching_1000Rows()
    {
        _matchQuery.Run();
    }

    [Benchmark]
    public void RegexReplace_PatternReplacement_1000Rows()
    {
        _regexReplaceQuery.Run();
    }

    [Benchmark]
    public void RegexMatches_FindAllMatches_1000Rows()
    {
        _regexMatchesQuery.Run();
    }
}
