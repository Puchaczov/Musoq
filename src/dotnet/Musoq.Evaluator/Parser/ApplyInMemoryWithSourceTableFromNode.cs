using Musoq.Parser.Nodes;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Parser;

public class ApplyInMemoryWithSourceTableFromNode(string inMemoryTableAlias, FromNode sourceTable, ApplyType applyType, bool withOrdinality = false)
    : Musoq.Parser.Nodes.From.ApplyInMemoryWithSourceTableFromNode(inMemoryTableAlias, sourceTable, applyType,
        typeof(RowSource<>), withOrdinality);
