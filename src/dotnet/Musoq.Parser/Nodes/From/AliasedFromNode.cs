namespace Musoq.Parser.Nodes.From;

public class AliasedFromNode : FromNode
{
    internal AliasedFromNode(string identifier, ArgsListNode args, string alias, int inSourcePosition, string? typeParameter = null)
        : base(alias)
    {
        Identifier = identifier;
        Args = args;
        InSourcePosition = inSourcePosition;
        TypeParameter = typeParameter;
    }

    public AliasedFromNode(string identifier, ArgsListNode args, string alias, Type returnType, int inSourcePosition, string? typeParameter = null)
        : base(alias, returnType)
    {
        Identifier = identifier;
        Args = args;
        InSourcePosition = inSourcePosition;
        TypeParameter = typeParameter;
    }

    public string Identifier { get; }

    public ArgsListNode Args { get; }

    public int InSourcePosition { get; }

    public string? TypeParameter { get; }

    public override string Id => $"{Identifier}{CreateTypeParameterSuffix(TypeParameter)}-{Alias}";

    public override void Accept(IExpressionVisitor visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);
        visitor.Visit(this);
    }

    public override string ToString()
    {
        var typeParameter = CreateTypeParameterSuffix(TypeParameter);

        if (!string.IsNullOrWhiteSpace(Alias))
            return $"{Identifier}{typeParameter}({Args.ToString()}) as {Alias}";

        return $"{Identifier}{typeParameter}({Args.ToString()})";
    }

    private static string CreateTypeParameterSuffix(string? typeParameter)
    {
        return string.IsNullOrWhiteSpace(typeParameter) ? string.Empty : $"<{typeParameter}>";
    }
}
