using System.Reflection;
using System.Runtime;
using System.Runtime.InteropServices;
using Musoq.Converter;
using Musoq.Benchmarks.Scenarios;

namespace Musoq.Benchmarks;

public sealed record MeasuredProcessStamp(int ProcessId, DateTimeOffset CapturedUtc, string Runtime, bool ServerGc,
    int EffectiveProcessors, string EngineInformationalVersion, string SuiteInformationalVersion,
    SourceStamp? Source = null, string ScenarioId = "", string Sql = "", int InputCount = 0);

public static class MeasuredProcess
{
    public const string OutputVariable = "MUSOQ_BENCHMARK_PROCESS_METADATA";

    public static void Record(Scenario scenario, int inputCount)
    {
        var directory = Environment.GetEnvironmentVariable(OutputVariable);
        if (string.IsNullOrEmpty(directory))
            return;
        var stamp = Capture() with { ScenarioId = scenario.Id, Sql = scenario.Sql, InputCount = inputCount };
        ArtifactJson.Write(Path.Combine(directory, $"measured-process-{stamp.ProcessId}-{Guid.NewGuid():N}.json"), stamp);
    }

    public static MeasuredProcessStamp Capture()
    {
        var root = ProvenanceCollector.RepositoryRoot();
        return new(Environment.ProcessId, DateTimeOffset.UtcNow,
            RuntimeInformation.FrameworkDescription, GCSettings.IsServerGC, Environment.ProcessorCount,
            InformationalVersion(typeof(InstanceCreator).Assembly), InformationalVersion(typeof(MeasuredProcess).Assembly),
            ProvenanceCollector.Source(root));
    }

    private static string InformationalVersion(Assembly assembly) => assembly
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? assembly.GetName().Version!.ToString();
}
