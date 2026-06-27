namespace Musoq.Parser.Nodes;

public sealed class ScriptVariableDeclarationNode : Node
{
    public ScriptVariableDeclarationNode(string name, string typeName, bool isNullable, Node initializer)
        : this(name, typeName, isNullable, initializer, default)
    {
    }

    public ScriptVariableDeclarationNode(
        string name,
        string typeName,
        bool isNullable,
        Node initializer,
        TextSpan span)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        TypeName = typeName ?? throw new ArgumentNullException(nameof(typeName));
        Initializer = initializer ?? throw new ArgumentNullException(nameof(initializer));
        IsNullable = isNullable;
        Id = $"{nameof(ScriptVariableDeclarationNode)}{Name}{DeclaredTypeName}{Initializer.Id}";
        Span = span;
        FullSpan = span;
    }

    public string Name { get; }

    public string TypeName { get; }

    public bool IsNullable { get; }

    public string DeclaredTypeName => IsNullable ? $"{TypeName}?" : TypeName;

    public Node Initializer { get; }

    public override Type ReturnType => typeof(void);

    public override string Id { get; }

    public override void Accept(IExpressionVisitor visitor)
    {
        visitor.Visit(this);
    }

    public override string ToString()
    {
        return $"let {Name}: {DeclaredTypeName} = {Initializer}";
    }
}