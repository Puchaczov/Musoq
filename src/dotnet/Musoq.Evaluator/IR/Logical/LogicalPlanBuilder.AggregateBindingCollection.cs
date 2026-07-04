using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;
using AggregateRefRewriter = Musoq.Evaluator.IR.Expressions.AggregateRefRewriter;
using IrNodes = Musoq.Evaluator.IR.Logical.Nodes;

namespace Musoq.Evaluator.IR.Logical;

public sealed partial class LogicalPlanBuilder
{
    private AggregateRefreshLookup BuildRefreshLookup()
    {
        var lookup = new Dictionary<string, RefreshMethodCapture>(StringComparer.Ordinal);
        var rawIdentifiersByNormalizedIdentifier = new Dictionary<string, string>(StringComparer.Ordinal);
        var ambiguousNormalizedIdentifiers = new HashSet<string>(StringComparer.Ordinal);

        foreach (var refresh in _refreshMethods)
        {
            if (refresh.SetArguments.Count > 0 && refresh.SetArguments[0] is Literal { Value: string identifier })
            {
                lookup[identifier] = refresh;

                var normalizedIdentifier = NormalizeAggregateIdentifier(identifier);
                if (!string.Equals(normalizedIdentifier, identifier, StringComparison.Ordinal))
                {
                    if (!string.IsNullOrWhiteSpace(normalizedIdentifier) &&
                        rawIdentifiersByNormalizedIdentifier.TryGetValue(normalizedIdentifier, out var existingIdentifier) &&
                        !string.Equals(existingIdentifier, identifier, StringComparison.Ordinal))
                        ambiguousNormalizedIdentifiers.Add(normalizedIdentifier);
                    else if (!string.IsNullOrWhiteSpace(normalizedIdentifier))
                        rawIdentifiersByNormalizedIdentifier[normalizedIdentifier] = identifier;

                    if (!string.IsNullOrWhiteSpace(normalizedIdentifier))
                        lookup.TryAdd(normalizedIdentifier, refresh);
                }
            }
        }

        return new AggregateRefreshLookup(lookup, ambiguousNormalizedIdentifiers);
    }

    private void CollectBindingsFromProjectedFields(
        AggregateRefreshLookup refreshLookup,
        List<AggregateBinding> bindings,
        Dictionary<string, AggregateBinding> bindingsByIdentifier)
    {
        foreach (var field in _projectedFields)
            CollectBindingsFromExpression(field.Expression, field.OutputName, refreshLookup, bindings, bindingsByIdentifier);
    }

    private void CollectBindingsFromOrderFields(
        AggregateRefreshLookup refreshLookup,
        List<AggregateBinding> bindings,
        Dictionary<string, AggregateBinding> bindingsByIdentifier)
    {
        foreach (var field in _orderFields)
            CollectBindingsFromExpression(field.Expression, null, refreshLookup, bindings, bindingsByIdentifier);
    }

    private static void CollectBindingsFromSourceExpressions(
        LogicalNode source,
        AggregateRefreshLookup refreshLookup,
        List<AggregateBinding> bindings,
        Dictionary<string, AggregateBinding> bindingsByIdentifier)
    {
        switch (source)
        {
            case IrNodes.HavingFilterNode having:
                CollectBindingsFromExpression(having.Predicate, null, refreshLookup, bindings, bindingsByIdentifier);
                CollectBindingsFromSourceExpressions(having.Input, refreshLookup, bindings, bindingsByIdentifier);
                break;
            case IrNodes.WindowNode window:
                foreach (var registration in window.Registrations)
                    CollectBindingsFromWindowRegistration(registration, refreshLookup, bindings, bindingsByIdentifier);
                CollectBindingsFromSourceExpressions(window.Input, refreshLookup, bindings, bindingsByIdentifier);
                break;
            case IrNodes.QualifyFilterNode qualify:
                CollectBindingsFromExpression(qualify.Predicate, null, refreshLookup, bindings, bindingsByIdentifier);
                CollectBindingsFromSourceExpressions(qualify.Input, refreshLookup, bindings, bindingsByIdentifier);
                break;
        }
    }

    private static void CollectBindingsFromWindowRegistration(
        WindowRegistration registration,
        AggregateRefreshLookup refreshLookup,
        List<AggregateBinding> bindings,
        Dictionary<string, AggregateBinding> bindingsByIdentifier)
    {
        foreach (var partitionKey in registration.PartitionKeys)
            CollectBindingsFromExpression(partitionKey, null, refreshLookup, bindings, bindingsByIdentifier);

        foreach (var orderKey in registration.OrderKeys)
            CollectBindingsFromExpression(orderKey.Expression, null, refreshLookup, bindings, bindingsByIdentifier);

        foreach (var valueArgument in registration.ValueArguments)
            CollectBindingsFromExpression(valueArgument, null, refreshLookup, bindings, bindingsByIdentifier);

        CollectBindingsFromExpression(registration.FilterPredicate, null, refreshLookup, bindings, bindingsByIdentifier);
    }

    private static void CollectBindingsFromExpression(
        IrExpression? expression,
        string? outputName,
        AggregateRefreshLookup refreshLookup,
        List<AggregateBinding> bindings,
        Dictionary<string, AggregateBinding> bindingsByIdentifier)
    {
        if (expression is null)
            return;

        if (expression is MethodCall methodCall && AggregateRefRewriter.IsAggregateMethod(methodCall.Method))
        {
            var identifierCandidates = GetAggregateIdentifierCandidates(methodCall, refreshLookup);

            if (TryResolveRefreshCapture(identifierCandidates, outputName, refreshLookup, out var resolvedIdentifier, out var refresh) &&
                !bindingsByIdentifier.ContainsKey(resolvedIdentifier))
            {
                var kernel = TryCreateAggregateKernelDescriptor(methodCall.Method, methodCall.Arguments) ??
                             TryCreateAggregateKernelDescriptor(
                                 methodCall.Method,
                                 refresh.SetArguments.Skip(1).ToArray());
                var binding = new AggregateBinding(
                    resolvedIdentifier,
                    ResolveAggregateColumnName(outputName, refresh, resolvedIdentifier),
                    refresh.SetMethod,
                    [.. refresh.SetArguments],
                    refresh.FilterPredicate,
                    methodCall.Method,
                    kernel is null
                        ? CreateAggregateGetArguments(methodCall.Method, methodCall.Arguments, refresh)
                        : [],
                    kernel?.ResultType ?? methodCall.ReturnType,
                    kernel,
                    ResolveAggregateParentDepth(kernel, methodCall.Arguments),
                    refresh.DisplayName);

                bindings.Add(binding);
                bindingsByIdentifier[resolvedIdentifier] = binding;

                var normalizedIdentifier = AggregateRefRewriter.NormalizeIdentifier(resolvedIdentifier);
                if (!string.IsNullOrWhiteSpace(normalizedIdentifier))
                    bindingsByIdentifier.TryAdd(normalizedIdentifier, binding);

                if (!string.IsNullOrWhiteSpace(binding.ColumnName))
                    bindingsByIdentifier.TryAdd(binding.ColumnName, binding);

                var normalizedColumnName = AggregateRefRewriter.NormalizeIdentifier(binding.ColumnName);
                if (!string.IsNullOrWhiteSpace(normalizedColumnName))
                    bindingsByIdentifier.TryAdd(normalizedColumnName, binding);
            }

            return;
        }

        switch (expression)
        {
            case BinaryOp binaryOp:
                CollectBindingsFromExpression(binaryOp.Left, outputName, refreshLookup, bindings, bindingsByIdentifier);
                CollectBindingsFromExpression(binaryOp.Right, outputName, refreshLookup, bindings, bindingsByIdentifier);
                break;
            case UnaryOp unaryOp:
                CollectBindingsFromExpression(unaryOp.Operand, outputName, refreshLookup, bindings, bindingsByIdentifier);
                break;
            case CaseWhen caseWhen:
                foreach (var branch in caseWhen.Branches)
                {
                    CollectBindingsFromExpression(branch.Condition, outputName, refreshLookup, bindings, bindingsByIdentifier);
                    CollectBindingsFromExpression(branch.Result, outputName, refreshLookup, bindings, bindingsByIdentifier);
                }
                if (caseWhen.ElseExpression is not null)
                    CollectBindingsFromExpression(caseWhen.ElseExpression, outputName, refreshLookup, bindings, bindingsByIdentifier);
                break;
        }
    }

    private static string ResolveAggregateColumnName(
        string? outputName,
        RefreshMethodCapture refresh,
        string resolvedIdentifier)
    {
        if (!string.IsNullOrWhiteSpace(outputName))
            return outputName;

        return !string.IsNullOrWhiteSpace(refresh.DisplayName)
            ? refresh.DisplayName
            : resolvedIdentifier;
    }
}
