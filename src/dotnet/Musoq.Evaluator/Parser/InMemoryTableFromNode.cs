using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Parser;

public class InMemoryTableFromNode(string variableName, string alias)
    : Musoq.Parser.Nodes.From.InMemoryTableFromNode(variableName, alias, typeof(RowSource<>));
