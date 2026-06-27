using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Musoq.Evaluator.IR.Expressions;
using AggregateRefRewriter = Musoq.Evaluator.IR.Expressions.AggregateRefRewriter;

namespace Musoq.Evaluator.IR.Logical;

public sealed partial class LogicalPlanBuilder
{
    private static bool TryResolveRefreshCapture(
        IReadOnlyList<string> identifiers,
        string? outputName,
        AggregateRefreshLookup refreshLookup,
        [NotNullWhen(true)] out string? resolvedIdentifier,
        [NotNullWhen(true)] out RefreshMethodCapture? refresh)
    {
        resolvedIdentifier = null;
        refresh = null;

        foreach (var identifier in identifiers)
        {
            if (TryFindRefresh(identifier, refreshLookup.Captures, out resolvedIdentifier, out refresh))
                return true;
        }

        if (!string.IsNullOrWhiteSpace(outputName) &&
            TryFindRefresh(outputName, refreshLookup.Captures, out resolvedIdentifier, out refresh))
            return true;

        return false;
    }

    private static bool TryFindRefresh(
        string? candidate,
        IReadOnlyDictionary<string, RefreshMethodCapture> refreshByIdentifier,
        [NotNullWhen(true)] out string? resolvedIdentifier,
        [NotNullWhen(true)] out RefreshMethodCapture? refresh)
    {
        resolvedIdentifier = null;
        refresh = null;

        if (string.IsNullOrWhiteSpace(candidate))
            return false;

        if (refreshByIdentifier.TryGetValue(candidate, out refresh))
        {
            resolvedIdentifier = candidate;
            return true;
        }

        var normalizedCandidate = NormalizeAggregateIdentifier(candidate);
        if (string.Equals(normalizedCandidate, candidate, StringComparison.Ordinal))
            return false;

        if (string.IsNullOrWhiteSpace(normalizedCandidate) ||
            !refreshByIdentifier.TryGetValue(normalizedCandidate, out refresh))
            return false;

        resolvedIdentifier = normalizedCandidate;
        return true;
    }

    private static string? NormalizeAggregateIdentifier(string identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier))
            return identifier;

        return AggregateRefRewriter.NormalizeIdentifier(identifier);
    }

    private sealed record AggregateRefreshLookup(
        Dictionary<string, RefreshMethodCapture> Captures,
        HashSet<string> AmbiguousNormalizedIdentifiers);

    private sealed record RefreshMethodCapture(MethodInfo SetMethod, IReadOnlyList<IrExpression> SetArguments);
}
