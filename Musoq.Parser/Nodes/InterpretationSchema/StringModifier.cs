using System;

namespace Musoq.Parser.Nodes.InterpretationSchema;

/// <summary>
///     String processing modifiers applied after decoding.
///     Can be combined as flags.
/// </summary>
[Flags]
public enum StringModifier
{
    /// <summary>
    ///     No modifiers applied.
    /// </summary>
    None = 0,

    /// <summary>
    ///     Remove leading and trailing whitespace/nulls.
    /// </summary>
    Trim = 1,

    /// <summary>
    ///     Remove trailing whitespace/nulls only.
    /// </summary>
    RTrim = 2,

    /// <summary>
    ///     Remove leading whitespace/nulls only.
    /// </summary>
    LTrim = 4,

    /// <summary>
    ///     String ends at first null character. Remaining bytes are consumed but ignored.
    /// </summary>
    NullTerm = 8
}
