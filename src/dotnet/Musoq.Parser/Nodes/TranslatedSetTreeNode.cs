using System.Collections.Generic;
using System.Linq;

namespace Musoq.Parser.Nodes;

public class TranslatedSetTreeNode : Node
{
    public TranslatedSetTreeNode(List<TranslatedSetOperatorNode> nodes)
    {
        ArgumentNullException.ThrowIfNull(nodes);
        Nodes = nodes;
        var setId = nodes.Count == 0 ? string.Empty : string.Concat(nodes.Select(f => f.Id));
        Id = $"{nameof(TranslatedSetTreeNode)}{setId}";
    }

    public List<TranslatedSetOperatorNode> Nodes { get; }

    public override Type? ReturnType => null;

    public override string Id { get; }

    public override void Accept(IExpressionVisitor visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);
        visitor.Visit(this);
    }

    public override string ToString()
    {
        return string.Join(Environment.NewLine, Nodes.ConvertAll(f => f.ToString()));
    }
}
