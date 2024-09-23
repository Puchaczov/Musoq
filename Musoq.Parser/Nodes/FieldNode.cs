using System;

namespace Musoq.Parser.Nodes;

public class FieldNode : Node
{
    private readonly string _fieldName;

    public FieldNode(Node expression, int fieldOrder, string fieldName)
    {
        _fieldName = fieldName;
        Expression = expression;
        FieldOrder = fieldOrder;
        Id = $"{nameof(FieldNode)}{expression.Id}";
    }

    public Node Expression { get; }

    public int FieldOrder { get; }

    public string FieldName => string.IsNullOrEmpty(_fieldName) ? Expression.ToString() : _fieldName;

    public override Type ReturnType => Expression.ReturnType;

    public override string Id { get; }

    public override void Accept(IExpressionVisitor visitor)
    {
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