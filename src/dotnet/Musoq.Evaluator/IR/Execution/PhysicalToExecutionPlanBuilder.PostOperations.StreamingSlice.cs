using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Bindings;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class PhysicalToExecutionPlanBuilder
{
    private static IReadOnlyList<PostOperation> CombineAdjacentSkipTakeOperations(
        IReadOnlyList<PostOperation> operations)
    {
        List<PostOperation>? combined = null;

        for (var index = 0; index < operations.Count; index++)
        {
            if (index + 1 < operations.Count &&
                operations[index] is SkipOperation skip &&
                operations[index + 1] is TakeOperation take)
            {
                combined ??= operations.Take(index).ToList();
                combined.Add(new SliceOperation(skip.Count, take.Count));
                index++;
                continue;
            }

            combined?.Add(operations[index]);
        }

        return combined ?? operations;
    }

    private static StreamingSlice? TryCreateStreamingSlice(
        string resultTableName,
        IReadOnlyList<PostOperation> postOperations,
        bool isDistinct,
        TableProjection? finalProjection,
        IReadOnlyList<ProjectedField> projectedFields,
        out IReadOnlyList<PostOperation> remainingPostOperations)
    {
        remainingPostOperations = postOperations;

        if (isDistinct ||
            finalProjection != null ||
            postOperations.Count != 1 ||
            projectedFields.Any(static field => IsRowNumberProjection(field.Expression)))
        {
            return null;
        }

        var skipCount = 0;
        int? takeCount = null;

        switch (postOperations[0])
        {
            case SkipOperation skip:
                skipCount = skip.Count;
                break;
            case TakeOperation take:
                takeCount = take.Count;
                break;
            case SliceOperation slice:
                skipCount = slice.SkipCount;
                takeCount = slice.TakeCount;
                break;
            default:
                return null;
        }

        if (skipCount <= 0 && takeCount == null)
        {
            remainingPostOperations = [];
            return null;
        }

        var skipRemaining = skipCount > 0
            ? new ExecutionVariable(CreateIdentifierCandidate($"__{resultTableName}SkipRemaining", 0), typeof(int))
            : null;
        var takeRemaining = takeCount != null
            ? new ExecutionVariable(CreateIdentifierCandidate($"__{resultTableName}TakeRemaining", 0), typeof(int))
            : null;

        remainingPostOperations = [];
        return new StreamingSlice(skipCount, takeCount, skipRemaining, takeRemaining);
    }

    private static ExecutionCapacityHint? CreateStreamingSliceCapacityCandidate(
        ExecutionVariable target,
        StreamingSlice? streamingSlice)
    {
        return streamingSlice?.TakeCount is { } takeCount
            ? ExecutionCapacityHintCandidates.CreateConstantCandidate(target, takeCount)
            : null;
    }

    private static IEnumerable<ExecutionNode> CreateStreamingSliceCounterDeclarations(StreamingSlice? streamingSlice)
    {
        if (streamingSlice == null)
            yield break;

        if (streamingSlice.SkipRemaining != null)
        {
            yield return new ExecutionLet(
                streamingSlice.SkipRemaining,
                new ExecutionLiteral(streamingSlice.SkipCount, typeof(int)));
        }

        if (streamingSlice is { TakeRemaining: not null, TakeCount: { } takeCount })
        {
            yield return new ExecutionLet(
                streamingSlice.TakeRemaining,
                new ExecutionLiteral(takeCount, typeof(int)));
        }
    }

    private static PostOperationResult CreateSkipOperation(
        SkipOperation operation,
        ExecutionVariable sourceTable,
        GeneratedRowShape rowShape)
    {
        var target = new ExecutionVariable($"{sourceTable.Name}Skipped", typeof(object));
        return PostOperationResult.Success(
            new ExecutionSkipTable(
                sourceTable,
                target,
                operation.Count,
                ExecutionCapacityHintCandidates.CreateSkipCandidate(target, sourceTable, operation.Count),
                SerialAppendMode,
                CreateTablePostOperationMetadata(target, rowShape)),
            target);
    }

    private static PostOperationResult CreateTakeOperation(
        TakeOperation operation,
        ExecutionVariable sourceTable,
        GeneratedRowShape rowShape)
    {
        var target = new ExecutionVariable($"{sourceTable.Name}Taken", typeof(object));
        return PostOperationResult.Success(
            new ExecutionTakeTable(
                sourceTable,
                target,
                operation.Count,
                ExecutionCapacityHintCandidates.CreateTakeCandidate(target, sourceTable, operation.Count),
                SerialAppendMode,
                CreateTablePostOperationMetadata(target, rowShape)),
            target);
    }

    private static PostOperationResult CreateSliceOperation(
        SliceOperation operation,
        ExecutionVariable sourceTable,
        GeneratedRowShape rowShape)
    {
        var target = new ExecutionVariable($"{sourceTable.Name}Sliced", typeof(object));
        return PostOperationResult.Success(
            new ExecutionSliceTable(
                sourceTable,
                target,
                operation.SkipCount,
                operation.TakeCount,
                ExecutionCapacityHintCandidates.CreateSkipTakeCandidate(
                    target,
                    sourceTable,
                    operation.SkipCount,
                    operation.TakeCount),
                SerialAppendMode,
                CreateTablePostOperationMetadata(target, rowShape)),
            target);
    }
}
