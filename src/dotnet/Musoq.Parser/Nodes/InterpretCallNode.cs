using System;

namespace Musoq.Parser.Nodes;

/// <summary>
///     Represents a call to the Interpret function for binary data interpretation.
///     This node is used for CROSS APPLY Interpret(data, SchemaName) patterns.
/// </summary>
public class InterpretCallNode : Node
{
    /// <summary>
    ///     Creates a new InterpretCallNode.
    /// </summary>
    /// <param name="dataSource">The expression providing the binary data (typically a column or method call).</param>
    /// <param name="schemaName">The name of the interpretation schema to use.</param>
    public InterpretCallNode(Node dataSource, string schemaName)
    {
        DataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));
        SchemaName = schemaName ?? throw new ArgumentNullException(nameof(schemaName));
    }

    /// <summary>
    ///     Creates a new InterpretCallNode with return type.
    /// </summary>
    /// <param name="dataSource">The expression providing the binary data.</param>
    /// <param name="schemaName">The name of the interpretation schema to use.</param>
    /// <param name="returnType">The return type of the interpretation.</param>
    public InterpretCallNode(Node dataSource, string schemaName, Type returnType)
    {
        DataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));
        SchemaName = schemaName ?? throw new ArgumentNullException(nameof(schemaName));
        ReturnType = returnType;
    }

    /// <summary>
    ///     Gets the expression providing the binary data to interpret.
    /// </summary>
    public Node DataSource { get; }

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
    public override string Id => $"{nameof(InterpretCallNode)}({DataSource.Id},{SchemaName})";

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
        return $"Interpret({DataSource.ToString()}, {SchemaName})";
    }
}
