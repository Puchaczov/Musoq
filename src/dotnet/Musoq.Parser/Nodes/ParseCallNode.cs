using System;

namespace Musoq.Parser.Nodes;

/// <summary>
///     Represents a call to the Parse function for text data interpretation.
///     This node is used for CROSS APPLY Parse(text, SchemaName) patterns.
/// </summary>
public class ParseCallNode : Node
{
    /// <summary>
    ///     Creates a new ParseCallNode.
    /// </summary>
    /// <param name="dataSource">The expression providing the text data (typically a column or method call).</param>
    /// <param name="schemaName">The name of the text schema to use.</param>
    public ParseCallNode(Node dataSource, string schemaName)
    {
        DataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));
        SchemaName = schemaName ?? throw new ArgumentNullException(nameof(schemaName));
    }

    /// <summary>
    ///     Creates a new ParseCallNode with return type.
    /// </summary>
    /// <param name="dataSource">The expression providing the text data.</param>
    /// <param name="schemaName">The name of the text schema to use.</param>
    /// <param name="returnType">The return type of the parsing.</param>
    public ParseCallNode(Node dataSource, string schemaName, Type returnType)
    {
        DataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));
        SchemaName = schemaName ?? throw new ArgumentNullException(nameof(schemaName));
        ReturnType = returnType;
    }

    /// <summary>
    ///     Gets the expression providing the text data to parse.
    /// </summary>
    public Node DataSource { get; }

    /// <summary>
    ///     Gets the name of the text schema to use.
    /// </summary>
    public string SchemaName { get; }

    /// <summary>
    ///     Gets the return type of the parsing.
    /// </summary>
    public override Type ReturnType { get; }

    /// <summary>
    ///     Gets the unique identifier for this node.
    /// </summary>
    public override string Id => $"{nameof(ParseCallNode)}({DataSource.Id},{SchemaName})";

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
        return $"Parse({DataSource.ToString()}, {SchemaName})";
    }
}