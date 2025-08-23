using Newtonsoft.Json;

namespace Musoq.Benchmarks.Performance.Models;

public class BenchmarkResult
{
    [JsonProperty("method")]
    public string Method { get; set; } = string.Empty;

    [JsonProperty("mean")]
    public double Mean { get; set; }

    [JsonProperty("error")]
    public double Error { get; set; }

    [JsonProperty("stdDev")]
    public double StdDev { get; set; }

    [JsonProperty("min")]
    public double Min { get; set; }

    [JsonProperty("max")]
    public double Max { get; set; }

    [JsonProperty("median")]
    public double Median { get; set; }

    [JsonProperty("unit")]
    public string Unit { get; set; } = "ms";

    [JsonProperty("timestamp")]
    public DateTime Timestamp { get; set; }

    [JsonProperty("commitHash")]
    public string CommitHash { get; set; } = string.Empty;

    [JsonProperty("branch")]
    public string Branch { get; set; } = string.Empty;

    [JsonProperty("environment")]
    public EnvironmentInfo Environment { get; set; } = new();
}

public class EnvironmentInfo
{
    [JsonProperty("runtime")]
    public string Runtime { get; set; } = string.Empty;

    [JsonProperty("platform")]
    public string Platform { get; set; } = string.Empty;

    [JsonProperty("processorName")]
    public string ProcessorName { get; set; } = string.Empty;

    [JsonProperty("logicalCores")]
    public int LogicalCores { get; set; }

    [JsonProperty("physicalCores")]
    public int PhysicalCores { get; set; }

    [JsonProperty("gcMode")]
    public string GcMode { get; set; } = string.Empty;
}