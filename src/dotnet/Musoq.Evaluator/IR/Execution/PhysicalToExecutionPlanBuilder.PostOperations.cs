using System.Collections.Generic;
using Musoq.Evaluator.IR.Bindings;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class PhysicalToExecutionPlanBuilder
{
    private static IReadOnlyList<PostOperation> CreatePostOperations(
        List<PostOperation> operations,
        IReadOnlyList<ProjectedField> projectedFields)
    {
        return PostOperationPlanner.Default.CreatePostOperations(operations, projectedFields);
    }
}
