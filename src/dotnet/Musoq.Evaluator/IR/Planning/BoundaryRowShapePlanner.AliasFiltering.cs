using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Physical;

namespace Musoq.Evaluator.IR.Planning;

internal static partial class BoundaryRowShapePlanner
{
    private static string[] FilterColumnsProducedBy(PhysicalNode node, IReadOnlyList<string> columns)
    {
        return columns
            .Where(column =>
            {
                var alias = GetAlias(column);
                return string.IsNullOrWhiteSpace(alias) || ProducesAlias(node, alias);
            })
            .ToArray();
    }

    private static bool IsCurrentlyPrunableBoundary(BoundaryRowShapeKind kind)
    {
        return kind is BoundaryRowShapeKind.Sort
            or BoundaryRowShapeKind.TopN
            or BoundaryRowShapeKind.TopOffset
            or BoundaryRowShapeKind.Aggregate
            or BoundaryRowShapeKind.Distinct
            or BoundaryRowShapeKind.SetOperation
            or BoundaryRowShapeKind.HashJoinBuild
            or BoundaryRowShapeKind.HashJoinProbe
            or BoundaryRowShapeKind.CteMaterialization
            or BoundaryRowShapeKind.Window;
    }
}
