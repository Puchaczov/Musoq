using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.IR.Execution;

internal sealed record SubqueryCteBinding(string Name, int TableIndex, GeneratedRowShape Shape)
{
    internal string ParameterName { get; init; } = $"__subqueryCte{TableIndex}";
    internal string? InheritedParameterName { get; init; }
    internal ExecutionExpression Capture => InheritedParameterName is null
        ? new ExecutionStoredTableRows(TableIndex, Shape)
        : new ExecutionVariableRead(new ExecutionVariable(InheritedParameterName, typeof(object), $"IReadOnlyList<{Shape.TypeName}>"));
}

// Compilation bindings, keyed by the unique lexical subquery identity. Values describe
// existing stored rows; they never contain runtime values or executable delegates.
internal sealed class NativeSubqueryBindings
{
    private readonly Dictionary<string, IReadOnlyList<SubqueryCteBinding>> _bindings = new(StringComparer.Ordinal);

    internal IReadOnlyList<SubqueryCteBinding> Get(string identifier) =>
        _bindings.TryGetValue(identifier, out var bindings) ? bindings : [];

    internal void Register(PhysicalNode query, IReadOnlyDictionary<string, int> indexes,
        IReadOnlyDictionary<string, GeneratedRowShape> shapes, IReadOnlyList<SubqueryCteBinding>? inherited = null)
    {
        var available = indexes.Where(pair => shapes.ContainsKey(pair.Key)).ToDictionary(static pair => pair.Key,
            pair => new SubqueryCteBinding(pair.Key, pair.Value, shapes[pair.Key]), StringComparer.OrdinalIgnoreCase);
        foreach (var binding in inherited ?? [])
            available.TryAdd(binding.Name, binding with { InheritedParameterName = binding.ParameterName });
        foreach (var expression in Expressions(query).OfType<SubqueryExpression>())
        {
            var body = expression.Planning?.Result.PhysicalArtifacts.OptimizedPhysicalPlan;
            if (body is null)
                continue;
            _bindings[expression.Identifier] = ExternalNames(body).Where(available.ContainsKey)
                .Select((name, index) => available[name] with { ParameterName = $"__subqueryCte{index}" }).ToArray();
        }
    }

    private static IEnumerable<string> ExternalNames(PhysicalNode body)
    {
        var locals = Nodes(body).OfType<PhysicalCteNode>()
            .SelectMany(static cte => cte.Definitions.Select(static definition => definition.Name))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        return Nodes(body).OfType<PhysicalCteRefNode>().Select(static reference => reference.CteName)
            .Concat(Expressions(body).OfType<SubqueryExpression>()
                .Where(static query => query.Planning is not null)
                .SelectMany(static query => ExternalNames(query.Planning!.Result.PhysicalArtifacts.OptimizedPhysicalPlan)))
            .Where(name => !locals.Contains(name)).Distinct(StringComparer.OrdinalIgnoreCase);
    }

    internal static bool Contains(PhysicalNode query) => Expressions(query).Any(static expression => expression is SubqueryExpression);

    private static IEnumerable<IrExpression> Expressions(PhysicalNode query) => Nodes(query)
        .SelectMany(CteColumnUsageCollector.EnumerateNodeExpressions).SelectMany(IrExpressionTraversal.SelfAndDescendants);

    private static IEnumerable<PhysicalNode> Nodes(PhysicalNode node)
    {
        yield return node;
        foreach (var child in node.Children)
            foreach (var descendant in Nodes(child))
                yield return descendant;
    }
}
