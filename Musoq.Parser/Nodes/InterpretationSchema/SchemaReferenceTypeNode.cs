#nullable enable
using System;

namespace Musoq.Parser.Nodes.InterpretationSchema;

/// <summary>
///     Represents a reference to another schema type.
///     Used for nested schemas, schema composition, and generic type parameter references.
/// </summary>
/// <example>
///     Header          -- Simple schema reference
///     Point[Count]    -- Array of schema
///     Wrapper&lt;Data&gt;   -- Generic schema instantiation
///     T               -- Generic type parameter reference (when used inside a generic schema)
/// </example>
public class SchemaReferenceTypeNode : TypeAnnotationNode
{
    /// <summary>
    ///     Creates a new schema reference type annotation.
    /// </summary>
    /// <param name="schemaName">The name of the referenced schema or type parameter.</param>
    /// <param name="typeArguments">Optional type arguments for generic schema instantiation.</param>
    public SchemaReferenceTypeNode(string schemaName, string[]? typeArguments = null)
    {
        SchemaName = schemaName ?? throw new ArgumentNullException(nameof(schemaName));
        TypeArguments = typeArguments ?? [];

        var typeArgsId = TypeArguments.Length == 0
            ? string.Empty
            : "<" + string.Join(",", TypeArguments) + ">";
        Id = $"{nameof(SchemaReferenceTypeNode)}{schemaName}{typeArgsId}";
    }

    /// <summary>
    ///     Gets the name of the referenced schema or type parameter.
    /// </summary>
    public string SchemaName { get; }

    /// <summary>
    ///     Gets the type arguments for generic schema instantiation.
    ///     Empty array if not a generic instantiation.
    /// </summary>
    /// <example>
    ///     For "LengthPrefixed&lt;Record&gt;", TypeArguments = ["Record"]
    ///     For "Header", TypeArguments = []
    /// </example>
    public string[] TypeArguments { get; }

    /// <summary>
    ///     Gets whether this is a generic schema instantiation (has type arguments).
    /// </summary>
    public bool IsGenericInstantiation => TypeArguments.Length > 0;

    /// <summary>
    ///     Gets the full type name including type arguments.
    /// </summary>
    public string FullTypeName => IsGenericInstantiation
        ? $"{SchemaName}<{string.Join(", ", TypeArguments)}>"
        : SchemaName;

    /// <inheritdoc />
    /// <remarks>
    ///     The actual CLR type is determined at compilation time when the referenced schema is resolved.
    ///     Returns object as a placeholder.
    /// </remarks>
    public override Type ClrType => typeof(object);

    /// <inheritdoc />
    /// <remarks>
    ///     Size depends on the referenced schema definition.
    /// </remarks>
    public override bool IsFixedSize => false;

    /// <inheritdoc />
    public override int? FixedSizeBytes => null;

    /// <inheritdoc />
    public override Type ReturnType => ClrType;

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
        return FullTypeName;
    }
}
