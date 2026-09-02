namespace Musoq.Parser.Nodes.From;

public class SchemaFromNode : FromNode
{
    internal SchemaFromNode(string schema, string method, ArgsListNode parameters, string alias, int queryId)
        : base(alias)
    {
        Schema = schema;
        Method = method;
        Parameters = parameters;
        QueryId = queryId;
        var paramsId = parameters.Id;
        Id = $"{nameof(SchemaFromNode)}{schema}{method}{paramsId}{Alias}";
    }

    public SchemaFromNode(string schema, string method, ArgsListNode parameters, string alias, Type returnType,
        int queryId)
        : base(alias, returnType)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        Schema = schema;
        Method = method;
        Parameters = parameters;
        QueryId = queryId;
        var paramsId = parameters.Id;
        Id = $"{nameof(SchemaFromNode)}{schema}{method}{paramsId}{Alias}";
    }

    public string Schema { get; }

    public string Method { get; }

    public ArgsListNode Parameters { get; }

    public int QueryId { get; }

    /// <summary>Source span of the schema identifier, when parsed from query text.</summary>
    public TextSpan? SchemaSpan { get; private set; }

    /// <summary>Source span of the source/method identifier, when available.</summary>
    public TextSpan? MethodSpan { get; private set; }

    public override string Id { get; }

    public SchemaFromNode WithSchemaSpan(TextSpan span) { SchemaSpan = span; return this; }

    public SchemaFromNode WithMethodSpan(TextSpan span) { MethodSpan = span; return this; }

    public override void Accept(IExpressionVisitor visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);
        visitor.Visit(this);
    }

    public override string ToString()
    {
        if (string.IsNullOrWhiteSpace(Alias))
            return $"{Schema}.{Method}({Parameters.ToString()})";

        return $"{Schema}.{Method}({Parameters.ToString()}) {Alias}";
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode(StringComparison.Ordinal);
    }

    public override bool Equals(object? obj)
    {
        if (obj is SchemaFromNode node)
            return node.Id == Id;

        return false;
    }
}
