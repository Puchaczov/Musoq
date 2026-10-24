using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

internal sealed record RepeatExpressionBinding(string Name, string LastValue, string Values)
{
    internal string? EmitReference(Node expression, Func<string, Node, string> indexed) => expression switch
    {
        AccessColumnNode column when Matches(column.Name) => LastValue,
        IdentifierNode identifier when Matches(identifier.Name) => LastValue,
        ArrayIndexNode { Array: AccessColumnNode column } array when Matches(column.Name) => indexed(Values, array.Index),
        ArrayIndexNode { Array: IdentifierNode identifier } array when Matches(identifier.Name) => indexed(Values, array.Index),
        _ => null
    };

    private bool Matches(string name) => string.Equals(Name, name, StringComparison.OrdinalIgnoreCase);
}
