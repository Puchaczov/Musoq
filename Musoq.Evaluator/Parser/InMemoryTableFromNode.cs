using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Parser;

public class InMemoryTableFromNode : Musoq.Parser.Nodes.From.InMemoryTableFromNode
{
    public InMemoryTableFromNode(string variableName, string alias)
        : base(variableName, alias, typeof(RowSource))
    {
    }
}
