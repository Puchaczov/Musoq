using System;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator;

/// <summary>
///     Represents a registered interpretation schema within a query batch.
///     Contains the AST node and generated type information.
/// </summary>
public class SchemaRegistration
{
    /// <summary>
    ///     Creates a new schema registration.
    /// </summary>
    /// <param name="name">The schema name.</param>
    /// <param name="node">The AST node representing the schema definition.</param>
    public SchemaRegistration(string name, Node node)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Node = node ?? throw new ArgumentNullException(nameof(node));
    }

    /// <summary>
    ///     Gets the name of the schema.
    /// </summary>
    public string Name { get; }

    /// <summary>
    ///     Gets the AST node for the schema definition.
    /// </summary>
    public Node Node { get; }

    /// <summary>
    ///     Gets or sets the generated type for this schema.
    ///     Set after code generation is complete (only when a separate assembly is compiled).
    /// </summary>
    public Type? GeneratedType { get; set; }

    /// <summary>
    ///     Gets or sets the fully qualified name of the generated interpreter type.
    ///     Used when the interpreter source is embedded in the main assembly.
    /// </summary>
    public string? GeneratedTypeName { get; set; }

    /// <summary>
    ///     Gets a value indicating whether this is a binary schema.
    /// </summary>
    public bool IsBinarySchema => Node is BinarySchemaNode;

    /// <summary>
    ///     Gets a value indicating whether this is a text schema.
    /// </summary>
    public bool IsTextSchema => Node is TextSchemaNode;
}
