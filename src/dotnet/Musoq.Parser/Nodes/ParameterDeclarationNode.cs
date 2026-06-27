namespace Musoq.Parser.Nodes;

public class ParameterDeclarationNode : Node
{
    public ParameterDeclarationNode(string name, string typeName, bool isNullable, Node? defaultValue)
        : this(name, typeName, isNullable, defaultValue, default)
    {
    }

    public ParameterDeclarationNode(string name, string typeName, bool isNullable, Node? defaultValue, TextSpan span)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        TypeName = typeName ?? throw new ArgumentNullException(nameof(typeName));
        IsNullable = isNullable;
        DefaultValue = defaultValue;
        Id = $"{nameof(ParameterDeclarationNode)}{Name}{DeclaredTypeName}{DefaultValue?.Id}";
        Span = span;
        FullSpan = span;
    }

    public string Name { get; }

    public string TypeName { get; }

    public bool IsNullable { get; }

    public string DeclaredTypeName => IsNullable ? $"{TypeName}?" : TypeName;

    public Node? DefaultValue { get; }

    public bool HasDefaultValue => DefaultValue != null;

    public override Type ReturnType => typeof(void);

    public override string Id { get; }

    public override void Accept(IExpressionVisitor visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);
        visitor.Visit(this);
    }

    public override string ToString()
    {
        var defaultText = HasDefaultValue ? $" = {DefaultValue}" : string.Empty;
        return $"{Name}: {DeclaredTypeName}{defaultText}";
    }
}
