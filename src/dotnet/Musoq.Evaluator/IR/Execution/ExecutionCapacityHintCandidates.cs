namespace Musoq.Evaluator.IR.Execution;

internal static class ExecutionCapacityHintCandidates
{
    public static ExecutionCapacityHint? CreateRowsCandidate(
        ExecutionVariable target,
        ExecutionExpression rows)
    {
        return rows is ExecutionStoredTableRows or ExecutionRowStream { Kind: ExecutionRowStreamKind.Rows }
            ? new ExecutionRowsCapacityHintCandidate(target, rows)
            : null;
    }

    public static ExecutionCapacityHint CreateConstantCandidate(
        ExecutionVariable target,
        int capacity)
    {
        return new ExecutionConstantCapacityHintCandidate(target, capacity);
    }

    public static ExecutionCapacityHint CreateCollectionCountCandidate(
        ExecutionVariable target,
        ExecutionVariable collection)
    {
        return new ExecutionCollectionCountCapacityHintCandidate(target, collection);
    }

    public static ExecutionCapacityHint CreateTakeCandidate(
        ExecutionVariable target,
        ExecutionVariable collection,
        int count)
    {
        return new ExecutionTakeCapacityHintCandidate(target, collection, count);
    }

    public static ExecutionCapacityHint CreateSkipCandidate(
        ExecutionVariable target,
        ExecutionVariable collection,
        int count)
    {
        return new ExecutionSkipCapacityHintCandidate(target, collection, count);
    }

    public static ExecutionCapacityHint CreateSkipTakeCandidate(
        ExecutionVariable target,
        ExecutionVariable collection,
        int skipCount,
        int takeCount)
    {
        return new ExecutionSkipTakeCapacityHintCandidate(target, collection, skipCount, takeCount);
    }

    public static bool IsCandidate(ExecutionCapacityHint capacityHint)
    {
        return capacityHint is ExecutionRowsCapacityHintCandidate or
            ExecutionConstantCapacityHintCandidate or
            ExecutionCollectionCountCapacityHintCandidate or
            ExecutionTakeCapacityHintCandidate or
            ExecutionSkipCapacityHintCandidate or
            ExecutionSkipTakeCapacityHintCandidate;
    }

    public static string GetCandidateDiagnosticName(ExecutionCapacityHint capacityHint)
    {
        return capacityHint switch
        {
            ExecutionRowsCapacityHintCandidate => "Rows",
            ExecutionConstantCapacityHintCandidate => "Constant",
            ExecutionCollectionCountCapacityHintCandidate => "CollectionCount",
            ExecutionTakeCapacityHintCandidate => "Take",
            ExecutionSkipCapacityHintCandidate => "Skip",
            ExecutionSkipTakeCapacityHintCandidate => "SkipTake",
            _ => "Unknown"
        };
    }

    public static bool TryLower(
        ExecutionCapacityHint candidate,
        out ExecutionCapacityHint? capacityHint)
    {
        return candidate switch
        {
            ExecutionRowsCapacityHintCandidate rows => TryLower(rows, out capacityHint),
            ExecutionConstantCapacityHintCandidate constant => Lower(
                new ExecutionConstantCapacityHint(constant.Capacity),
                out capacityHint),
            ExecutionCollectionCountCapacityHintCandidate collection => Lower(
                new ExecutionCollectionCountCapacityHint(collection.Collection),
                out capacityHint),
            ExecutionTakeCapacityHintCandidate take => Lower(
                new ExecutionTakeCapacityHint(take.Collection, take.Count),
                out capacityHint),
            ExecutionSkipCapacityHintCandidate skip => Lower(
                new ExecutionSkipCapacityHint(skip.Collection, skip.Count),
                out capacityHint),
            ExecutionSkipTakeCapacityHintCandidate skipTake => Lower(
                new ExecutionSkipTakeCapacityHint(skipTake.Collection, skipTake.SkipCount, skipTake.TakeCount),
                out capacityHint),
            _ => Lower(null, out capacityHint)
        };
    }

    public static bool TryLower(
        ExecutionRowsCapacityHintCandidate candidate,
        out ExecutionCapacityHint? capacityHint)
    {
        capacityHint = candidate.Rows switch
        {
            ExecutionStoredTableRows storedRows => new ExecutionStoredTableCountCapacityHint(storedRows.TableIndex),
            ExecutionRowStream { Kind: ExecutionRowStreamKind.Rows } materializedRows => new ExecutionCollectionCountCapacityHint(materializedRows.Variable),
            _ => null
        };

        return capacityHint != null;
    }

    private static bool Lower(ExecutionCapacityHint? lowered, out ExecutionCapacityHint? capacityHint)
    {
        capacityHint = lowered;
        return capacityHint != null;
    }

    public static string CreateCapacityVariableName(ExecutionVariable target)
    {
        return ExecutionSymbolicNamePolicy.CreateLoweringIdentifierCandidate(
            $"{target.Name}Capacity",
            0);
    }
}
