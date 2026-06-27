using Musoq.Parser.Nodes;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Parser;

public class ApplySourcesTableFromNode(FromNode first, FromNode second, ApplyType applyType, bool withOrdinality = false)
    : Musoq.Parser.Nodes.From.ApplySourcesTableFromNode(first, second, applyType, typeof(RowSource<>), withOrdinality);
