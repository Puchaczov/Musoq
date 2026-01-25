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

/// <summary>
///     Represents a call to the TryInterpret function for safe binary data interpretation.
///     Returns null instead of throwing on parse failure.
/// </summary>
public class TryInterpretCallNode : Node
{
    /// <summary>
    ///     Creates a new TryInterpretCallNode.
    /// </summary>
    /// <param name="dataSource">The expression providing the binary data.</param>
    /// <param name="schemaName">The name of the interpretation schema to use.</param>
    public TryInterpretCallNode(Node dataSource, string schemaName)
    {
        DataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));
        SchemaName = schemaName ?? throw new ArgumentNullException(nameof(schemaName));
    }

    /// <summary>
    ///     Creates a new TryInterpretCallNode with return type.
    /// </summary>
    /// <param name="dataSource">The expression providing the binary data.</param>
    /// <param name="schemaName">The name of the interpretation schema to use.</param>
    /// <param name="returnType">The return type of the interpretation (nullable).</param>
    public TryInterpretCallNode(Node dataSource, string schemaName, Type returnType)
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
    public override string Id => $"{nameof(TryInterpretCallNode)}({DataSource.Id},{SchemaName})";

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
        return $"TryInterpret({DataSource.ToString()}, {SchemaName})";
    }
}

/// <summary>
///     Represents a call to the TryParse function for safe text data interpretation.
///     Returns null instead of throwing on parse failure.
/// </summary>
public class TryParseCallNode : Node
{
    /// <summary>
    ///     Creates a new TryParseCallNode.
    /// </summary>
    /// <param name="dataSource">The expression providing the text data.</param>
    /// <param name="schemaName">The name of the text schema to use.</param>
    public TryParseCallNode(Node dataSource, string schemaName)
    {
        DataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));
        SchemaName = schemaName ?? throw new ArgumentNullException(nameof(schemaName));
    }

    /// <summary>
    ///     Creates a new TryParseCallNode with return type.
    /// </summary>
    /// <param name="dataSource">The expression providing the text data.</param>
    /// <param name="schemaName">The name of the text schema to use.</param>
    /// <param name="returnType">The return type of the parsing (nullable).</param>
    public TryParseCallNode(Node dataSource, string schemaName, Type returnType)
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
    public override string Id => $"{nameof(TryParseCallNode)}({DataSource.Id},{SchemaName})";

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
        return $"TryParse({DataSource.ToString()}, {SchemaName})";
    }
}

/// <summary>
///     Represents a call to the PartialInterpret function for debugging malformed data.
///     Returns partial results with successfully parsed fields and error information.
/// </summary>
public class PartialInterpretCallNode : Node
{
    /// <summary>
    ///     Creates a new PartialInterpretCallNode.
    /// </summary>
    /// <param name="dataSource">The expression providing the binary data.</param>
    /// <param name="schemaName">The name of the interpretation schema to use.</param>
    public PartialInterpretCallNode(Node dataSource, string schemaName)
    {
        DataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));
        SchemaName = schemaName ?? throw new ArgumentNullException(nameof(schemaName));
    }

    /// <summary>
    ///     Creates a new PartialInterpretCallNode with return type.
    /// </summary>
    /// <param name="dataSource">The expression providing the binary data.</param>
    /// <param name="schemaName">The name of the interpretation schema to use.</param>
    /// <param name="returnType">The return type of the partial interpretation.</param>
    public PartialInterpretCallNode(Node dataSource, string schemaName, Type returnType)
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
    ///     Gets the return type of the partial interpretation.
    /// </summary>
    public override Type ReturnType { get; }

    /// <summary>
    ///     Gets the unique identifier for this node.
    /// </summary>
    public override string Id => $"{nameof(PartialInterpretCallNode)}({DataSource.Id},{SchemaName})";

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
        return $"PartialInterpret({DataSource.ToString()}, {SchemaName})";
    }
}
