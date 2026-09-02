using System.Text;

namespace Musoq.Parser.Nodes.InterpretationSchema;

/// <summary>
///     Represents a field definition within a text schema.
///     Text fields use pattern matching, delimiters, or fixed-width capture.
/// </summary>
public class TextFieldDefinitionNode : Node
{
    /// <summary>
    ///     Creates a new text field definition.
    /// </summary>
    /// <param name="name">The field name (use "_" for discard fields).</param>
    /// <param name="fieldType">The capture strategy type.</param>
    /// <param name="primaryValue">Primary value (pattern, delimiter, or count).</param>
    /// <param name="secondaryValue">Secondary value (end delimiter for between).</param>
    /// <param name="modifiers">Optional processing modifiers.</param>
    /// <param name="escapeCharacter">Custom escape character for escaped modifier.</param>
    /// <param name="captureGroups">Named capture groups for pattern fields.</param>
    public TextFieldDefinitionNode(
        string name,
        TextFieldType fieldType,
        string? primaryValue = null,
        string? secondaryValue = null,
        TextFieldModifier modifiers = TextFieldModifier.None,
        string? escapeCharacter = null,
        string[]? captureGroups = null)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        FieldType = fieldType;
        PrimaryValue = primaryValue;
        SecondaryValue = secondaryValue;
        Modifiers = modifiers;
        EscapeCharacter = escapeCharacter;
        CaptureGroups = captureGroups ?? Array.Empty<string>();
        SwitchCases = Array.Empty<TextSwitchCaseNode>();

        Id = $"{nameof(TextFieldDefinitionNode)}{name}{fieldType}{primaryValue}{secondaryValue}{modifiers}";
    }

    /// <summary>
    ///     Creates a new text field definition for switch fields.
    /// </summary>
    /// <param name="name">The field name (use "_" for discard fields).</param>
    /// <param name="switchCases">The switch cases for pattern-based type selection.</param>
    /// <param name="modifiers">Optional processing modifiers.</param>
    public TextFieldDefinitionNode(
        string name,
        TextSwitchCaseNode[] switchCases,
        TextFieldModifier modifiers = TextFieldModifier.None)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        FieldType = TextFieldType.Switch;
        PrimaryValue = null;
        SecondaryValue = null;
        Modifiers = modifiers;
        EscapeCharacter = null;
        CaptureGroups = Array.Empty<string>();
        SwitchCases = switchCases ?? throw new ArgumentNullException(nameof(switchCases));

        Id = $"{nameof(TextFieldDefinitionNode)}{name}{FieldType}switch{switchCases.Length}";
    }

    /// <summary>
    ///     Gets the field name. Use "_" for discard fields.
    /// </summary>
    public string Name { get; }

    /// <summary>
    ///     Gets the capture strategy type.
    /// </summary>
    public TextFieldType FieldType { get; }

    /// <summary>
    ///     Gets the primary value:
    ///     - Pattern: the regex pattern
    ///     - Literal: the exact string to match
    ///     - Until: the delimiter
    ///     - Between: the opening delimiter
    ///     - Chars: the character count (as string)
    ///     - SchemaReference: the referenced text schema name
    /// </summary>
    public string? PrimaryValue { get; }

    /// <summary>
    ///     Gets the secondary value:
    ///     - Between: the closing delimiter
    /// </summary>
    public string? SecondaryValue { get; }

    /// <summary>
    ///     Gets the processing modifiers.
    /// </summary>
    public TextFieldModifier Modifiers { get; }

    /// <summary>
    ///     Gets the custom escape character when using Escaped modifier.
    /// </summary>
    public string? EscapeCharacter { get; }

    /// <summary>
    ///     Gets the capture group names for pattern fields.
    /// </summary>
    public string[] CaptureGroups { get; }

    /// <summary>
    ///     Gets the switch cases for Switch field type.
    ///     Contains pattern + type pairs for lookahead-based type selection.
    /// </summary>
    public TextSwitchCaseNode[] SwitchCases { get; }

    /// <summary>
    ///     Gets whether this is a discard field (name is "_").
    /// </summary>
    public bool IsDiscard => Name == "_";

    /// <inheritdoc />
    public override Type ReturnType => FieldType switch
    {
        TextFieldType.Repeat => typeof(object[]),
        TextFieldType.Switch or TextFieldType.SchemaReference => typeof(object),
        _ => typeof(string)
    };

    /// <inheritdoc />
    public override string Id { get; }

    /// <inheritdoc />
    public override void Accept(IExpressionVisitor visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);
        visitor.Visit(this);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        var builder = new StringBuilder();
        builder.Append(Name);
        builder.Append(": ");

        switch (FieldType)
        {
            case TextFieldType.Pattern:
                builder.Append(System.Globalization.CultureInfo.InvariantCulture, $"pattern '{PrimaryValue}'");
                if (CaptureGroups.Length > 0) builder.Append(System.Globalization.CultureInfo.InvariantCulture, $" capture ({string.Join(", ", CaptureGroups)})");
                break;

            case TextFieldType.Literal:
                builder.Append(System.Globalization.CultureInfo.InvariantCulture, $"literal '{EscapeString(PrimaryValue ?? string.Empty)}'");
                break;

            case TextFieldType.Until:
                builder.Append(System.Globalization.CultureInfo.InvariantCulture, $"until '{EscapeString(PrimaryValue ?? string.Empty)}'");
                break;

            case TextFieldType.Between:
                builder.Append(System.Globalization.CultureInfo.InvariantCulture,
                    $"between '{EscapeString(PrimaryValue ?? string.Empty)}' '{EscapeString(SecondaryValue ?? string.Empty)}'");
                break;

            case TextFieldType.Chars:
                builder.Append(System.Globalization.CultureInfo.InvariantCulture, $"chars[{PrimaryValue}]");
                break;

            case TextFieldType.Token:
                builder.Append("token");
                break;

            case TextFieldType.Rest:
                builder.Append("rest");
                break;

            case TextFieldType.SchemaReference:
                builder.Append(PrimaryValue);
                break;

            case TextFieldType.Whitespace:
                builder.Append("whitespace");
                if (PrimaryValue is "*" or "?")
                    builder.Append(PrimaryValue);
                break;

            case TextFieldType.Repeat:
                builder.Append(System.Globalization.CultureInfo.InvariantCulture, $"repeat {PrimaryValue}");
                if (SecondaryValue != null)
                    builder.Append(System.Globalization.CultureInfo.InvariantCulture, $" until '{EscapeString(SecondaryValue)}'");
                else
                    builder.Append(" until end");
                break;

            case TextFieldType.Switch:
                builder.Append("switch { ");
                for (var i = 0; i < SwitchCases.Length; i++)
                {
                    if (i > 0) builder.Append(", ");
                    builder.Append(SwitchCases[i]);
                }

                builder.Append(" }");
                break;
        }

        AppendModifiers(builder);

        return builder.ToString();
    }

    private void AppendModifiers(StringBuilder builder)
    {
        if ((Modifiers & TextFieldModifier.Trim) != 0)
            builder.Append(" trim");
        if ((Modifiers & TextFieldModifier.RTrim) != 0)
            builder.Append(" rtrim");
        if ((Modifiers & TextFieldModifier.LTrim) != 0)
            builder.Append(" ltrim");
        if ((Modifiers & TextFieldModifier.Nested) != 0)
            builder.Append(" nested");
        if ((Modifiers & TextFieldModifier.Escaped) != 0)
        {
            builder.Append(" escaped");
            if (!string.IsNullOrEmpty(EscapeCharacter)) builder.Append(System.Globalization.CultureInfo.InvariantCulture, $" '{EscapeString(EscapeCharacter)}'");
        }

        if ((Modifiers & TextFieldModifier.Greedy) != 0)
            builder.Append(" greedy");
        if ((Modifiers & TextFieldModifier.Lazy) != 0)
            builder.Append(" lazy");
        if ((Modifiers & TextFieldModifier.Lower) != 0)
            builder.Append(" lower");
        if ((Modifiers & TextFieldModifier.Upper) != 0)
            builder.Append(" upper");
        if ((Modifiers & TextFieldModifier.Optional) != 0)
            builder.Append(" optional");
    }

    private static string EscapeString(string value)
    {
        return value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("'", "\\'", StringComparison.Ordinal)
            .Replace("\r", "\\r", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal)
            .Replace("\t", "\\t", StringComparison.Ordinal);
    }
}
