namespace Musoq.Schema.Optimization;

/// <summary>Point in source evaluation at which a typed predicate is applied.</summary>
public enum SourcePredicateEvaluationPhase
{
    /// <summary>Applies the predicate after the source has materialized a row.</summary>
    RowFiltering,

    /// <summary>Applies the predicate to cheap candidate metadata before payload open, decode, or row materialization.</summary>
    CandidateMetadata
}
