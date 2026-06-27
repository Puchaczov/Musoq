namespace Musoq.Parser.Nodes;

/// <summary>
///     Represents a call to the PartialParse function for debugging malformed text.
///     Returns partial results with successfully parsed fields and error information.
/// </summary>
public class PartialParseCallNode : Node
{
    /// <summary>
    ///     Creates a new PartialParseCallNode.
    /// </summary>
    /// <param name="dataSource">The expression providing the text data.</param>
    /// <param name="schemaName">The name of the interpretation schema to use.</param>
    public PartialParseCallNode(Node dataSource, string schemaName)
    {
        DataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));
        SchemaName = schemaName ?? throw new ArgumentNullException(nameof(schemaName));
    }

    /// <summary>
    ///     Creates a new PartialParseCallNode with return type.
    /// </summary>
    /// <param name="dataSource">The expression providing the text data.</param>
    /// <param name="schemaName">The name of the interpretation schema to use.</param>
    /// <param name="returnType">The return type of the partial parse.</param>
    public PartialParseCallNode(Node dataSource, string schemaName, Type? returnType)
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
    ///     Gets the name of the interpretation schema to use.
    /// </summary>
    public string SchemaName { get; }

    /// <summary>
    ///     Gets the return type of the partial parse.
    /// </summary>
    public override Type? ReturnType { get; }

    /// <summary>
    ///     Gets the unique identifier for this node.
    /// </summary>
    public override string Id => $"{nameof(PartialParseCallNode)}<{SchemaName}>({DataSource.Id})";

    /// <summary>
    ///     Accepts a visitor for this node.
    /// </summary>
    public override void Accept(IExpressionVisitor visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);
        visitor.Visit(this);
    }

    /// <summary>
    ///     Returns a string representation of the node.
    /// </summary>
    public override string ToString()
    {
        return $"PartialParse<{SchemaName}>({DataSource.ToString()})";
    }
}
