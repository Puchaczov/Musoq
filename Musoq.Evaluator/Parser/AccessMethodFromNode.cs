using System;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Parser;

public class AccessMethodFromNode : Musoq.Parser.Nodes.From.AccessMethodFromNode
{
    public AccessMethodFromNode(string alias, string sourceAlias, AccessMethodNode accessMethod, Type returnType)
        : base(alias, sourceAlias, accessMethod, returnType)
    {
    }
}