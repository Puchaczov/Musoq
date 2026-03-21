using System;
using System.Linq;

namespace Musoq.Parser.Nodes;

public class WindowNode : Node
{
    public WindowNode(WindowDefinitionNode[] definitions)
    {
        Definitions = definitions;
        var defsId = definitions.Length == 0
            ? string.Empty
            : string.Concat(definitions.Select(d => d.Id));
        Id = $"{nameof(WindowNode)}{defsId}";
    }

    public WindowDefinitionNode[] Definitions { get; }

    public override Type ReturnType => typeof(void);

    public override string Id { get; }

    public override void Accept(IExpressionVisitor visitor)
    {
        visitor.Visit(this);
    }

    public override string ToString()
    {
        return $"window {string.Join(", ", Definitions.Select(d => d.ToString()))}";
    }
}
