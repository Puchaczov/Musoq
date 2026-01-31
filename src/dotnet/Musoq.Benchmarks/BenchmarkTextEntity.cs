namespace Musoq.Benchmarks;

/// <summary>
///     Entity with text content for benchmarks.
/// </summary>
public class BenchmarkTextEntity
{
    public static readonly IReadOnlyDictionary<string, int> NameToIndexMap = new Dictionary<string, int>
    {
        { nameof(Name), 0 },
        { nameof(Text), 1 }
    };

    public static readonly IReadOnlyDictionary<int, Func<BenchmarkTextEntity, object>> IndexToObjectAccessMap =
        new Dictionary<int, Func<BenchmarkTextEntity, object>>
        {
            { 0, e => e.Name },
            { 1, e => e.Text }
        };

    public string Name { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
}
