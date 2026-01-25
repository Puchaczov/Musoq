#nullable enable

using System;

namespace Musoq.Parser.Nodes.InterpretationSchema;

/// <summary>
///     Represents a single case in a text switch field.
///     Each case has a pattern (or null for default) and a type to parse if the pattern matches.
/// </summary>
public class TextSwitchCaseNode
{
    /// <summary>
    ///     Creates a new switch case with a pattern.
    /// </summary>
    /// <param name="pattern">The regex pattern for lookahead matching, or null for default case.</param>
    /// <param name="typeName">The type/schema name to parse when this case matches.</param>
    public TextSwitchCaseNode(string? pattern, string typeName)
    {
        Pattern = pattern;
        TypeName = typeName ?? throw new ArgumentNullException(nameof(typeName));
    }

    /// <summary>
    ///     Gets the regex pattern for lookahead matching.
    ///     Null indicates this is the default case (_).
    /// </summary>
    public string? Pattern { get; }

    /// <summary>
    ///     Gets the type/schema name to parse when this case matches.
    /// </summary>
    public string TypeName { get; }

    /// <summary>
    ///     Gets whether this is the default case.
    /// </summary>
    public bool IsDefault => Pattern == null;

    /// <inheritdoc />
    public override string ToString()
    {
        if (IsDefault)
            return $"_ => {TypeName}";

        return $"pattern '{EscapeString(Pattern!)}' => {TypeName}";
    }

    private static string EscapeString(string value)
    {
        return value
            .Replace("\\", "\\\\")
            .Replace("'", "\\'");
    }
}
