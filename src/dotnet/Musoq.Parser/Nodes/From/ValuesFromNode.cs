using System.Collections.Generic;
using System.Linq;

namespace Musoq.Parser.Nodes.From;

public class ValuesFromNode : FromNode
{
    public ValuesFromNode(IReadOnlyList<ValuesRowNode> rows, string alias)
        : base(alias)
    {
        Rows = rows ?? throw new ArgumentNullException(nameof(rows));
    }

    public ValuesFromNode(IReadOnlyList<ValuesRowNode> rows, string alias, Type returnType)
        : base(alias, returnType)
    {
        Rows = rows ?? throw new ArgumentNullException(nameof(rows));
    }

    public IReadOnlyList<ValuesRowNode> Rows { get; }

    public override string Id => $"{nameof(ValuesFromNode)}{Alias}{string.Join(string.Empty, Rows.Select(row => row.Id))}";

    public override void Accept(IExpressionVisitor visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);
        visitor.Visit(this);
    }

    public override string ToString()
    {
        return $"values {{ {string.Join(", ", Rows.Select(row => row.ToString()))} }} {Alias}";
    }
}
