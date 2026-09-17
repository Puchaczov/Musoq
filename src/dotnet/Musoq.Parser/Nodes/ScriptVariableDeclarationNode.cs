namespace Musoq.Parser.Nodes;

public sealed class ScriptVariableDeclarationNode : Node
{
    public ScriptVariableDeclarationNode(string name, string typeName, bool isNullable, Node initializer)
        : this(name, typeName, isNullable, initializer, null, false, default)
    {
    }

    public ScriptVariableDeclarationNode(
        string name,
        string typeName,
        bool isNullable,
        Node initializer,
        TextSpan span)
        : this(name, typeName, isNullable, initializer, null, false, span)
    {
    }

    public ScriptVariableDeclarationNode(
        string name,
        StructuralTypeSyntaxNode typeSyntax,
        Node initializer,
        TextSpan span)
        : this(
            name,
            typeSyntax?.ToString() ?? throw new ArgumentNullException(nameof(typeSyntax)),
            typeSyntax.IsNullable,
            initializer,
            typeSyntax,
            false,
            span)
    {
    }

    public ScriptVariableDeclarationNode(string name, Node initializer, TextSpan span)
        : this(name, string.Empty, false, initializer, null, true, span)
    {
    }

    private ScriptVariableDeclarationNode(
        string name,
        string typeName,
        bool isNullable,
        Node initializer,
        StructuralTypeSyntaxNode? typeSyntax,
        bool isInferred,
        TextSpan span)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        TypeName = typeName ?? throw new ArgumentNullException(nameof(typeName));
        Initializer = initializer ?? throw new ArgumentNullException(nameof(initializer));
        IsNullable = isNullable;
        TypeSyntax = typeSyntax;
        IsInferred = isInferred;
        Id = $"{nameof(ScriptVariableDeclarationNode)}{Name}{DeclaredTypeName}{Initializer.Id}";
        Span = span;
        FullSpan = span;
    }

    public string Name { get; }

    public string TypeName { get; }

    public bool IsNullable { get; }

    public StructuralTypeSyntaxNode? TypeSyntax { get; }

    public bool IsInferred { get; }

    public string DeclaredTypeName => IsInferred ? string.Empty : TypeSyntax?.ToString() ?? (IsNullable ? $"{TypeName}?" : TypeName);

    public Node Initializer { get; }

    public override Type ReturnType => typeof(void);

    public override string Id { get; }

    public override void Accept(IExpressionVisitor visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);
        visitor.Visit(this);
    }

    public override string ToString()
    {
        return IsInferred ? $"let {Name} = {Initializer}" : $"let {Name}: {DeclaredTypeName} = {Initializer}";
    }
}
