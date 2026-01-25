#nullable enable
using System;

namespace Musoq.Parser.Nodes.InterpretationSchema;

/// <summary>
///     Base class for field definitions within a binary or text schema.
///     Both parsed fields (FieldDefinitionNode) and computed fields (ComputedFieldNode) inherit from this.
/// </summary>
public abstract class SchemaFieldNode : Node
{
    /// <summary>
    ///     Creates a new schema field.
    /// </summary>
    /// <param name="name">The field name.</param>
    protected SchemaFieldNode(string name)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
    }

    /// <summary>
    ///     Gets the field name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    ///     Gets whether this field is computed (expression-based, does not consume bytes).
    /// </summary>
    public abstract bool IsComputed { get; }

    /// <summary>
    ///     Gets whether this field is conditional (has a when clause).
    ///     Conditional fields may be null when condition evaluates to false.
    /// </summary>
    public virtual bool IsConditional => false;
}
