namespace Musoq.Plugins;

/// <summary>
///     Describes aggregate result behavior for empty or all-null inputs.
/// </summary>
public enum AggregateEmptyResultBehavior
{
    /// <summary>The aggregate defines custom empty-input behavior.</summary>
    Custom,

    /// <summary>The aggregate returns zero for empty input.</summary>
    Zero,

    /// <summary>The aggregate returns null for empty input.</summary>
    Null
}
