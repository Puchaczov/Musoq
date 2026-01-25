using System;

namespace Musoq.Parser.Nodes.InterpretationSchema;

/// <summary>
///     Text field processing modifiers applied after capture.
///     Can be combined as flags.
/// </summary>
[Flags]
public enum TextFieldModifier
{
    /// <summary>
    ///     No modifiers applied.
    /// </summary>
    None = 0,

    /// <summary>
    ///     Remove leading and trailing whitespace.
    /// </summary>
    Trim = 1,

    /// <summary>
    ///     Remove trailing whitespace only.
    /// </summary>
    RTrim = 2,

    /// <summary>
    ///     Remove leading whitespace only.
    /// </summary>
    LTrim = 4,

    /// <summary>
    ///     Handle balanced/nested delimiters.
    /// </summary>
    Nested = 8,

    /// <summary>
    ///     Handle escape sequences in content.
    /// </summary>
    Escaped = 16,

    /// <summary>
    ///     Match as many as possible (default behavior).
    /// </summary>
    Greedy = 32,

    /// <summary>
    ///     Match as few as possible.
    /// </summary>
    Lazy = 64,

    /// <summary>
    ///     Convert captured content to lowercase.
    /// </summary>
    Lower = 128,

    /// <summary>
    ///     Convert captured content to uppercase.
    /// </summary>
    Upper = 256,

    /// <summary>
    ///     Optional field - missing content produces null instead of error.
    /// </summary>
    Optional = 512
}
