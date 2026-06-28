using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Logical.Nodes;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.IR.Planning;

internal static class SetOperationStrategyPlanner
{
    public static SetOperationStrategyDecision Choose(PhysicalSetOperationNode node)
    {
        if (CanStreamUnionAll(node))
        {
            return SetOperationStrategyDecision.StreamingUnionAll(
                "UnionAll arms use directly streamable row sources with optional filters, direct column or literal projections, and no post-operations, so Execution IR can append both arms directly into the result table.");
        }

        if (node.Kind == SetOpKind.UnionAll)
        {
            return SetOperationStrategyDecision.RowComparer(
                "UnionAll uses the materialized comparer strategy because at least one arm is not a directly streamable row-source pipeline with optional filters and direct column or literal projections.");
        }

        if (TryGetNanSensitiveKeyType(node.FieldIndexes, node.FieldTypes, out var nanSensitiveType))
        {
            return SetOperationStrategyDecision.RowComparer(
                $"Set operation key type '{FormatType(nanSensitiveType)}' uses NaN equality semantics that must match the row-comparer path, so HashSet lowering is skipped.");
        }

        if (CanUseHashSetSetOperation(node.FieldIndexes))
        {
            return SetOperationStrategyDecision.HashSet(
                $"Set operation has {node.FieldIndexes.Length.ToString(CultureInfo.InvariantCulture)} key field(s), so Execution IR can use the HashSet strategy.");
        }

        return SetOperationStrategyDecision.RowComparer(
            "Set operation has no key fields, so the materialized comparer strategy compares complete rows.");
    }

    private static bool CanStreamUnionAll(PhysicalSetOperationNode node)
    {
        if (node.Kind != SetOpKind.UnionAll)
            return false;

        var leftPipeline = ExecutionStrategyPipelineDecomposer.TryDecomposeSupportedPipeline(
            ExecutionStrategyPipelineDecomposer.UnwrapSingleStatement(node.Left));
        var rightPipeline = ExecutionStrategyPipelineDecomposer.TryDecomposeSupportedPipeline(
            ExecutionStrategyPipelineDecomposer.UnwrapSingleStatement(node.Right));

        return leftPipeline != null &&
               rightPipeline != null &&
               CanStreamUnionAllArm(leftPipeline) &&
               CanStreamUnionAllArm(rightPipeline);
    }

    private static bool CanStreamUnionAllArm(SupportedPipeline pipeline)
    {
        return CanStreamUnionAllSource(pipeline.Source) &&
               !pipeline.Project.IsDistinct &&
               pipeline.PostOperations.Count == 0 &&
               pipeline.Project.Fields.All(static field => field.Expression is ColumnRef or Literal);
    }

    private static bool CanStreamUnionAllSource(PhysicalNode source)
    {
        return source is PhysicalSchemaScanNode
            or PhysicalCteRefNode
            or PhysicalInterpretSourceNode
            or PhysicalPropertySourceNode
            or PhysicalAccessMethodSourceNode;
    }

    private static bool CanUseHashSetSetOperation(int[] fieldIndexes)
    {
        return fieldIndexes.Length >= 1;
    }

    private static bool TryGetNanSensitiveKeyType(
        IReadOnlyList<int> fieldIndexes,
        IReadOnlyList<Type> fieldTypes,
        out Type nanSensitiveType)
    {
        for (var index = 0; index < fieldIndexes.Count && index < fieldTypes.Count; index++)
        {
            var type = fieldTypes[index];
            var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

            if (underlyingType != typeof(float) && underlyingType != typeof(double))
                continue;

            nanSensitiveType = type;
            return true;
        }

        nanSensitiveType = typeof(object);
        return false;
    }

    private static string FormatType(Type type)
    {
        var underlyingType = Nullable.GetUnderlyingType(type);
        if (underlyingType == null)
            return type.Name;

        return $"{underlyingType.Name}?";
    }
}
