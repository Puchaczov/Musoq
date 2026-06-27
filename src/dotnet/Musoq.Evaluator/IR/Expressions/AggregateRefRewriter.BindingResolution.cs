using System.Reflection;
using Musoq.Evaluator.IR.Bindings;

namespace Musoq.Evaluator.IR.Expressions;

public sealed partial class AggregateRefRewriter
{
    private static bool ShouldRewriteToAggregateRef(MethodInfo method, AggregateBinding binding)
    {
         return IsAggregateMethod(method) ||
                method == binding.GetMethod ||
                method == binding.SetMethod;
    }

    private string? ExtractBoundIdentifier(MethodCall methodCall)
    {
        foreach (var argument in methodCall.Arguments)
        {
            if (argument is Literal { Value: string identifier } &&
                _bindingsByIdentifier.ContainsKey(identifier))
                return identifier;
        }

        var derivedIdentifier = ExtractIdentifier(methodCall);
        if (derivedIdentifier is not null && _bindingsByIdentifier.ContainsKey(derivedIdentifier))
            return derivedIdentifier;

        return null;
    }

    private bool TryResolveFallbackBinding(MethodInfo method, out AggregateBinding binding)
    {
        binding = null!;

        AggregateBinding? candidate = null;

        foreach (var currentBinding in _bindingsByIdentifier.Values)
        {
            if (!ShouldRewriteToAggregateRef(method, currentBinding))
                continue;

            if (candidate is not null &&
                !string.Equals(candidate.Identifier, currentBinding.Identifier, StringComparison.Ordinal))
                return false;

            candidate = currentBinding;
        }

        if (candidate is null)
            return false;

        binding = candidate;
        return true;
    }

    private bool TryResolveColumnAggregate(ColumnRef columnRef, out AggregateBinding binding)
    {
        if (TryResolveAggregateBinding(columnRef.ColumnName, out binding))
            return true;

        if (!string.IsNullOrWhiteSpace(columnRef.Alias) &&
            TryResolveAggregateBinding($"{columnRef.Alias}.{columnRef.ColumnName}", out binding))
        {
            return true;
        }

        binding = null!;
        return false;
    }

    private bool TryResolveAggregateBinding(string? key, out AggregateBinding binding)
    {
        binding = null!;
        if (string.IsNullOrWhiteSpace(key))
            return false;

        if (_bindingsByIdentifier.TryGetValue(key, out binding!))
            return true;

        var normalized = NormalizeIdentifier(key);
        if (!string.IsNullOrWhiteSpace(normalized) &&
            _bindingsByIdentifier.TryGetValue(normalized, out binding!))
        {
            return true;
        }

        binding = null!;
        return false;
    }
}
