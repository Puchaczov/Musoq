namespace Musoq.Evaluator.Tests.Schema.SourcePlanning;

public enum SourcePlanningMode
{
    RejectAll,
    RejectAllWithExactCardinality,
    RejectAllWithLowConfidenceCardinality,
    AcceptProjection,
    AcceptPredicate,
    AcceptTypedStringPredicate,
    AcceptTypedStringPrefixPredicate,
    AcceptTypedStringPredicateUnknownVersion,
    MalformedTypedMissingApplication,
    MalformedTypedDuplicateApplication,
    MalformedTypedAlteredApplication,
    MalformedTypedNestedAccepted,
    MalformedTypedUnknownVersionApplication,
    MalformedTypedUnadvertisedPhaseApplication,
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
