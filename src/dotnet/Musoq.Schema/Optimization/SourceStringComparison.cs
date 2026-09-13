namespace Musoq.Schema.Optimization;

/// <summary>Defines comparison semantics for a typed source string-match predicate.</summary>
public enum SourceStringComparison
{
    /// <summary>
    /// Matches the case-insensitive behavior of the generic Musoq <c>LIKE</c> operator, including its Unicode behavior.
    /// </summary>
    LikeIgnoreCase
}
