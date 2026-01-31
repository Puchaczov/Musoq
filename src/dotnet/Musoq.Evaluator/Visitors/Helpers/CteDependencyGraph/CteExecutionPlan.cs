using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Musoq.Evaluator.Visitors.Helpers.CteDependencyGraph;

/// <summary>
///     Represents an execution plan for CTE parallelization.
///     Groups CTEs into execution levels where all CTEs at the same level can run in parallel.
/// </summary>
public class CteExecutionPlan
{
    private readonly List<CteExecutionLevel> _levels;

    /// <summary>
    ///     Initializes a new instance of the <see cref="CteExecutionPlan" /> class.
    /// </summary>
    /// <param name="levels">The execution levels.</param>
    /// <param name="graph">The dependency graph this plan was created from.</param>
    public CteExecutionPlan(IEnumerable<CteExecutionLevel> levels, CteDependencyGraph graph)
    {
        _levels = levels.ToList();
        Graph = graph;
    }

    /// <summary>
    ///     Gets the execution levels.
    ///     Level 0 contains CTEs with no dependencies, higher levels depend on lower ones.
    /// </summary>
    public IReadOnlyList<CteExecutionLevel> Levels => _levels;

    /// <summary>
    ///     Gets the dependency graph this plan was created from.
    /// </summary>
    public CteDependencyGraph Graph { get; }

    /// <summary>
    ///     Gets the total number of execution levels.
    /// </summary>
    public int LevelCount => _levels.Count;

    /// <summary>
    ///     Gets the total number of CTEs in the plan.
    /// </summary>
    public int TotalCteCount => _levels.Sum(l => l.Count);

    /// <summary>
    ///     Gets a value indicating whether any parallelization is possible.
    ///     True if at least one level has more than one CTE.
    /// </summary>
    public bool CanParallelize => _levels.Any(l => l.CanParallelize);

    /// <summary>
    ///     Gets the maximum parallelism (largest number of CTEs at any level).
    /// </summary>
    public int MaxParallelism => _levels.Count > 0 ? _levels.Max(l => l.Count) : 0;

    /// <summary>
    ///     Gets a value indicating whether the plan is empty (no CTEs to execute).
    /// </summary>
    public bool IsEmpty => _levels.Count == 0;

    /// <summary>
    ///     Returns a string representation of the execution plan.
    /// </summary>
    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"CteExecutionPlan: {TotalCteCount} CTEs, {LevelCount} levels, CanParallelize={CanParallelize}");
        foreach (var level in _levels) sb.AppendLine($"  {level}");
        return sb.ToString();
    }
}
