using System;

namespace Musoq.Evaluator.Parser;

public class PropertyFromNode : Musoq.Parser.Nodes.From.PropertyFromNode
{
    public PropertyFromNode(string alias, string sourceAlias, string propertyName, Type returnType) 
        : base(alias, sourceAlias, propertyName, returnType)
    {
    }
}