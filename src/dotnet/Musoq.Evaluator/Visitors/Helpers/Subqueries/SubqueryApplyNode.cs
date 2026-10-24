using Musoq.Parser.Nodes;
using System.Collections.Generic;
using Musoq.Parser;

namespace Musoq.Evaluator.Visitors.Helpers.Subqueries;

internal enum SubqueryApplyKind
{
    Scalar,
    Exists,
    In,
    Any,
    All
}

internal sealed class SubqueryApplyNode(
    Node subquery,
    SubqueryApplyKind kind,
    Node? operand = null,
    Type? returnType = null,
    IReadOnlyList<Node>? bindings = null) : ScalarSubqueryNode(subquery)
{
    internal Musoq.Parser.Tokens.TokenType Comparison { get; init; } = Musoq.Parser.Tokens.TokenType.Equality;
    internal SubqueryApplyKind Kind { get; } = kind;
    internal Node? Operand { get; } = operand;
    internal bool IsBound => bindings is not null;
    internal IReadOnlyList<Node> Bindings { get; } = bindings ?? [];
    public override Type ReturnType { get; } = returnType ?? (kind == SubqueryApplyKind.Exists ? typeof(bool) : typeof(object));
    public override string Id => $"Apply{Kind}:{Comparison}:{Operand?.Id}:{Subquery.Id}";
    public override string ToString() => Kind is SubqueryApplyKind.Any or SubqueryApplyKind.All
        ? $"{Operand} {Comparison} {Kind} ({Subquery})"
        : base.ToString();
}

internal sealed class SubqueryParameterNode(int index, Type type, TextSpan span, string? displayName = null)
    : AccessColumnNode($"__outer{index}", string.Empty, type, span)
{
    internal int Index { get; } = index;
    public override string ToString() => displayName ?? base.ToString();
}
