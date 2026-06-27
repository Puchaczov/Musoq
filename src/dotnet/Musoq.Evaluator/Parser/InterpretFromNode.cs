using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Parser;

/// <summary>
///     Evaluator-specific InterpretFromNode that extends the parser's InterpretFromNode.
/// </summary>
public class InterpretFromNode(string alias, Node interpretCall, ApplyType applyType, Type? returnType)
    : Musoq.Parser.Nodes.From.InterpretFromNode(alias, interpretCall, applyType, returnType ?? typeof(object));
