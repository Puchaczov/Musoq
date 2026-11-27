using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;
using Musoq.Evaluator.Visitors.Helpers.Subqueries;
using Musoq.Plugins;

namespace Musoq.Evaluator.IR.Execution;

internal sealed partial class PhysicalLoweringImplementation
{
    private static IReadOnlyDictionary<string, ScalarSubqueryEmptyResultSpec> CollectScalarSubqueryEmptyResults(
        PhysicalCteNode cte
    )
    {
        var results = new Dictionary<string, ScalarSubqueryEmptyResultSpec>(StringComparer.OrdinalIgnoreCase);
        foreach (var definition in cte.Definitions)
        {
            var valueColumnName = GeneratedSubqueryContract.CreateValueColumnName(definition.Name);
            var output = definition.Plan switch
            {
                PhysicalMultiStatementNode { Statements: [.., PhysicalProjectNode final] } => final,
                PhysicalProjectNode project => project,
                _ => null,
            };
            var field = output?.Fields.FirstOrDefault(item =>
                string.Equals(item.OutputName, valueColumnName, StringComparison.OrdinalIgnoreCase)
            );
            if (field == null)
                continue;

            var bindings = CollectAggregateBindings(definition.Plan);
            if (bindings.Count == 0)
                continue;

            var expression = AggregateRefRewriter.Rewrite(field.Expression, CreateAggregateBindingsMap(bindings));
            var references = IrExpressionTraversal
                .SelfAndDescendants(expression)
                .OfType<AggregateRef>()
                .Select(reference => reference.Identifier)
                .ToHashSet(StringComparer.Ordinal);
            var projectedBindings = bindings.Where(binding => references.Contains(binding.Identifier)).ToArray();
            if (projectedBindings.Length != 0 && projectedBindings.All(binding => binding.Kernel != null))
                results[definition.Name] = new ScalarSubqueryEmptyResultSpec(
                    field with
                    {
                        Expression = expression,
                    },
                    projectedBindings
                );
        }

        return results;
    }

    private static IReadOnlyDictionary<string, ScalarSubqueryEmptyResultSpec> MergeScalarSubqueryEmptyResults(
        IReadOnlyDictionary<string, ScalarSubqueryEmptyResultSpec> existing,
        IReadOnlyDictionary<string, ScalarSubqueryEmptyResultSpec> current
    )
    {
        if (current.Count == 0)
            return existing;

        var merged = new Dictionary<string, ScalarSubqueryEmptyResultSpec>(existing, StringComparer.OrdinalIgnoreCase);
        foreach (var pair in current)
            merged[pair.Key] = pair.Value;
        return merged;
    }

    private static List<AggregateBinding> CollectAggregateBindings(PhysicalNode node)
    {
        var bindings = new List<AggregateBinding>();
        CollectAggregateBindings(node, bindings);
        return bindings;
    }

    private static void CollectAggregateBindings(PhysicalNode node, List<AggregateBinding> bindings)
    {
        switch (node)
        {
            case PhysicalAggregateCandidateNode aggregate:
                AddUniqueBindings(bindings, aggregate.Bindings);
                break;
            case PhysicalSingleKeyAggregateNode aggregate:
                AddUniqueBindings(bindings, aggregate.Bindings);
                break;
            case PhysicalValueTupleAggregateNode aggregate:
                AddUniqueBindings(bindings, aggregate.Bindings);
                break;
            case PhysicalAggregateOnlyNode aggregate:
                AddUniqueBindings(bindings, aggregate.Bindings);
                break;
        }

        foreach (var child in node.Children)
            CollectAggregateBindings(child, bindings);
    }

    private static void AddUniqueBindings(List<AggregateBinding> target, IReadOnlyList<AggregateBinding> source)
    {
        foreach (var binding in source)
            if (!target.Any(existing => ReferenceEquals(existing, binding)))
                target.Add(binding);
    }

    private bool TryCreateScalarSubqueryEmptyResultLowering(
        LoweringScope scope,
        PhysicalNode buildNode,
        string variablePrefix,
        out string valueColumnName,
        out ScalarSubqueryEmptyResultLowering lowering
    )
    {
        valueColumnName = string.Empty;
        lowering = null!;
        if (
            !TryGetScalarSubqueryCteRef(buildNode, out var cteRef)
            || !scope.ScalarSubqueryEmptyResults.TryGetValue(cteRef.CteName, out var spec)
        )
        {
            return false;
        }

        valueColumnName = spec.Projection.OutputName;
        var projection = ConvertProjectedExpression(spec.Projection, RowShapeLookup.EmptySourceShapeLookup());
        var values = new Dictionary<string, ExecutionExpression>(StringComparer.Ordinal);
        var prelude = new List<ExecutionNode>();
        foreach (var binding in spec.Bindings)
        {
            var prefix = spec.Bindings.Count == 1 ? variablePrefix : $"{variablePrefix}Agg{values.Count}";
            var empty = CreateEmptyAggregateResult(prefix, binding.Kernel!);
            values.Add(binding.Identifier, empty.Value);
            prelude.AddRange(empty.PreludeNodes);
        }

        var value = new ExecutionExpressionSubstitutionRewriter(candidate =>
            candidate is ExecutionAggregateResultRef aggregate ? values[aggregate.Identifier] : null
        ).RewriteExpression(projection);
        lowering = new ScalarSubqueryEmptyResultLowering(value, prelude);
        return true;
    }

    private static ScalarSubqueryEmptyResultLowering CreateEmptyAggregateResult(
        string variablePrefix,
        AggregateKernelDescriptor kernel
    )
    {
        switch (kernel.ResultDescriptor.EmptyResultBehavior)
        {
            case AggregateEmptyResultBehavior.Null:
                return new ScalarSubqueryEmptyResultLowering(new ExecutionLiteral(null, kernel.ResultType), []);
            case AggregateEmptyResultBehavior.Zero:
                var zero = CreateClrDefaultValue(kernel.UnderlyingResultType);
                return new ScalarSubqueryEmptyResultLowering(new ExecutionLiteral(zero, kernel.ResultType), []);
            case AggregateEmptyResultBehavior.Custom:
                var state = new ExecutionVariable($"{variablePrefix}EmptyState", kernel.StateType);
                var variable = new ExecutionVariable($"{variablePrefix}EmptyValue", kernel.ResultType);
                var call = new ExecutionMethodCall(
                    kernel.GetMethod,
                    [new ExecutionVariableRead(state)],
                    null,
                    kernel.ResultType
                );
                return new ScalarSubqueryEmptyResultLowering(
                    new ExecutionVariableRead(variable),
                    [
                        new ExecutionInitializeAggregateState(
                            state,
                            ExecutionClrBindingFactory.FromClr(kernel.ResetMethod)
                        ),
                        new ExecutionLet(variable, call),
                    ]
                );
            default:
                throw new ArgumentOutOfRangeException(
                    nameof(kernel.ResultDescriptor.EmptyResultBehavior),
                    kernel.ResultDescriptor.EmptyResultBehavior,
                    string.Create(
                        CultureInfo.InvariantCulture,
                        $"Unsupported aggregate empty result behavior for '{kernel.FunctionName}'."
                    )
                );
        }
    }

    private static bool TryGetScalarSubqueryCteRef(
        PhysicalNode node,
        [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out PhysicalCteRefNode? cteRef
    )
    {
        while (node is PhysicalFilterNode filter)
            node = filter.Input;

        cteRef = node as PhysicalCteRefNode;
        return cteRef != null;
    }
}
