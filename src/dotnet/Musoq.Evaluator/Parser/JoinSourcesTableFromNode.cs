using Musoq.Parser.Nodes;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Parser;

public class JoinSourcesTableFromNode(FromNode first, FromNode second, Node expression, JoinType joinType, FieldOrderedNode? tieBreak = null)
    : Musoq.Parser.Nodes.From.JoinSourcesTableFromNode(first, second, expression, joinType, typeof(RowSource<>), tieBreak);
