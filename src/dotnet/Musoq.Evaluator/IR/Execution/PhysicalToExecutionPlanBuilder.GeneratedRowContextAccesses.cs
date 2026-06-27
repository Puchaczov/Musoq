using System.Collections.Generic;
using System.Linq;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class PhysicalToExecutionPlanBuilder
{
    private static IReadOnlyList<FieldBinding> CreateTypedStoredGeneratedRowContextBindings(
        GeneratedRowShape rowShape)
    {
        if (!rowShape.SupportsGeneratedFieldAccess || rowShape.Contexts.Count == 0)
            return rowShape.Contexts;

        return rowShape.Contexts
            .Select((context, index) => context with
            {
                AccessStrategy = new GeneratedRowContextAccess(rowShape.TypeName, index)
            })
            .ToArray();
    }
}
