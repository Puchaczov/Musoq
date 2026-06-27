using System.Globalization;

namespace Musoq.Evaluator.IR.Execution;

public static partial class ExecutionPlanPrinter
{
    private static string FormatCapacityHint(ExecutionCapacityHint capacityHint)
    {
        return capacityHint switch
        {
            ExecutionConstantCapacityHint constant => constant.Capacity.ToString(CultureInfo.InvariantCulture),
            ExecutionCollectionCountCapacityHint collection => $"{collection.Collection.Name}.Count",
            ExecutionTryGetNonEnumeratedCountCapacityHint enumerable => $"TryCount({enumerable.Collection.Name})",
            ExecutionStoredTableCountCapacityHint storedTable => FormatStoredTableCountCapacityHint(storedTable.TableIndex),
            ExecutionRowsCapacityHintCandidate candidate => $"Candidate({candidate.Target.Name} <- {FormatExpression(candidate.Rows)})",
            ExecutionConstantCapacityHintCandidate candidate => $"Candidate({candidate.Target.Name} <- {candidate.Capacity.ToString(CultureInfo.InvariantCulture)})",
            ExecutionCollectionCountCapacityHintCandidate candidate => $"Candidate({candidate.Target.Name} <- {candidate.Collection.Name}.Count)",
            ExecutionTakeCapacityHintCandidate candidate => $"Candidate({candidate.Target.Name} <- Min({candidate.Collection.Name}.Count, {candidate.Count.ToString(CultureInfo.InvariantCulture)}))",
            ExecutionSkipCapacityHintCandidate candidate => $"Candidate({candidate.Target.Name} <- Max({candidate.Collection.Name}.Count - {candidate.Count.ToString(CultureInfo.InvariantCulture)}, 0))",
            ExecutionSkipTakeCapacityHintCandidate candidate => $"Candidate({candidate.Target.Name} <- Min(Max({candidate.Collection.Name}.Count - {candidate.SkipCount.ToString(CultureInfo.InvariantCulture)}, 0), {candidate.TakeCount.ToString(CultureInfo.InvariantCulture)}))",
            ExecutionTakeCapacityHint take => $"Min({take.Collection.Name}.Count, {take.Count.ToString(CultureInfo.InvariantCulture)})",
            ExecutionSkipCapacityHint skip => $"Max({skip.Collection.Name}.Count - {skip.Count.ToString(CultureInfo.InvariantCulture)}, 0)",
            ExecutionSkipTakeCapacityHint skipTake => $"Min(Max({skipTake.Collection.Name}.Count - {skipTake.SkipCount.ToString(CultureInfo.InvariantCulture)}, 0), {skipTake.TakeCount.ToString(CultureInfo.InvariantCulture)})",
            _ => capacityHint.GetType().Name
        };
    }

    private static string FormatOptionalCapacity(ExecutionCapacityHint? capacityHint)
    {
        return capacityHint == null
            ? string.Empty
            : $"; capacity: {FormatCapacityHint(capacityHint)}";
    }

    private static string FormatOptionalCandidateCapacity(ExecutionCapacityHint? capacityHint)
    {
        return capacityHint != null && ExecutionCapacityHintCandidates.IsCandidate(capacityHint)
            ? $"; capacity: {FormatCapacityHint(capacityHint)}"
            : string.Empty;
    }

    private static string FormatStoredTableCountCapacityHint(int tableIndex)
    {
        return TryGetTypedStoredTableSlot(tableIndex, out _)
            ? $"{FormatCteRowResultSlot(tableIndex)}.Count"
            : $"_tableResults[{tableIndex.ToString(CultureInfo.InvariantCulture)}].Count";
    }
}
