namespace Musoq.Schema.Tests;

/// <summary>Typed declarations used to test method selection independently of SQL execution.</summary>
public static class MetadataAggregateKernel
{
    public struct State
    {
        public int Count;
    }

    public static void Reset(ref State state) => state = default;
    public static void Set(ref State state, string name) => state.Count = checked(state.Count + 1);
    public static void Set(ref State state, string name, int value) => Set(ref state, name);
    public static void Set(ref State state, string name, decimal value) => Set(ref state, name);
    public static void Set(ref State state, string name, decimal value, string format, string[] tags) => Set(ref state, name);
    public static int Get(in State state) => state.Count;
}
