namespace Musoq.Parser.Nodes;

public class ParameterDeclarationNode : Node
{
    public ParameterDeclarationNode(string name, string typeName, bool isNullable, Node? defaultValue)
        : this(name, typeName, isNullable, defaultValue, null, default)
    {
    }

    public ParameterDeclarationNode(string name, string typeName, bool isNullable, Node? defaultValue, TextSpan span)
        : this(name, typeName, isNullable, defaultValue, null, span)
    {
    }

    public ParameterDeclarationNode(
        string name,
        StructuralTypeSyntaxNode typeSyntax,
        Node? defaultValue,
        TextSpan span)
        : this(
            name,
            typeSyntax?.ToString() ?? throw new ArgumentNullException(nameof(typeSyntax)),
            typeSyntax.IsNullable,
            defaultValue,
            typeSyntax,
            span)
    {
    }

    private ParameterDeclarationNode(
        string name,
        string typeName,
        bool isNullable,
        Node? defaultValue,
        StructuralTypeSyntaxNode? typeSyntax,
        TextSpan span)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        TypeName = typeName ?? throw new ArgumentNullException(nameof(typeName));
        IsNullable = isNullable;
        TypeSyntax = typeSyntax;
        DefaultValue = defaultValue;
        Id = $"{nameof(ParameterDeclarationNode)}{Name}{DeclaredTypeName}{DefaultValue?.Id}";
        Span = span;
        FullSpan = span;
    }

    public string Name { get; }

    public string TypeName { get; }

    public bool IsNullable { get; }

    public StructuralTypeSyntaxNode? TypeSyntax { get; }

    public string DeclaredTypeName => TypeSyntax?.ToString() ?? (IsNullable ? $"{TypeName}?" : TypeName);

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
