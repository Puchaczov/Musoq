using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

internal sealed record RepeatExpressionBinding(string Name, string LastValue, string Values)
{
    internal string? EmitReference(Node expression, Func<Node, string> generateIndex) =>
        expression switch
        {
            AccessColumnNode column when Matches(column.Name) => LastValue,
            IdentifierNode identifier when Matches(identifier.Name) => LastValue,
            ArrayIndexNode { Array: AccessColumnNode column } array when Matches(column.Name) =>
                $"global::Musoq.Evaluator.Helpers.SafeArrayAccess.GetListElement({Values}, {generateIndex(array.Index)})",
            ArrayIndexNode { Array: IdentifierNode identifier } array when Matches(identifier.Name) =>
                $"global::Musoq.Evaluator.Helpers.SafeArrayAccess.GetListElement({Values}, {generateIndex(array.Index)})",
            _ => null,
        };

    private bool Matches(string name) => string.Equals(Name, name, StringComparison.Ordinal);
}
