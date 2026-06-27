using System;
using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Execution;

namespace Musoq.Evaluator.IR.Optimization;

internal sealed record ExpressionCseSkipDiagnosticSummary(
    int HashKeyGroups,
    int ProbePredicateGroups,
    int AggregateHelperBodyGroups,
    int WindowHelperBodyGroups,
    int GeneratedHelperBodyGroups)
{
    public bool HasSkippedOpportunities =>
        HashKeyGroups > 0 ||
        ProbePredicateGroups > 0 ||
        AggregateHelperBodyGroups > 0 ||
        WindowHelperBodyGroups > 0 ||
        GeneratedHelperBodyGroups > 0;

    public string Format()
    {
        var parts = new List<string>(5);
        AddPart(parts, "hash keys", HashKeyGroups);
        AddPart(parts, "probe predicates", ProbePredicateGroups);
        AddPart(parts, "aggregate helper bodies", AggregateHelperBodyGroups);
        AddPart(parts, "window helper bodies", WindowHelperBodyGroups);
        AddPart(parts, "generated helper bodies", GeneratedHelperBodyGroups);
        return string.Join(", ", parts);
    }

    private static void AddPart(List<string> parts, string name, int count)
    {
        if (count > 0)
            parts.Add($"{name}={count}");
    }
}
