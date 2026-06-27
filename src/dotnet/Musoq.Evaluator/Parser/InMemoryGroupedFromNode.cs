using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Parser;

public class InMemoryGroupedFromNode(string alias)
    : Musoq.Parser.Nodes.From.InMemoryGroupedFromNode(alias, typeof(RowSource<>));
