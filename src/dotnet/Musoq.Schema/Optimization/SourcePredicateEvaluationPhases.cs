namespace Musoq.Schema.Optimization;

/// <summary>Evaluation phases a source can use for typed predicate pushdown.</summary>
[Flags]
public enum SourcePredicateEvaluationPhases
{
    /// <summary>No evaluation phase is supported.</summary>
    None = 0,

    /// <summary>Row filtering is supported.</summary>
    RowFiltering = 1 << 0,

    /// <summary>Candidate-metadata evaluation is supported.</summary>
    CandidateMetadata = 1 << 1,

    /// <summary>All version 1 evaluation phases are supported.</summary>
    All = RowFiltering | CandidateMetadata
}
