namespace Musoq.Benchmarks;

/// <summary>
///     Entity with binary content for benchmarks.
/// </summary>
public class BenchmarkBinaryEntity
{
    public static readonly IReadOnlyDictionary<string, int> NameToIndexMap = new Dictionary<string, int>
    {
        { nameof(Name), 0 },
        { nameof(Content), 1 }
    };

    public static readonly IReadOnlyDictionary<int, Func<BenchmarkBinaryEntity, object>> IndexToObjectAccessMap =
        new Dictionary<int, Func<BenchmarkBinaryEntity, object>>
        {
            { 0, e => e.Name },
            { 1, e => e.Content }
        };

    public string Name { get; set; } = string.Empty;
    public byte[] Content { get; set; } = Array.Empty<byte>();
}
