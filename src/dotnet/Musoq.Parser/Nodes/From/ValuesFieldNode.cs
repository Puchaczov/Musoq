using System.Collections.Generic;
using System.Linq;

namespace Musoq.Parser.Nodes.From;

public class ValuesFieldNode(string name, Node expression, TextSpan nameSpan)
{
    public ValuesFieldNode(string name, Node expression)
        : this(name, expression, TextSpan.Empty)
    {
    }

    public string Name { get; } = name ?? throw new ArgumentNullException(nameof(name));

    public Node Expression { get; } = expression ?? throw new ArgumentNullException(nameof(expression));

    public TextSpan NameSpan { get; } = nameSpan;

    public string Id => $"{nameof(ValuesFieldNode)}{Name}{Expression.Id}";

    public override string ToString()
    {
        return $"{Name}: {Expression}";
    }
}
