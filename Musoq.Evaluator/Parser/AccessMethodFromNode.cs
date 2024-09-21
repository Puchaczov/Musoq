﻿using System;

namespace Musoq.Evaluator.Parser;

public class AccessMethodFromNode : Musoq.Parser.Nodes.From.AccessMethodFromNode
{
    public AccessMethodFromNode(string alias, string sourceAlias, Musoq.Parser.Nodes.AccessMethodNode accessMethod, Type returnType) 
        : base(alias, sourceAlias, accessMethod, returnType)
    {
    }
}