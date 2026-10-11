namespace Musoq.Plugins;

public static partial class CountAllAggregateKernel
{
    /// <summary>Initializes independently owned kernel state.</summary>
    public static void Reset(ref State state) => state = default;
}

public static partial class CountNullableAggregateKernel<T> where T : struct
{
    /// <summary>Initializes independently owned kernel state.</summary>
    public static void Reset(ref State state) => state = default;
}

public static partial class CountReferenceAggregateKernel<T> where T : class
{
    /// <summary>Initializes independently owned kernel state.</summary>
    public static void Reset(ref State state) => state = default;
}
