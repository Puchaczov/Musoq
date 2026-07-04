using Musoq.Parser;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

internal sealed class AggregateIdentifierNode(string identifier, string displayName)
    : WordNode(identifier, TextSpan.Empty)
{
    public string DisplayName { get; } = displayName;
}
