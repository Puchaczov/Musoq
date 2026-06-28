using System.Collections.Generic;
using System.Globalization;
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
                "UnionAll arms use directly streamable row sources with optional filters, projected expressions, and no post-operations, so Execution IR can append both arms directly into the result table.");
        }

        if (node.Kind == SetOpKind.UnionAll)
        {
            return SetOperationStrategyDecision.AppendLoop(
                "UnionAll uses generated append lowering because at least one arm is not a directly streamable row-source pipeline with optional filters, projected expressions, and no post-operations.");
        }

        if (TryGetNanSensitiveKeyType(node.FieldIndexes, node.FieldTypes, out var nanSensitiveType))
        {
            return SetOperationStrategyDecision.GeneratedEqualityLoop(
                $"Set operation key type '{FormatType(nanSensitiveType)}' uses NaN-sensitive equality semantics, so Execution IR emits an explicit generated equality loop instead of HashSet lowering.");
        }

        if (CanUseHashSetSetOperation(node.FieldIndexes))
        {
            return SetOperationStrategyDecision.HashSet(
                $"Set operation has {node.FieldIndexes.Length.ToString(CultureInfo.InvariantCulture)} key field(s), so Execution IR can use the HashSet strategy.");
        }

        return SetOperationStrategyDecision.GeneratedEqualityLoop(
            "Set operation has no key fields, so Execution IR emits an explicit generated equality loop over complete rows.");
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
               pipeline.PostOperations.Count == 0;
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
