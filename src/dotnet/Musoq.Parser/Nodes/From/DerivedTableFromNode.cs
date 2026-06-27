namespace Musoq.Parser.Nodes.From;

public class DerivedTableFromNode(Node query, string alias, bool allowsCorrelation)
    : InMemoryTableFromNode(alias, alias)
{
    public Node Query { get; } = query;

    public bool AllowsCorrelation { get; } = allowsCorrelation;

    public override string Id => $"{nameof(DerivedTableFromNode)}{Alias}{Query.Id}";

    public override string ToString()
    {
        return $"({Query}) {Alias}";
    }
}
