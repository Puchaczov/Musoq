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
        Body = new TextFieldDefinitionNode("Value", TextFieldType.SchemaReference,
            typeName ?? throw new ArgumentNullException(nameof(typeName)));
    }

    /// <summary>
    ///     Gets the regex pattern for lookahead matching.
    ///     Null indicates this is the default case (_).
    /// </summary>
    public string? Pattern { get; }

    /// <summary>
    ///     Gets the type/schema name to parse when this case matches.
    /// </summary>
    public string TypeName => Body.FieldType == TextFieldType.SchemaReference
        ? Body.PrimaryValue! : throw new InvalidOperationException("Only schema-reference arms have a schema name.");

    /// <summary>Gets the complete text type parsed when this arm is selected.</summary>
    public TextFieldDefinitionNode Body { get; }

    /// <summary>Creates an arm with a complete typed text body.</summary>
    public TextSwitchCaseNode(string? pattern, TextFieldDefinitionNode body)
    {
        Pattern = pattern;
        Body = body ?? throw new ArgumentNullException(nameof(body));
    }

    /// <summary>
    ///     Gets whether this is the default case.
    /// </summary>
    public bool IsDefault => Pattern == null;

    /// <inheritdoc />
    public override string ToString()
    {
        if (IsDefault)
            return $"_ => {Body.TypeDefinition}";

        return $"pattern '{EscapeString(Pattern!)}' => {Body.TypeDefinition}";
    }

    private static string EscapeString(string value)
    {
        return value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("'", "\\'", StringComparison.Ordinal);
    }
}
