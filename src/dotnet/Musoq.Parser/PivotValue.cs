using Musoq.Parser.Nodes;

namespace Musoq.Parser;

internal sealed record PivotValue(Node[] Expressions, string Alias);
