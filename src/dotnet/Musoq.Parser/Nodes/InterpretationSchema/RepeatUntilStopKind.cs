namespace Musoq.Parser.Nodes.InterpretationSchema;

/// <summary>
///     Identifies how a repeat-until binary field decides when to stop reading elements.
/// </summary>
public enum RepeatUntilStopKind
{
    /// <summary>
    ///     Stop when the user-supplied condition expression evaluates to true.
    ///     At least one element is always attempted (do-while semantics).
    /// </summary>
    Condition,

    /// <summary>
    ///     Stop when the current interpreter input span is exhausted.
    ///     Zero or more elements are read (while semantics); an empty input yields an empty array.
    /// </summary>
    EndOfInput
}
