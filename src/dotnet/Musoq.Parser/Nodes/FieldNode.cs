namespace Musoq.Parser.Nodes;

public class FieldNode : Node
{
    private readonly string? _fieldName;

    public FieldNode(Node expression, int fieldOrder, string? fieldName)
        : this(expression, fieldOrder, fieldName, default(TextSpan))
    {
    }

    public FieldNode(Node expression, int fieldOrder, string? fieldName, TextSpan span)
        : this(expression, fieldOrder, fieldName, !string.IsNullOrEmpty(fieldName), span)
    {
    }

    public FieldNode(Node expression, int fieldOrder, string? fieldName, bool hasExplicitFieldName)
        : this(expression, fieldOrder, fieldName, hasExplicitFieldName, default)
    {
    }

    public FieldNode(Node expression, int fieldOrder, string? fieldName, bool hasExplicitFieldName, TextSpan span)
    {
        ArgumentNullException.ThrowIfNull(expression);
        _fieldName = fieldName;
        Expression = expression;
        FieldOrder = fieldOrder;
        HasExplicitFieldName = hasExplicitFieldName && !string.IsNullOrEmpty(fieldName);
        Id = $"{nameof(FieldNode)}{expression.Id}";

        // Inherit span from expression if not provided
        if (span.IsEmpty && expression?.HasSpan == true)
        {
            Span = expression.Span;
            FullSpan = expression.Span;
        }
        else
        {
            Span = span;
            FullSpan = span;
        }
    }

    public Node Expression { get; }

    public int FieldOrder { get; }

    public bool HasExplicitFieldName { get; }

    public string FieldName => string.IsNullOrEmpty(_fieldName) ? Expression.ToString() : _fieldName;

    public override Type? ReturnType => Expression.ReturnType;

    public override string Id { get; }

    public override void Accept(IExpressionVisitor visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);
        visitor.Visit(this);
    }

    public override string ToString()
    {
        var expression = Expression.ToString();
        if (_fieldName == expression)
            return Expression.ToString();

        if (string.IsNullOrEmpty(_fieldName))
            return Expression.ToString();

        return $"{Expression.ToString()} as {_fieldName}";
    }
}
