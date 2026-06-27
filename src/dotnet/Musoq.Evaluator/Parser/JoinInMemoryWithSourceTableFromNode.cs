using Musoq.Parser.Nodes;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Parser;

public class JoinInMemoryWithSourceTableFromNode(
    string inMemoryTableAlias,
    FromNode sourceTable,
    Node expression,
    JoinType joinType,
    string? inMemoryTableVariableName = null,
    FieldOrderedNode? tieBreak = null)
    : Musoq.Parser.Nodes.From.JoinInMemoryWithSourceTableFromNode(inMemoryTableAlias, sourceTable, expression, joinType,
        typeof(RowSource<>), tieBreak)
{
    public string? InMemoryTableVariableName { get; } = inMemoryTableVariableName;
}
