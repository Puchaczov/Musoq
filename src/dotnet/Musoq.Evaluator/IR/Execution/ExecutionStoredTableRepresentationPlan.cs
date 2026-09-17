using System.Linq;

namespace Musoq.Evaluator.IR.Execution;

/// <summary>Identifies the physical representation selected for a stored CTE.</summary>
public enum ExecutionStoredTableRepresentationKind
{
    GeneratedRowList
}

/// <summary>
/// Planner-owned representation metadata for one stored CTE table. Renderers
/// consume this value and must not rediscover row shapes or storage strategy.
/// </summary>
public sealed record ExecutionStoredTableRepresentationPlan
{
    public ExecutionStoredTableRepresentationPlan(
        int tableIndex,
        GeneratedRowShape rowShape,
        ExecutionStoredTableRepresentationKind kind = ExecutionStoredTableRepresentationKind.GeneratedRowList,
        ExecutionStructuralOwnershipMode ownership = ExecutionStructuralOwnershipMode.ConstructFresh,
        ExecutionStructuralPreparationLifetime lifetime = ExecutionStructuralPreparationLifetime.Execution)
    {
        if (tableIndex < 0)
            throw new ArgumentOutOfRangeException(nameof(tableIndex));

        TableIndex = tableIndex;
        RowShape = rowShape ?? throw new ArgumentNullException(nameof(rowShape));
        Kind = kind;
        Ownership = ownership;
        Lifetime = lifetime;
    }

    public int TableIndex { get; }

    public GeneratedRowShape RowShape { get; }

    public ExecutionStoredTableRepresentationKind Kind { get; }

    public ExecutionStructuralOwnershipMode Ownership { get; }

    public ExecutionStructuralPreparationLifetime Lifetime { get; }

    public string Fingerprint =>
        $"table={TableIndex};kind={Kind};row={RowShape.TypeName};" +
        $"fields={string.Join(',', RowShape.Fields.Select(static rowField =>
            $"{rowField.Name}:{rowField.Type.StableId}:{rowField.Nullability}:{rowField.AccessStrategy}"))};" +
        $"ownership={Ownership};lifetime={Lifetime}";
}
