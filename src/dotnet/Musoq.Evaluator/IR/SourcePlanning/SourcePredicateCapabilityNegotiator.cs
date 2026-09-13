using System.Collections.Generic;
using Musoq.Schema.Optimization;

namespace Musoq.Evaluator.IR.SourcePlanning;

internal sealed record SourcePredicateCapabilityNegotiation(
    SourcePlanRequest ProviderRequest,
    SourcePredicateExpression? DeferredPredicate,
    bool HasUnknownContractVersion);

internal static partial class SourcePredicateCapabilityNegotiator
{
    public static SourcePredicateCapabilityNegotiation Negotiate(
        SourcePlanRequest request,
        SourcePredicateCapabilities capabilities)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(capabilities);

        if (request.Predicate == null)
        {
            return new SourcePredicateCapabilityNegotiation(request, null, false);
        }

        if (!ContainsStringMatch(request.Predicate))
        {
            return new SourcePredicateCapabilityNegotiation(request, null, false);
        }

        if (!capabilities.IsKnownVersion)
        {
            var providerPredicate = RemoveTypedPredicates(request.Predicate, out var unknownDeferred);
            return new SourcePredicateCapabilityNegotiation(
                request with { Predicate = providerPredicate },
                unknownDeferred,
                true);
        }

        var accepted = new List<SourcePredicateExpression>();
        var deferred = new List<SourcePredicateExpression>();
        foreach (var conjunct in FlattenConjunction(request.Predicate))
        {
            if (!ContainsStringMatch(conjunct))
            {
                accepted.Add(conjunct);
                continue;
            }

            if (SupportsTopLevelStringMatch(conjunct, capabilities))
                accepted.Add(conjunct);
            else
                deferred.Add(conjunct);
        }

        return new SourcePredicateCapabilityNegotiation(
            request with { Predicate = Combine(accepted) },
            Combine(deferred),
            false);
    }

    public static SourcePlanResult RestoreDeferredPredicates(
        SourcePlanResult result,
        SourcePredicateCapabilityNegotiation negotiation)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(negotiation);
        if (negotiation.DeferredPredicate == null)
            return result;

        var residual = result.ResidualPredicate == null
            ? negotiation.DeferredPredicate
            : new SourcePredicateLogical(
                SourcePredicateLogicalOperator.And,
                negotiation.DeferredPredicate,
                result.ResidualPredicate);
        return result with { ResidualPredicate = residual };
    }

}
