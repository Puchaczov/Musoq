using System;

namespace Musoq.Parser.Nodes;

public class AccessColumnNode : IdentifierNode
{
    private readonly string _column;
    private Type _type;

    public AccessColumnNode(string column, string alias, TextSpan span)
        : this(column, alias, typeof(void), span)
    {
    }

    public AccessColumnNode(string column, string alias, Type type, TextSpan span)
        : base(column, null, span)
    {
        _column = column;
        _type = type;
        Alias = alias;
    }

    public AccessColumnNode(string column, string alias, Type type, TextSpan span, string? intendedTypeName)
        : this(column, alias, type, span)
    {
        IntendedTypeName = intendedTypeName;
    }

    public string Alias { get; }

    public override Type ReturnType => _type;

    /// <summary>
    ///     Gets the intended fully-qualified type name for this column.
    ///     Used when the actual Type is not available at compile time
    ///     (e.g., for embedded interpreter types).
    /// </summary>
    public string? IntendedTypeName { get; private set; }

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

    public void SetIntendedTypeName(string? intendedTypeName)
    {
        IntendedTypeName = intendedTypeName;
    }
}
