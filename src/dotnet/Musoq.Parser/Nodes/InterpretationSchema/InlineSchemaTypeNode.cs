using System;
using System.Linq;
using System.Text;

namespace Musoq.Parser.Nodes.InterpretationSchema;

/// <summary>
///     Represents an inline anonymous schema type definition.
///     Used for one-off nested structures defined directly in a field type position.
/// </summary>
/// <example>
///     binary Packet {
///     Header: {           -- InlineSchemaTypeNode starts here
///     Magic: int le,
///     Version: short le
///     },                  -- InlineSchemaTypeNode ends here
///     Body: byte[64]
///     }
/// </example>
public class InlineSchemaTypeNode : TypeAnnotationNode
{
    private readonly string _id;

    /// <summary>
    ///     Creates a new inline anonymous schema type.
    /// </summary>
    /// <param name="fields">The field definitions for the inline schema.</param>
    public InlineSchemaTypeNode(SchemaFieldNode[] fields)
    {
        Fields = fields ?? throw new ArgumentNullException(nameof(fields));

        var fieldsId = fields.Length == 0
            ? string.Empty
            : string.Concat(fields.Select(f => f.Id));
        _id = $"{nameof(InlineSchemaTypeNode)}{fieldsId}";
    }

    /// <inheritdoc />
    public override string Id => _id;

    /// <inheritdoc />
    public override Type ReturnType => ClrType;

    /// <summary>
    ///     Gets the field definitions for this inline schema.
    /// </summary>
    public SchemaFieldNode[] Fields { get; }

    /// <inheritdoc />
    /// <remarks>
    ///     Inline schemas generate an anonymous type at compilation time.
    ///     The actual type is determined during code generation.
    /// </remarks>
    public override Type ClrType => typeof(object);

    /// <inheritdoc />
    /// <remarks>
    ///     Inline schemas have a fixed size only if all their fields have fixed sizes.
    /// </remarks>
    public override bool IsFixedSize => Fields.All(f =>
        f is FieldDefinitionNode fd &&
        fd.TypeAnnotation.IsFixedSize);

    /// <inheritdoc />
    /// <remarks>
    ///     Returns the sum of all field sizes if all are fixed, otherwise null.
    /// </remarks>
    public override int? FixedSizeBytes
    {
        get
        {
            if (!IsFixedSize) return null;

            var total = 0;
            foreach (var field in Fields)
                if (field is FieldDefinitionNode fd && fd.TypeAnnotation.FixedSizeBytes.HasValue)
                    total += fd.TypeAnnotation.FixedSizeBytes.Value;
                else
                    return null; // Computed field or unknown size

            return total;
        }
    }

    /// <inheritdoc />
    public override string ToString()
    {
        if (Fields.Length == 0)
            return "{ }";

        var sb = new StringBuilder();
        sb.Append("{ ");
        sb.Append(string.Join(", ", Fields.Select(f => f.ToString())));
        sb.Append(" }");
        return sb.ToString();
    }

    /// <inheritdoc />
    public override void Accept(IExpressionVisitor visitor)
    {
        visitor.Visit(this);
    }
}
