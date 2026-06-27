using Musoq.Parser.Nodes;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Parser;

public class JoinFromNode(FromNode joinFrom, FromNode from, Node expression, JoinType joinType, FieldOrderedNode? tieBreak = null)
    : Musoq.Parser.Nodes.From.JoinFromNode(joinFrom, from, expression, joinType, typeof(RowSource<>), tieBreak);
