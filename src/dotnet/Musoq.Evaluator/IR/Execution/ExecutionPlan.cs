using System.Collections.Generic;
using System.Linq;
using Musoq.Targets.Abstractions;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionPlan
{
    public ExecutionPlan(
        string identifier,
        IReadOnlyList<RowShape> shapes,
        ExecutionBlock body,
        FinalShapeResult? finalResult = null,
        ExecutionSemanticsContract? semanticsContract = null,
        int executionIrVersion = TargetContractVersions.ExecutionIr,
        IEnumerable<ExecutionStoredTableRepresentationPlan>? storedTableRepresentations = null)
    {
        if (executionIrVersion <= 0)
            throw new ArgumentOutOfRangeException(nameof(executionIrVersion));

        Identifier = identifier;
        Shapes = ExecutionIrCollections.Freeze(shapes);
        Body = body;
        FinalResult = finalResult;
        SemanticsContract = semanticsContract ?? ExecutionSemanticsContract.Version1;
        ExecutionIrVersion = executionIrVersion;
        StoredTableRepresentations = storedTableRepresentations?.ToArray() ?? [];
    }

    public string Identifier { get; init; }

    private IReadOnlyList<RowShape> _shapes = [];

    public IReadOnlyList<RowShape> Shapes
    {
        get => _shapes;
        init => _shapes = ExecutionIrCollections.Freeze(value);
    }

    public ExecutionBlock Body { get; init; }

    public FinalShapeResult? FinalResult { get; init; }

    public ExecutionSemanticsContract SemanticsContract { get; init; }

    public int ExecutionIrVersion { get; init; }

    private IReadOnlyList<ExecutionStoredTableRepresentationPlan> _storedTableRepresentations = [];

    /// <summary>
    /// Gets the immutable storage decisions selected by the planner for each
    /// stored CTE table. Targets consume this map without rediscovering it.
    /// </summary>
    public IReadOnlyList<ExecutionStoredTableRepresentationPlan> StoredTableRepresentations
    {
        get => _storedTableRepresentations;
        init
        {
            ArgumentNullException.ThrowIfNull(value);
            var frozen = ExecutionIrCollections.Freeze(value);
            if (frozen.Select(static representation => representation.TableIndex).Distinct().Count() != frozen.Count)
                throw new ArgumentException("Stored table representation indexes must be unique.", nameof(value));

            _storedTableRepresentations = frozen;
        }
    }
}
