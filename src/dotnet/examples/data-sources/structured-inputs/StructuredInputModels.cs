using System.Collections.Generic;

namespace Musoq.Examples.DataSources.StructuredInputs;

public readonly record struct PatternInput(
    string Id,
    string Pattern,
    string Mode = "literal");

public readonly record struct WindowInput(int Before, int After);

public readonly record struct OptionsInput(
    bool Enabled,
    int[] Codes,
    WindowInput Window);

public readonly record struct WeightedInput(
    int Value,
    decimal Weight,
    bool Enabled = true);

/// <summary>Static complex metadata exposed for DESC COLUMN examples.</summary>
public sealed class PatternMatchMetadata
{
    private static readonly PatternMatchMetadata EmptyValue = new();

    /// <summary>Returns the immutable metadata instance used by result rows.</summary>
    public static PatternMatchMetadata Empty => EmptyValue;

    /// <summary>Describes the structural pattern inputs accepted by <c>match</c>.</summary>
    public IReadOnlyList<PatternInput> Patterns { get; } = Array.Empty<PatternInput>();
}

public readonly record struct PatternMatchRow(
    string PatternId,
    string MatchText,
    int Offset)
{
    /// <summary>Complex metadata used by the maintained DESC COLUMN example.</summary>
    public PatternMatchMetadata Metadata => PatternMatchMetadata.Empty;
}

public readonly record struct ConfigureRow(
    bool Enabled,
    int[] Codes,
    int Before,
    int After);

public readonly record struct NumberRow(int Value);

public readonly record struct MatrixValueRow(
    int Value,
    int Row,
    int Column);

public readonly record struct WeightedRow(
    int Value,
    decimal Weight,
    bool Enabled);

/// <summary>Rows returned by the source that demonstrates typed overload selection.</summary>
public readonly record struct OverloadRow(string Kind, int Value, int Count);

/// <summary>Rows returned by the declaration-versus-constructor default probe.</summary>
public readonly record struct DefaultConflictRow(string Id, string Mode);

/// <summary>Rows returned by the source-context identity probe.</summary>
public readonly record struct ContextProbeRow(string QueryId, string SourceContextId, string Alias);

/// <summary>Rows returned by the strict input-limit probe.</summary>
public readonly record struct StrictLimitRow(int Value);

/// <summary>Mutable receiver used to prove per-invocation ownership.</summary>
public sealed class MutableInput
{
    public MutableInput(int value) => Value = value;

    public int Value { get; set; }
}

/// <summary>Rows returned after a source mutates its private receiving value.</summary>
public readonly record struct MutableProbeRow(int Value);

/// <summary>First of two intentionally shape-equivalent ambiguity inputs.</summary>
public readonly record struct AmbiguousInputA(string Value, string Extra = "a");

/// <summary>Second of two intentionally shape-equivalent ambiguity inputs.</summary>
public readonly record struct AmbiguousInputB(string Value, string Other = "b");

/// <summary>Row exposing a collection-typed column for scalar/CTE ambiguity coverage.</summary>
public readonly record struct CollectionColumnRow(IReadOnlyList<PatternInput> Items);

/// <summary>Deterministic counters used by the maintained example and its tests.</summary>
public static class StructuredInputSourceCounters
{
    private static int _matchConstructed;
    private static int _configureConstructed;
    private static int _numbersConstructed;
    private static int _matrixConstructed;
    private static int _weightedConstructed;
    private static int _overloadConstructed;
    private static int _defaultConflictConstructed;
    private static int _contextProbeConstructed;
    private static int _strictConstructed;
    private static int _mutableConstructed;
    private static int _throwingConstructed;

    public static int MatchConstructed => Volatile.Read(ref _matchConstructed);
    public static int ConfigureConstructed => Volatile.Read(ref _configureConstructed);
    public static int NumbersConstructed => Volatile.Read(ref _numbersConstructed);
    public static int MatrixConstructed => Volatile.Read(ref _matrixConstructed);
    public static int WeightedConstructed => Volatile.Read(ref _weightedConstructed);
    public static int OverloadConstructed => Volatile.Read(ref _overloadConstructed);
    public static int DefaultConflictConstructed => Volatile.Read(ref _defaultConflictConstructed);
    public static int ContextProbeConstructed => Volatile.Read(ref _contextProbeConstructed);
    public static int StrictConstructed => Volatile.Read(ref _strictConstructed);
    public static int MutableConstructed => Volatile.Read(ref _mutableConstructed);
    public static int ThrowingConstructed => Volatile.Read(ref _throwingConstructed);

    public static void Reset()
    {
        Interlocked.Exchange(ref _matchConstructed, 0);
        Interlocked.Exchange(ref _configureConstructed, 0);
        Interlocked.Exchange(ref _numbersConstructed, 0);
        Interlocked.Exchange(ref _matrixConstructed, 0);
        Interlocked.Exchange(ref _weightedConstructed, 0);
        Interlocked.Exchange(ref _overloadConstructed, 0);
        Interlocked.Exchange(ref _defaultConflictConstructed, 0);
        Interlocked.Exchange(ref _contextProbeConstructed, 0);
        Interlocked.Exchange(ref _strictConstructed, 0);
        Interlocked.Exchange(ref _mutableConstructed, 0);
        Interlocked.Exchange(ref _throwingConstructed, 0);
    }

    internal static void MatchCreated() => Interlocked.Increment(ref _matchConstructed);
    internal static void ConfigureCreated() => Interlocked.Increment(ref _configureConstructed);
    internal static void NumbersCreated() => Interlocked.Increment(ref _numbersConstructed);
    internal static void MatrixCreated() => Interlocked.Increment(ref _matrixConstructed);
    internal static void WeightedCreated() => Interlocked.Increment(ref _weightedConstructed);
    internal static void OverloadCreated() => Interlocked.Increment(ref _overloadConstructed);
    internal static void DefaultConflictCreated() => Interlocked.Increment(ref _defaultConflictConstructed);
    internal static void ContextProbeCreated() => Interlocked.Increment(ref _contextProbeConstructed);
    internal static void StrictCreated() => Interlocked.Increment(ref _strictConstructed);
    internal static void MutableCreated() => Interlocked.Increment(ref _mutableConstructed);
    internal static void ThrowingCreated() => Interlocked.Increment(ref _throwingConstructed);
}
