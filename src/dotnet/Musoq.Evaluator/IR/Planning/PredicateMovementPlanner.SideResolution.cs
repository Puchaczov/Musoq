using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Logical;
using Musoq.Evaluator.IR.Logical.Nodes;
using ColumnRefExtractor = Musoq.Evaluator.IR.Expressions.ColumnRefExtractor;

namespace Musoq.Evaluator.IR.Planning;

internal static partial class PredicateMovementPlanner
{
    private static SideResolution ResolveSidePlacement(
        LogicalNode node,
        PredicateMovementSide side,
        string alias,
        IrExpression predicate)
    {
        switch (node)
        {
            case SchemaScanNode scan:
                if (string.Equals(scan.Alias, alias, StringComparison.OrdinalIgnoreCase))
                    return SideResolution.Eligible(side, $"The {alias} source is directly available below the join side.");

                return SideResolution.NotEligible($"The {alias} source is not the direct scan for this join side.");
            case ValuesScanNode values:
                if (string.Equals(values.Alias, alias, StringComparison.OrdinalIgnoreCase))
                    return SideResolution.Eligible(side, $"The {alias} values source is directly available below the join side.");

                return SideResolution.NotEligible($"The {alias} source is not the direct values source for this join side.");
            case FilterNode filter:
                return ResolveSidePlacement(filter.Input, side, alias, predicate);
            case SortNode sort:
                return ResolveSidePlacement(sort.Input, side, alias, predicate)
                    .AddReason("Predicate can move through a transparent sort boundary.");
            case ProjectNode project when ProjectPreservesPredicateColumns(project, predicate):
                return ResolveSidePlacement(project.Input, side, alias, predicate)
                    .AddReason("Predicate can move through a transparent direct-column project boundary.");
            case ProjectNode:
                return SideResolution.NotEligible($"The {alias} side contains a project that does not preserve every predicate column directly.");
            default:
                return SideResolution.NotEligible($"The {alias} side contains {node.GetType().Name}, so movement stays conservative.");
        }
    }

    private static bool ProjectPreservesPredicateColumns(ProjectNode project, IrExpression predicate)
    {
        var refs = ColumnRefExtractor.Extract(predicate);
        return refs.Count > 0 &&
               refs.All(column => ProjectPreservesColumn(project.Fields, column));
    }

    private static bool ProjectPreservesColumn(
        IReadOnlyList<ProjectedField> fields,
        ColumnRef column)
    {
        return fields.Any(field =>
            field.Expression is ColumnRef projected &&
            string.Equals(projected.Alias, column.Alias, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(projected.ColumnName, column.ColumnName, StringComparison.OrdinalIgnoreCase));
    }
}
