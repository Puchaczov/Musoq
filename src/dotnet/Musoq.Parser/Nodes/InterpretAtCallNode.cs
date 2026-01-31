using System;

namespace Musoq.Parser.Nodes;

/// <summary>
///     Represents a call to the InterpretAt function for binary data interpretation at a specific offset.
/// </summary>
public class InterpretAtCallNode : Node
{
    /// <summary>
    ///     Creates a new InterpretAtCallNode.
    /// </summary>
    /// <param name="dataSource">The expression providing the binary data.</param>
    /// <param name="offset">The byte offset to start interpretation from.</param>
    /// <param name="schemaName">The name of the interpretation schema to use.</param>
    public InterpretAtCallNode(Node dataSource, Node offset, string schemaName)
    {
        DataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));
        Offset = offset ?? throw new ArgumentNullException(nameof(offset));
        SchemaName = schemaName ?? throw new ArgumentNullException(nameof(schemaName));
    }

    /// <summary>
    ///     Creates a new InterpretAtCallNode with return type.
    /// </summary>
    /// <param name="dataSource">The expression providing the binary data.</param>
    /// <param name="offset">The byte offset to start interpretation from.</param>
    /// <param name="schemaName">The name of the interpretation schema to use.</param>
    /// <param name="returnType">The return type of the interpretation.</param>
    public InterpretAtCallNode(Node dataSource, Node offset, string schemaName, Type returnType)
    {
        DataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));
        Offset = offset ?? throw new ArgumentNullException(nameof(offset));
        SchemaName = schemaName ?? throw new ArgumentNullException(nameof(schemaName));
        ReturnType = returnType;
    }

    /// <summary>
    ///     Gets the expression providing the binary data to interpret.
    /// </summary>
    public Node DataSource { get; }

    /// <summary>
    ///     Gets the expression for the byte offset.
    /// </summary>
    public Node Offset { get; }

    /// <summary>
    ///     Gets the name of the interpretation schema to use.
    /// </summary>
    public string SchemaName { get; }

    /// <summary>
    ///     Gets the return type of the interpretation.
    /// </summary>
    public override Type ReturnType { get; }

    /// <summary>
    ///     Gets the unique identifier for this node.
    /// </summary>
    public override string Id => $"{nameof(InterpretAtCallNode)}({DataSource.Id},{Offset.Id},{SchemaName})";

    /// <summary>
    ///     Accepts a visitor for this node.
    /// </summary>
    public override void Accept(IExpressionVisitor visitor)
    {
        visitor.Visit(this);
    }

    /// <summary>
    ///     Returns a string representation of the node.
    /// </summary>
    public override string ToString()
    {
        return $"InterpretAt({DataSource.ToString()}, {Offset.ToString()}, {SchemaName})";
    }
}