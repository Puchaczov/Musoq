#nullable enable
using System;
using System.Linq;
using System.Text;

namespace Musoq.Parser.Nodes.InterpretationSchema;

/// <summary>
///     Represents a text schema definition.
///     Text schemas define how to interpret character sequences using patterns.
/// </summary>
/// <example>
///     text LogLine {
///     Timestamp: pattern '\d{4}-\d{2}-\d{2}',
///     Level:     token whitespace,
///     Message:   rest
///     }
/// </example>
public class TextSchemaNode : Node
{
    /// <summary>
    ///     Creates a new text schema definition.
    /// </summary>
    /// <param name="name">The schema name.</param>
    /// <param name="fields">The field definitions.</param>
    /// <param name="extends">Optional base schema name for inheritance.</param>
    public TextSchemaNode(string name, TextFieldDefinitionNode[] fields, string? extends = null)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Fields = fields ?? throw new ArgumentNullException(nameof(fields));
        Extends = extends;

        var fieldsId = fields.Length == 0
            ? string.Empty
            : string.Concat(fields.Select(f => f.Id));
        var extendsId = extends ?? string.Empty;
        Id = $"{nameof(TextSchemaNode)}{name}{extendsId}{fieldsId}";
    }

    /// <summary>
    ///     Gets the schema name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    ///     Gets the field definitions.
    /// </summary>
    public TextFieldDefinitionNode[] Fields { get; }

    /// <summary>
    ///     Gets the optional base schema name for inheritance.
    /// </summary>
    public string? Extends { get; }

    /// <inheritdoc />
    /// <remarks>
    ///     Text schemas generate a class type at compilation time.
    /// </remarks>
    public override Type ReturnType => typeof(object);

    /// <inheritdoc />
    public override string Id { get; }

    /// <inheritdoc />
    public override void Accept(IExpressionVisitor visitor)
    {
        visitor.Visit(this);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        var builder = new StringBuilder();
        builder.Append("text ");
        builder.Append(Name);

        if (!string.IsNullOrEmpty(Extends))
        {
            builder.Append(" extends ");
            builder.Append(Extends);
        }

        builder.AppendLine(" {");

        for (var i = 0; i < Fields.Length; i++)
        {
            builder.Append("    ");
            builder.Append(Fields[i].ToString());

            if (i < Fields.Length - 1) builder.Append(',');

            builder.AppendLine();
        }

        builder.Append('}');

        return builder.ToString();
    }
}
