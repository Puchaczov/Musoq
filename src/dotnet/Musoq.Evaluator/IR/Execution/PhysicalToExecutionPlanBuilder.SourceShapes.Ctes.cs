using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class PhysicalToExecutionPlanBuilder
{
    private static GeneratedRowShape? ResolveCteGeneratedRowShape(
        PhysicalCteRefNode cteRef,
        IReadOnlyDictionary<string, GeneratedRowShape>? cteShapesByName)
    {
        var rowShape = ResolveCteStoredRowShape(cteRef, cteShapesByName);

        return rowShape != null && CanUseTypedStoredRows(rowShape) ? rowShape : null;
    }

    private static GeneratedRowShape? ResolveCteStoredRowShape(
        PhysicalCteRefNode cteRef,
        IReadOnlyDictionary<string, GeneratedRowShape>? cteShapesByName)
    {
        return cteShapesByName != null && cteShapesByName.TryGetValue(cteRef.CteName, out var rowShape)
            ? rowShape
            : null;
    }

    private static bool CanUseTypedStoredRows(GeneratedRowShape rowShape)
    {
        return rowShape.SupportsGeneratedFieldAccess &&
               !rowShape.Contexts.Any(static context => DynamicEntityBoundary.IsStringObjectDictionaryContext(context.Type.ClrType));
    }
}
