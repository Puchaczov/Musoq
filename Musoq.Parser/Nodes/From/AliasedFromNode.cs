using System;

namespace Musoq.Parser.Nodes.From;

public class AliasedFromNode : FromNode
{
    internal AliasedFromNode(string identifier, ArgsListNode args, string alias, int inSourcePosition)
        : base(alias)
    {
        Identifier = identifier;
        Args = args;
        InSourcePosition = inSourcePosition;
    }
        
    public AliasedFromNode(string identifier, ArgsListNode args, string alias, Type returnType, int inSourcePosition)
        : base(alias, returnType)
    {
        Identifier = identifier;
        Args = args;
        InSourcePosition = inSourcePosition;
    }

    public string Identifier { get; }

    public ArgsListNode Args { get; }
        
    public int InSourcePosition { get; }

    public override string Id => $"{Identifier}-{Alias}";

    public override void Accept(IExpressionVisitor visitor)
    {
        visitor.Visit(this);
    }

    public override string ToString()
    {
        if (!string.IsNullOrWhiteSpace(Alias))
            return $"{Identifier}({Args.ToString()}) as {Alias}";
            
        return $"{Identifier}({Args.ToString()})";
    }
}