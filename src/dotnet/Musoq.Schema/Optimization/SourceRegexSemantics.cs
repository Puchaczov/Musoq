namespace Musoq.Schema.Optimization;

/// <summary>Identifies the complete regex semantics required by a predicate.</summary>
public enum SourceRegexSemantics
{
    /// <summary>
    /// Musoq RLIKE: .NET regex syntax and inline options, substring matching unless anchored,
    /// execution-time current culture, and Core's non-backtracking engine with backtracking fallback.
    /// Null input matches false before negation. The predicate's finite match timeout applies.
    /// </summary>
    MusoqRLike
}
