using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Parser;

public class JoinNode(JoinFromNode join) : Musoq.Parser.Nodes.From.JoinNode(join, typeof(RowSource<>));
