namespace Musoq.Benchmarks;

public enum OptimizationBenchmarkPlanningMode
{
    RejectAll,
    RejectAllWithExactCardinality,
    RejectProjection,
    AcceptProjection,
    AcceptPredicate,
    AcceptCandidateStringPredicate,
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
