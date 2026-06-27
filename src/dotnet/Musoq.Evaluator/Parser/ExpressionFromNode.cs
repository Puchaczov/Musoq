using Musoq.Parser.Nodes;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Parser;

public class ExpressionFromNode(FromNode fromNode)
    : Musoq.Parser.Nodes.From.ExpressionFromNode(fromNode, typeof(RowSource<>));
