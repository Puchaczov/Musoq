#nullable enable annotations

using System;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Parser;

/// <summary>
///     Evaluator-specific InterpretFromNode that extends the parser's InterpretFromNode.
/// </summary>
public class InterpretFromNode : Musoq.Parser.Nodes.From.InterpretFromNode
{
    public InterpretFromNode(string alias, Node interpretCall, ApplyType applyType, Type? returnType)
        : base(alias, interpretCall, applyType, returnType ?? typeof(object))
    {
    }
}
