using System.Collections.Generic;
using Musoq.Evaluator.IR.Bindings;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class PhysicalToExecutionPlanBuilder
{
    private static PostOperationResult CreatePostOperation(
        PostOperation operation,
        ExecutionVariable sourceTable,
        GeneratedRowShape rowShape)
    {
        return PostOperationPlanner.Default.CreatePostOperation(operation, sourceTable, rowShape);
    }

    private static IReadOnlyList<PostOperation> CreatePostOperations(
        List<PostOperation> operations,
        IReadOnlyList<ProjectedField> projectedFields)
    {
        return PostOperationPlanner.Default.CreatePostOperations(operations, projectedFields);
    }
}
