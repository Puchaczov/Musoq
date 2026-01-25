#nullable enable
using System;
using System.Linq;
using System.Text;

namespace Musoq.Parser.Nodes.InterpretationSchema;

/// <summary>
///     Represents a binary schema definition.
///     Binary schemas define how to interpret raw byte sequences.
/// </summary>
/// <example>
///     binary Header {
///     Magic:   int le,
///     Version: short le,
///     Length:  int le,
///     IsValid: Magic = 0x12345678   -- Computed field (no bytes consumed)
///     }
///     binary LengthPrefixed&lt;T&gt; {
///     Length: int le,
///     Data:   T[Length]
///     }
/// </example>
public class BinarySchemaNode : Node
{
    /// <summary>
    ///     Creates a new binary schema definition.
    /// </summary>
    /// <param name="name">The schema name.</param>
    /// <param name="fields">The field definitions (parsed and/or computed).</param>
    /// <param name="extends">Optional base schema name for inheritance.</param>
    /// <param name="typeParameters">Optional generic type parameters (e.g., T, U).</param>
    public BinarySchemaNode(string name, SchemaFieldNode[] fields, string? extends = null,
        string[]? typeParameters = null)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Fields = fields ?? throw new ArgumentNullException(nameof(fields));
        Extends = extends;
        TypeParameters = typeParameters ?? [];

        var fieldsId = fields.Length == 0
            ? string.Empty
            : string.Concat(fields.Select(f => f.Id));
        var extendsId = extends ?? string.Empty;
        var typeParamsId = TypeParameters.Length == 0
            ? string.Empty
            : "<" + string.Join(",", TypeParameters) + ">";
        Id = $"{nameof(BinarySchemaNode)}{name}{typeParamsId}{extendsId}{fieldsId}";
    }

    /// <summary>
    ///     Gets the schema name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    ///     Gets the field definitions (both parsed and computed).
    /// </summary>
    public SchemaFieldNode[] Fields { get; }

    /// <summary>
    ///     Gets the optional base schema name for inheritance.
    /// </summary>
    public string? Extends { get; }

    /// <summary>
    ///     Gets the generic type parameters (e.g., ["T", "U"] for binary Schema&lt;T, U&gt;).
    ///     Empty array if the schema is not generic.
    /// </summary>
    public string[] TypeParameters { get; }

    /// <summary>
    ///     Gets whether this schema is a generic schema (has type parameters).
    /// </summary>
    public bool IsGeneric => TypeParameters.Length > 0;

    /// <inheritdoc />
    /// <remarks>
    ///     Binary schemas generate a class type at compilation time.
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
        builder.Append("binary ");
        builder.Append(Name);

        if (TypeParameters.Length > 0)
        {
            builder.Append('<');
            builder.Append(string.Join(", ", TypeParameters));
            builder.Append('>');
        }

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
