using System.Text.RegularExpressions;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using Musoq.Benchmarks.Scenarios;

namespace Musoq.Benchmarks;

public sealed record CampaignCase(CaseDefinition Definition, BenchmarkCase Benchmark);

public static class CampaignCases
{
    public static IReadOnlyList<Type> Types => BenchmarkRegistrations.All.Select(item => item.Type).ToArray();

    public static CampaignCase[] Create(CampaignOptions options, string workingDirectory)
    {
        var previous = Environment.GetEnvironmentVariable(BenchmarkSizes.SelectionVariable);
        try
        {
            Environment.SetEnvironmentVariable(BenchmarkSizes.SelectionVariable, options.Size?.ToString(System.Globalization.CultureInfo.InvariantCulture));
            return CreateSelected(options, workingDirectory);
        }
        finally
        {
            Environment.SetEnvironmentVariable(BenchmarkSizes.SelectionVariable, previous);
        }
    }

    private static CampaignCase[] CreateSelected(CampaignOptions options, string workingDirectory)
    {
        var cases = new List<CampaignCase>();
        foreach (var type in Types)
        {
            var job = CreateJob(options, type);
            var config = ManualConfig.Create(DefaultConfig.Instance).AddJob(job)
                .AddExporter(JsonExporter.Full).WithArtifactsPath(workingDirectory);
            using var run = BenchmarkConverter.TypeToBenchmarks(type, config);
            foreach (var item in run.BenchmarksCases)
            {
                var definition = Describe(item) with { JobSettings = job.DisplayInfo };
                if (!Matches(definition.Id, options.Filter) && !Matches(type.Name, options.Filter))
                    continue;
                var caseConfig = ManualConfig.Create(item.Config).WithArtifactsPath(Path.Combine(workingDirectory, definition.Id)).CreateImmutableConfig();
                cases.Add(new CampaignCase(definition, BenchmarkCase.Create(item.Descriptor, job, item.Parameters, caseConfig)));
            }
        }
        if (cases.GroupBy(item => item.Definition.Id).Any(group => group.Count() != 1))
            throw new InvalidOperationException("Duplicate benchmark case registrations.");
        return cases.OrderBy(item => item.Definition.Id, StringComparer.Ordinal).ToArray();
    }

    public static bool Matches(string value, string pattern) => Regex.IsMatch(value,
        "^" + Regex.Escape(pattern).Replace("\\*", ".*", StringComparison.Ordinal).Replace("\\?", ".", StringComparison.Ordinal) + "$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));

    public static CaseDefinition Describe(BenchmarkCase item)
    {
        var type = item.Descriptor.Type;
        var parameter = item.Parameters.Items.FirstOrDefault(value => value.Name == "ScenarioId");
        var registration = BenchmarkRegistrations.For(type);
        var id = parameter?.Value.ToString() ?? registration.ScenarioId
            ?? throw new InvalidOperationException($"No scenario for {type.Name}.");
        var scenario = ScenarioCatalog.Get(id);
        var size = item.Parameters.Items.FirstOrDefault(value => value.Name == "Size")?.Value.ToString() ?? "ContractFixture";
        var phase = registration.Phase;
        var api = registration.ResultApi;
        var count = phase switch { "execution" => BenchmarkSizes.Count(size), _ => FixtureData.TestSize };
        var key = $"{scenario.Id}-{phase}-{api}-{size}";
        var consumption = phase switch
        {
            "execution" => "Enumerate every row and consume typed projected values; dispose table results.",
            "artifact-load" => "Load a prepared in-memory artifact and dispose the executable; no Run.",
            _ => "Compile and dispose an executable; no Run; provider and inputs prepared outside measurement. Warm means warmed process, not guaranteed cache hit."
        };
        return new CaseDefinition(key, scenario.Id, scenario.Category, phase, api, size, count,
            scenario.Sql, scenario.Specification, consumption,
            Distribution: FixtureCatalog.GetDistribution(scenario), InputUnit: scenario.InputUnit,
            Arguments: ExecutionSettings.Arguments(scenario, count));
    }

    private static Job CreateJob(CampaignOptions options, Type type)
    {
        Job job;
        if (type == typeof(ColdCompilationBenchmark))
        {
            var launches = options.Job switch { "Default" => 8, "Short" => 3, _ => 1 };
            if (options.Official)
                launches = 8;
            job = Job.Dry.WithStrategy(RunStrategy.ColdStart)
                .WithLaunchCount(launches)
                .WithWarmupCount(0).WithIterationCount(1).WithInvocationCount(1).WithUnrollFactor(1).WithId("FirstCompilation");
        }
        else if (options.Official)
            job = Job.Default.WithLaunchCount(2).WithId("OfficialThroughput");
        else
            job = options.Job switch { "Dry" => Job.Dry, "Short" => Job.ShortRun, _ => Job.Default };
        job = job.WithGcServer(true).WithGcConcurrent(true);
        if (options.InProcess)
            job = job.WithToolchain(InProcessEmitToolchain.Instance);
        return job;
    }
}
