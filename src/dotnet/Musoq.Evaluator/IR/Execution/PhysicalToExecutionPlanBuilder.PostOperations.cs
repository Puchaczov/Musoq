using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Bindings;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class PhysicalToExecutionPlanBuilder
{
    private static PostOperationResult CreatePostOperation(
        PostOperation operation,
        ExecutionVariable sourceTable,
        GeneratedRowShape rowShape)
    {
        return operation switch
        {
            SortOperation sort => CreateSortOperation(sort, sourceTable, rowShape),
            TopNOperation topN => CreateTopNOperation(topN, sourceTable, rowShape),
            TopOffsetOperation topOffset => CreateTopOffsetOperation(topOffset, sourceTable, rowShape),
            SkipOperation skip => CreateSkipOperation(skip, sourceTable, rowShape),
            TakeOperation take => CreateTakeOperation(take, sourceTable, rowShape),
            SliceOperation slice => CreateSliceOperation(slice, sourceTable, rowShape),
            _ => PostOperationResult.Unsupported($"Execution IR post operation '{operation.GetType().Name}' is not supported.")
        };
    }

    private static IReadOnlyList<PostOperation> CreatePostOperations(
        List<PostOperation> operations,
        IReadOnlyList<ProjectedField> projectedFields)
    {
        operations.Reverse();

        var projectedOperations = operations.Any(operation => operation is SortOperation or TopNOperation or TopOffsetOperation)
            ? operations
            .Select(operation => operation switch
            {
                SortOperation sort => sort with { ProjectedFields = projectedFields },
                TopNOperation topN => topN with { ProjectedFields = projectedFields },
                TopOffsetOperation topOffset => topOffset with { ProjectedFields = projectedFields },
                _ => operation
            })
            .ToArray()
            : (IReadOnlyList<PostOperation>)operations;

        return CombineAdjacentSkipTakeOperations(projectedOperations);
    }
}
