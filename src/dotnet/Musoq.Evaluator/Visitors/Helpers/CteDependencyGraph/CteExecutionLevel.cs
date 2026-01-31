using System.Collections.Generic;
using System.Linq;

namespace Musoq.Evaluator.Visitors.Helpers.CteDependencyGraph;

/// <summary>
///     Represents an execution level containing CTEs that can run in parallel.
/// </summary>
public class CteExecutionLevel
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="CteExecutionLevel" /> class.
    /// </summary>
    /// <param name="level">The execution level number (0 = no dependencies).</param>
    /// <param name="ctes">The CTEs at this level.</param>
    public CteExecutionLevel(int level, IReadOnlyList<CteGraphNode> ctes)
    {
        Level = level;
        Ctes = ctes;
    }

    /// <summary>
    ///     Gets the execution level number.
    ///     Level 0 contains CTEs with no dependencies on other CTEs.
    /// </summary>
    public int Level { get; }

    /// <summary>
    ///     Gets the CTEs at this execution level.
    ///     All CTEs in the same level can potentially run in parallel.
    /// </summary>
    public IReadOnlyList<CteGraphNode> Ctes { get; }

    /// <summary>
    ///     Gets the number of CTEs at this level.
    /// </summary>
    public int Count => Ctes.Count;

    /// <summary>
    ///     Gets a value indicating whether this level can benefit from parallelization.
    ///     True if there are multiple CTEs at this level.
    /// </summary>
    public bool CanParallelize => Ctes.Count > 1;

    /// <summary>
    ///     Returns a string representation of this execution level.
    /// </summary>
    public override string ToString()
    {
        var cteNames = string.Join(", ", Ctes.Select(c => c.Name));
        return $"Level {Level}: [{cteNames}] (parallel={CanParallelize})";
    }
}
