namespace Musoq.Benchmarks;

public enum OptimizationBenchmarkPlanningMode
{
    RejectAll,
    RejectAllWithExactCardinality,
    RejectProjection,
    AcceptProjection,
    AcceptPredicate,
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
