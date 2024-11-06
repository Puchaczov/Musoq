using System;

namespace Musoq.Parser.Nodes;

public class AccessColumnNode(string column, string alias, Type type, TextSpan span)
    : IdentifierNode(column)
{
    private Type _type = type;
    private readonly string _column = column;

    public AccessColumnNode(string column, string alias, TextSpan span)
        : this(column, alias, typeof(void), span)
    {
    }

    public string Alias { get; } = alias;

    public TextSpan Span { get; } = span;

    public override Type ReturnType => _type;

    public override string Id => $"{nameof(AccessColumnNode)}{_column}{_type?.Name ?? string.Empty}";

    public override void Accept(IExpressionVisitor visitor)
    {
        visitor.Visit(this);
    }

    public override string ToString()
    {
        return string.IsNullOrEmpty(Alias) ? Name : $"{Alias}.{Name}";
    }

    public void ChangeReturnType(Type returnType)
    {
        _type = returnType;
    }
}