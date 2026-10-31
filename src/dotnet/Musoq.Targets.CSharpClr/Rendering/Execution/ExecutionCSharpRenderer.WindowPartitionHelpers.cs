using System.Diagnostics.CodeAnalysis;
using Musoq.Evaluator.Helpers;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private static ExecutionVariable? CreateWindowPartitionBuilderVariable(
        ExecutionVariable results,
        ExecutionWindowKeyArray? partitionKeys,
        ExecutionWindowPartitionSet? partitions)
    {
        if (partitionKeys is not { ShouldExtract: true, ShouldMaterialize: false } ||
            partitions is not { ShouldCreate: true })
        {
            return null;
        }

        if (HasGeneratedWindowKeyType(partitionKeys))
        {
            return new ExecutionVariable(
                $"{results.Name}PartitionBuilder",
                typeof(object),
                $"{typeof(WindowPartitionBuilder<>).Namespace}.{nameof(WindowPartitionBuilder<object>)}<{partitionKeys.Shape!.GeneratedElementTypeName}>");
        }

        var keyType = GetArrayElementType(partitionKeys.Variable);
        return new ExecutionVariable(
            $"{results.Name}PartitionBuilder",
            typeof(WindowPartitionBuilder<>).MakeGenericType(keyType));
    }

    private static ExecutionVariable CreateFusedIntOrderBuilderVariable(
        ExecutionComputeRankingWindow ranking,
        ExecutionWindowKeyArray? partitionKeys)
    {
        return CreateFusedIntOrderBuilderVariable(ranking.Results, partitionKeys);
    }

    private static ExecutionVariable CreateFusedIntOrderBuilderVariable(
        ExecutionVariable results,
        ExecutionWindowKeyArray? partitionKeys)
    {
        var builderType = partitionKeys == null
            ? typeof(WindowIntOrderBuilder)
            : typeof(WindowIntOrderBuilder<>).MakeGenericType(GetArrayElementType(partitionKeys.Variable));

        if (partitionKeys != null && HasGeneratedWindowKeyType(partitionKeys))
        {
            return new ExecutionVariable(
                $"{results.Name}IntOrderBuilder",
                typeof(object),
                $"{typeof(WindowIntOrderBuilder<>).Namespace}.{nameof(WindowIntOrderBuilder<object>)}<{partitionKeys.Shape!.GeneratedElementTypeName}>");
        }

        return new ExecutionVariable($"{results.Name}IntOrderBuilder", builderType);
    }

    private static bool ShouldMaterializeWindowKeyArray([NotNullWhen(true)] ExecutionWindowKeyArray? keyArray)
    {
        return keyArray is { ShouldExtract: true, ShouldMaterialize: true };
    }
}
