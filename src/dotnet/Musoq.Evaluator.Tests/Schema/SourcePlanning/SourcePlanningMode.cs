namespace Musoq.Evaluator.Tests.Schema.SourcePlanning;

public enum SourcePlanningMode
{
    RejectAll,
    RejectAllWithExactCardinality,
    RejectAllWithLowConfidenceCardinality,
    AcceptProjection,
    AcceptPredicate,
    AcceptFirstPredicate,
    AcceptPredicateOrderSkipTake,
    AcceptTake,
    AcceptSkipTake,
    AcceptOrder,
    AcceptOrderSkipTake,
    AcceptNaiveOrder,
    AcceptNaiveOrderSkipTake,
    AcceptTopNOrder,
    AcceptTopNOrderSkipTake,
    AcceptNaturalOrder,
    AcceptNaturalOrderSkipTake
}
