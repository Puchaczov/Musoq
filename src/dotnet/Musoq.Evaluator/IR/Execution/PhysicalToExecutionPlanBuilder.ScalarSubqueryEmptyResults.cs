using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;
using Musoq.Evaluator.Visitors.Helpers.Subqueries;
using Musoq.Plugins;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class PhysicalToExecutionPlanBuilder
{
    private static IReadOnlyDictionary<string, ScalarSubqueryEmptyResultSpec> CollectScalarSubqueryEmptyResults(
        PhysicalCteNode cte)
    {
        var results = new Dictionary<string, ScalarSubqueryEmptyResultSpec>(StringComparer.OrdinalIgnoreCase);
        foreach (var definition in cte.Definitions)
        {
            var valueColumnName = GeneratedSubqueryContract.CreateValueColumnName(definition.Name);
            if (definition.Plan.OutputSchema.FindByName(valueColumnName) == null)
                continue;

            var bindings = CollectAggregateBindings(definition.Plan);
            if (bindings.Count != 1 || bindings[0].Kernel is not { } kernel)
                continue;

            results[definition.Name] = new ScalarSubqueryEmptyResultSpec(
                definition.Name,
                valueColumnName,
                kernel);
        }

        return results;
    }

    private static IReadOnlyDictionary<string, ScalarSubqueryEmptyResultSpec> MergeScalarSubqueryEmptyResults(
        IReadOnlyDictionary<string, ScalarSubqueryEmptyResultSpec> existing,
        IReadOnlyDictionary<string, ScalarSubqueryEmptyResultSpec> current)
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

    private static bool TryCreateScalarSubqueryEmptyResultLowering(
        PhysicalToExecutionLoweringSession session,
        PhysicalNode buildNode,
        string variablePrefix,
        out string valueColumnName,
        out ScalarSubqueryEmptyResultLowering lowering)
    {
        valueColumnName = string.Empty;
        lowering = null!;
        if (!TryGetScalarSubqueryCteRef(buildNode, out var cteRef) ||
            !session.ScalarSubqueryEmptyResults.TryGetValue(cteRef.CteName, out var spec))
        {
            return false;
        }

        valueColumnName = spec.ValueColumnName;
        var kernel = spec.Kernel;
        switch (kernel.ResultDescriptor.EmptyResultBehavior)
        {
            case AggregateEmptyResultBehavior.Null:
                lowering = new ScalarSubqueryEmptyResultLowering(
                    new ExecutionLiteral(null, kernel.ResultType),
                    []);
                return true;
            case AggregateEmptyResultBehavior.Zero:
                var zero = CreateClrDefaultValue(kernel.UnderlyingResultType);
                lowering = new ScalarSubqueryEmptyResultLowering(
                    new ExecutionLiteral(zero, kernel.ResultType),
                    []);
                return true;
            case AggregateEmptyResultBehavior.Custom:
                var state = new ExecutionVariable(
                    $"{variablePrefix}EmptyState",
                    kernel.StateType);
                var variable = new ExecutionVariable(
                    $"{variablePrefix}EmptyValue",
                    kernel.ResultType);
                var call = new ExecutionMethodCall(
                    kernel.GetMethod,
                    [new ExecutionVariableRead(state)],
                    null,
                    kernel.ResultType);
                lowering = new ScalarSubqueryEmptyResultLowering(
                    new ExecutionVariableRead(variable),
                    [new ExecutionCreateObject(state), new ExecutionLet(variable, call)]);
                return true;
            default:
                throw new ArgumentOutOfRangeException(
                    nameof(kernel.ResultDescriptor.EmptyResultBehavior),
                    kernel.ResultDescriptor.EmptyResultBehavior,
                    string.Create(
                        CultureInfo.InvariantCulture,
                        $"Unsupported aggregate empty result behavior for '{kernel.FunctionName}'."));
        }
    }

    private static bool TryGetScalarSubqueryCteRef(
        PhysicalNode node,
        [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out PhysicalCteRefNode? cteRef)
    {
        while (node is PhysicalFilterNode filter)
            node = filter.Input;

        cteRef = node as PhysicalCteRefNode;
        return cteRef != null;
    }
}
