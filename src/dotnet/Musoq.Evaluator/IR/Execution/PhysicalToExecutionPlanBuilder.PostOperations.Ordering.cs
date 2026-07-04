using System.Collections.Generic;
using System.Reflection;
using Musoq.Evaluator.IR.Bindings;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class PhysicalToExecutionPlanBuilder
{
    private static IReadOnlyList<int> ResolveRenumberFieldIndexes(
        IReadOnlyList<ProjectedField> projectedFields,
        GeneratedRowShape rowShape)
    {
        return PostOperationPlanner.ResolveRenumberFieldIndexes(projectedFields, rowShape);
    }

    private static bool IsRowNumberMethod(MethodInfo method)
    {
        return PostOperationPlanner.IsRowNumberMethod(method);
    }
}
