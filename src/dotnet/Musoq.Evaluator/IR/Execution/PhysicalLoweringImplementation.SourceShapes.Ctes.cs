using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.IR.Execution;

internal sealed partial class PhysicalLoweringImplementation
{
    private static TableRowShape? ResolveCteSourceShape(
        PhysicalCteRefNode cteRef,
        IReadOnlyDictionary<string, int> cteIndexes,
        IReadOnlyDictionary<string, GeneratedRowShape>? cteShapesByName
    )
    {
        var storedShape = ResolveCteStoredRowShape(cteRef, cteShapesByName);
        if (storedShape != null)
        {
            var shape = CanUseTypedStoredRows(storedShape)
                ? CreateTypedTableRowShape(cteRef, storedShape)
                : CreateMaterializedTransitionTableRowShape(cteRef.Alias, storedShape);
            if (
                cteRef.IntermediateProducerId != null
                || !shape.Contexts.Any(static slot => slot.RowPresenceAlias != null)
            )
                return shape;

            // Presence bindings inside the named definition are outside the caller's alias scope.
            return shape with
            {
                Contexts = shape.Contexts.Select(static slot => slot with { RowPresenceAlias = null }).ToArray(),
            };
        }

        return cteIndexes.ContainsKey(cteRef.CteName) ? CreateTableRowShape(cteRef) : null;
    }

    private static GeneratedRowShape? ResolveCteGeneratedRowShape(
        PhysicalCteRefNode cteRef,
        IReadOnlyDictionary<string, GeneratedRowShape>? cteShapesByName
    )
    {
        var rowShape = ResolveCteStoredRowShape(cteRef, cteShapesByName);

        return rowShape != null && CanUseTypedStoredRows(rowShape) ? rowShape : null;
    }

    private static GeneratedRowShape? ResolveCteStoredRowShape(
        PhysicalCteRefNode cteRef,
        IReadOnlyDictionary<string, GeneratedRowShape>? cteShapesByName
    )
    {
        return cteShapesByName != null && cteShapesByName.TryGetValue(cteRef.CteName, out var rowShape)
            ? rowShape
            : null;
    }

    private static bool CanUseTypedStoredRows(GeneratedRowShape rowShape)
    {
        return rowShape.SupportsGeneratedFieldAccess
            && !rowShape.Contexts.Any(static context =>
                DynamicEntityBoundary.IsStringObjectDictionaryContext(context.Type.ResolveClrType())
            );
    }
}
