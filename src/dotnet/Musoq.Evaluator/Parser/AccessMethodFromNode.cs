using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Parser;

public class AccessMethodFromNode(string alias, string sourceAlias, AccessMethodNode accessMethod, Type returnType)
    : Musoq.Parser.Nodes.From.AccessMethodFromNode(alias, sourceAlias, accessMethod, returnType);
