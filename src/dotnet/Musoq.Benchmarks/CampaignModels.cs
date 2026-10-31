using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Musoq.Benchmarks;

public static class ArtifactJson
{
    public static JsonSerializerOptions Options { get; } = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Converters = { new OrdinalStringDictionaryConverter() }
    };
    public static void Write<T>(string path, T value) => WriteText(path, JsonSerializer.Serialize(value, Options) + "\n");

    public static void WriteText(string path, string text)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
        {
            var bytes = Encoding.UTF8.GetBytes(text);
            stream.Write(bytes);
            stream.Flush(flushToDisk: true);
        }
        File.Move(temporary, path, overwrite: true);
    }
    public static T Read<T>(string path) => JsonSerializer.Deserialize<T>(File.ReadAllText(path), Options)
        ?? throw new InvalidDataException($"Empty artifact: {path}");
}

internal sealed class OrdinalStringDictionaryConverter : JsonConverter<SortedDictionary<string, string>>
{
    public override SortedDictionary<string, string> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var values = JsonSerializer.Deserialize<Dictionary<string, string>>(ref reader, options)
            ?? throw new InvalidDataException("Expected a string dictionary.");
        return new SortedDictionary<string, string>(values, StringComparer.Ordinal);
    }

    public override void Write(Utf8JsonWriter writer, SortedDictionary<string, string> value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        foreach (var pair in value.OrderBy(pair => pair.Key, StringComparer.Ordinal))
            writer.WriteString(pair.Key, pair.Value);
        writer.WriteEndObject();
    }
}

public sealed record SourceStamp(string Commit, string? Branch, string[] Tags, bool Dirty, string? CiBranch)
{
    public bool Detached => Branch is null;
}
public sealed record HardwareStamp(string Profile, string CpuModel, int? PhysicalCores, int LogicalCores,
    int EffectiveProcessors, long MemoryBytes, string Architecture, string[] Unavailable);
public sealed record EnvironmentStamp(string Os, string Runtime, string Sdk, string BenchmarkDotNet,
    bool ServerGc, bool ConcurrentGc, int MaxDegreeOfParallelism, string Mode, string Job, bool InProcess,
    SortedDictionary<string, string>? RuntimeVariables = null);
public sealed record BuildStamp(string EngineVersion, string SuiteRevision,
    string EngineInformationalVersion = "", string SuiteInformationalVersion = "");
public sealed record RunProvenance(SourceStamp Source, HardwareStamp Hardware, EnvironmentStamp Environment, BuildStamp Build);

public sealed record CaseDefinition(string Id, string ScenarioId, string Category, string Phase, string ResultApi,
    string Size, int InputCount, string Sql, string Specification, string Consumption,
    string JobSettings = "", string Distribution = "", string InputUnit = "rows",
    SortedDictionary<string, string>? Arguments = null);

public sealed class RunManifest
{
    public string SchemaVersion { get; init; } = "3";
    public CampaignOptions? Invocation { get; init; }
    public required string SuiteVersion { get; init; }
    public required string RunId { get; init; }
    public required RunProvenance Provenance { get; init; }
    public required CaseDefinition[] Cases { get; init; }
    public DateTimeOffset StartedUtc { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? FinishedUtc { get; set; }
    public string Status { get; set; } = "running";
    public string Preflight { get; set; } = "not-run";
    public string? LastError { get; set; }
    public DateTimeOffset? RepackedUtc { get; init; }
}

public sealed record Measurement(string CaseId, DateTimeOffset StartedUtc, DateTimeOffset FinishedUtc, string Status,
    string? Error, double? MeanNanoseconds = null, double? MedianNanoseconds = null, double? StandardDeviationNanoseconds = null,
    double? ConfidenceLowerNanoseconds = null, double? ConfidenceUpperNanoseconds = null, int SampleCount = 0,
    long? AllocatedBytesPerOperation = null, int? Gen0Collections = null, int? Gen1Collections = null,
    int? Gen2Collections = null, long? GcOperations = null, string[]? RawReports = null,
    string? AttemptId = null);

public sealed record AttemptStart(string Id, string CaseId, DateTimeOffset StartedUtc);
public sealed record CampaignCompletion(string SchemaVersion, string RunId, DateTimeOffset CompletedUtc,
    int Expected, int Completed);
public sealed record CampaignSession(int ProcessId, DateTime ProcessStartedUtc, string Machine,
    DateTimeOffset StartedUtc, string Status = "running");
