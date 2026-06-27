using System.Collections.Generic;
using System.Linq;

namespace Musoq.Parser.Nodes.From;

public class ValuesRowNode(IReadOnlyList<ValuesFieldNode> fields, TextSpan span)
{
    public ValuesRowNode(IReadOnlyList<ValuesFieldNode> fields)
        : this(fields, TextSpan.Empty)
    {
    }

    public IReadOnlyList<ValuesFieldNode> Fields { get; } = fields ?? throw new ArgumentNullException(nameof(fields));

    public TextSpan Span { get; } = span;

    public string Id => $"{nameof(ValuesRowNode)}{string.Join(string.Empty, Fields.Select(valueField => valueField.Id))}";

    public override string ToString()
    {
        return $"{{ {string.Join(", ", Fields.Select(field => field.ToString()))} }}";
    }
}
