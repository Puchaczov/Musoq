namespace Musoq.Schema.StructuralInputs;

/// <summary>Measured resource usage for one structural input root.</summary>
public readonly record struct StructuralInputMetrics(
    int MaxDepth,
    long NodeCount,
    long StringBytes)
{
    public static StructuralInputMetrics Empty { get; } = new(0, 0, 0);
}

/// <summary>Identifies the resource bound that rejected a structural input.</summary>
public enum StructuralInputLimitKind
{
    Depth,
    Nodes,
    StringBytes
}
