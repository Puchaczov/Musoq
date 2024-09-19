using System;

namespace Musoq.Parser.Nodes.From;

public class PropertyFromNode : FromNode
{
    public PropertyFromNode(string alias, string sourceAlias, string propertyName) 
        : base(alias)
    {
        SourceAlias = sourceAlias;
        PropertyName = propertyName;
    }

    public PropertyFromNode(string alias, string sourceAlias, string propertyName, Type returnType) 
        : base(alias, returnType)
    {
        SourceAlias = sourceAlias;
        PropertyName = propertyName;
    }

    public string SourceAlias { get; }
    
    public string PropertyName { get; }
    
    public override string Id => $"{nameof(PropertyFromNode)}{Alias}{SourceAlias}{PropertyName}";
    
    public override void Accept(IExpressionVisitor visitor)
    {
        visitor.Visit(this);
    }

    public override string ToString()
    {
        if (!string.IsNullOrEmpty(Alias))
            return $"{SourceAlias}.{PropertyName} {Alias}";
        
        return $"{SourceAlias}.{PropertyName}";
    }
}