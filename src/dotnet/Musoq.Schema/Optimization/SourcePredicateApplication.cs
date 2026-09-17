using System.Collections.Generic;

namespace Musoq.Schema.Optimization;

/// <summary>Records where a source applies an accepted typed string-match predicate.</summary>
public sealed record SourcePredicateApplication
{
    /// <summary>Initializes an application record.</summary>
    /// <param name="predicate">The exact accepted typed predicate being applied.</param>
    /// <param name="phase">The phase at which the source applies it.</param>
    public SourcePredicateApplication(
        SourcePredicateStringMatch predicate,
        SourcePredicateEvaluationPhase phase)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        if (!Enum.IsDefined(phase))
            throw new ArgumentOutOfRangeException(nameof(phase), phase, "Unknown source predicate evaluation phase.");

        Predicate = predicate;
        Phase = phase;
    }

    /// <summary>Gets the exact accepted typed predicate being applied.</summary>
    public SourcePredicateStringMatch Predicate { get; }

    /// <summary>Gets the phase at which the source applies the predicate.</summary>
    public SourcePredicateEvaluationPhase Phase { get; }

    /// <summary>Creates row-filtering applications for direct top-level <c>AND</c> string-match conjuncts.</summary>
    /// <param name="predicate">The accepted predicate tree.</param>
    /// <returns>An immutable list preserving direct-conjunct order and multiplicity.</returns>
    public static IReadOnlyList<SourcePredicateApplication> ForRowFiltering(
        SourcePredicateExpression? predicate)
    {
        var applications = new List<SourcePredicateApplication>();
        AddTopLevelConjuncts(predicate, applications);
        return applications.AsReadOnly();
    }

    private static void AddTopLevelConjuncts(
        SourcePredicateExpression? predicate,
        ICollection<SourcePredicateApplication> applications)
    {
        if (predicate is SourcePredicateLogical { Operator: SourcePredicateLogicalOperator.And } logical)
        {
            AddTopLevelConjuncts(logical.Left, applications);
            AddTopLevelConjuncts(logical.Right, applications);
            return;
        }

        if (predicate is SourcePredicateStringMatch stringMatch)
            applications.Add(new SourcePredicateApplication(stringMatch, SourcePredicateEvaluationPhase.RowFiltering));
    }
}
