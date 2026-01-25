namespace Musoq.Evaluator.Parser;

public class PropertyFromNode(
    string alias,
    string sourceAlias,
    Musoq.Parser.Nodes.From.PropertyFromNode.PropertyNameAndTypePair[] properties)
    : Musoq.Parser.Nodes.From.PropertyFromNode(alias, sourceAlias, properties);
