using System;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Parser;

public class PositionalSchemaFromNode : SchemaFromNode
{   
    public PositionalSchemaFromNode(string schema, string method, ArgsListNode parameters, string alias, int inSourcePosition) 
        : base(schema, method, parameters, alias)
    {
        InSourcePosition = inSourcePosition;
        PositionalId = $"{alias}{InSourcePosition}";
    }
    
    public string PositionalId { get; }
    
    public int InSourcePosition { get; }

    public override int GetHashCode()
    {
        return PositionalId.GetHashCode();
    }
    
    public override bool Equals(object obj)
    {
        if (obj is PositionalSchemaFromNode node)
            return node.PositionalId == PositionalId;
        
        if (obj is SchemaFromNode _)
            throw new InvalidOperationException("Cannot compare PositionalSchemaFromNode with SchemaFromNode");

        return base.Equals(obj);
    }
}