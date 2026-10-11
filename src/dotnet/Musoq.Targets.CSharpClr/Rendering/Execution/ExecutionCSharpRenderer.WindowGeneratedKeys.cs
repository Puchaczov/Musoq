using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.IR.Bindings;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    internal static bool HasGeneratedWindowKeyType(ExecutionWindowKeyArray? keyArray)
    {
        return !string.IsNullOrWhiteSpace(keyArray?.Shape?.GeneratedElementTypeName);
    }

    private static ExecutionWindowKeyArray? ResolveGeneratedWindowPartitionKeyArray(
        ExecutionExpression? partitionKey,
        ExecutionWindowKeyArray? partitionKeyArray,
        string defaultVariableName)
    {
        if (partitionKey == null)
            return null;
        if (partitionKeyArray?.Shape is { IsTyped: true })
            return partitionKeyArray;
        throw UnsupportedShape.Of($"Window partition key '{defaultVariableName}' has no lowered concrete carrier.");
    }

    private static ExecutionWindowKeyArray ResolveGeneratedWindowOrderKeyArray(
        IReadOnlyList<ExecutionWindowOrderKey> orderKeyExpressions,
        ExecutionWindowKeyArray? orderKeyArray,
        string defaultVariableName)
    {
        if (orderKeyArray?.Shape is { IsGeneratedOrderKey: true, GeneratedElementTypeName: not null } shape &&
            shape.GeneratedParts?.Count == orderKeyExpressions.Count)
            return orderKeyArray;

        throw UnsupportedShape.Of($"Window order key '{defaultVariableName}' has no lowered concrete carrier.");
    }

    private static ExecutionWindowKeyArray? ResolveRankingPartitionKeyArray(ExecutionComputeRankingWindow ranking)
    {
        return ResolveGeneratedWindowPartitionKeyArray(
            ranking.PartitionKey,
            ranking.PartitionKeyArray,
            $"{ranking.Results.Name}PartitionKeys");
    }

    private static ExecutionWindowKeyArray ResolveRankingOrderKeyArray(ExecutionComputeRankingWindow ranking)
    {
        return ResolveGeneratedWindowOrderKeyArray(
            ranking.OrderKeys,
            ranking.OrderKeyArray,
            $"{ranking.Results.Name}OrderKeys");
    }

    private static ExecutionWindowKeyArray? ResolveOffsetPartitionKeyArray(ExecutionComputeOffsetWindow offset)
    {
        return ResolveGeneratedWindowPartitionKeyArray(
            offset.PartitionKey,
            offset.PartitionKeyArray,
            $"{offset.Results.Name}PartitionKeys");
    }

    private static ExecutionWindowKeyArray ResolveOffsetOrderKeyArray(ExecutionComputeOffsetWindow offset)
    {
        return ResolveGeneratedWindowOrderKeyArray(
            offset.OrderKeys,
            offset.OrderKeyArray,
            $"{offset.Results.Name}OrderKeys");
    }

    private static ExecutionWindowKeyArray? ResolvePluginPartitionKeyArray(ExecutionComputePluginWindow plugin)
    {
        return ResolveGeneratedWindowPartitionKeyArray(
            plugin.PartitionKey,
            plugin.PartitionKeyArray,
            $"{plugin.Results.Name}PartitionKeys");
    }

    private static ExecutionWindowKeyArray? ResolvePluginOrderKeyArray(ExecutionComputePluginWindow plugin)
    {
        return plugin.OrderKeys.Count == 0
            ? null
            : ResolveGeneratedWindowOrderKeyArray(
                plugin.OrderKeys,
                plugin.OrderKeyArray,
                $"{plugin.Results.Name}OrderKeys");
    }

    private static ExecutionWindowKeyArray? ResolveAggregatePartitionKeyArray(ExecutionWindowAggregateKernel aggregate)
    {
        return ResolveGeneratedWindowPartitionKeyArray(
            aggregate.PartitionKey,
            aggregate.PartitionKeyArray,
            $"{aggregate.Results.Name}PartitionKeys");
    }

    private static ExecutionWindowKeyArray? ResolveAggregateOrderKeyArray(ExecutionWindowAggregateKernel aggregate)
    {
        return aggregate.OrderKeys.Count == 0
            ? null
            : ResolveGeneratedWindowOrderKeyArray(
                aggregate.OrderKeys,
                aggregate.OrderKeyArray,
                $"{aggregate.Results.Name}OrderKeys");
    }

    private static IReadOnlyList<ExecutionExpression> GetWindowKeyParts(ExecutionExpression expression)
    {
        return expression switch
        {
            ExecutionValueTupleKey valueTupleKey => valueTupleKey.Parts,
            _ => [expression]
        };
    }

    private ObjectCreationExpressionSyntax CreateGeneratedWindowKeyCreation(
        ExecutionWindowKeyArray keyArray,
        IReadOnlyList<ExecutionExpression> parts)
    {
        var typeName = keyArray.Shape?.GeneratedElementTypeName ??
                       throw new InvalidOperationException("Generated window key type is required.");

        return SyntaxFactory.ObjectCreationExpression(SyntaxFactory.ParseTypeName(typeName))
            .WithArgumentList(CreateArgumentList(parts.Select(RenderExpression)));
    }

    private ExpressionSyntax CreateWindowPartitionKeyExpression(
        ExecutionWindowKeyArray partitionKeys,
        ExecutionExpression partitionKey)
    {
        return HasGeneratedWindowKeyType(partitionKeys)
            ? CreateGeneratedWindowKeyCreation(partitionKeys, GetWindowKeyParts(partitionKey))
            : RenderExpression(partitionKey);
    }

    private ExpressionSyntax CreateWindowOrderKeyExpression(
        ExecutionWindowKeyArray orderKeys,
        IReadOnlyList<ExecutionWindowOrderKey> orderKeyExpressions)
    {
        return CreateGeneratedWindowKeyCreation(orderKeys,
            orderKeyExpressions.Select(static key => key.Expression).ToArray());
    }

    private ExpressionSyntax CreateRankingPartitionKeyExpression(
        ExecutionWindowKeyArray partitionKeys,
        ExecutionExpression partitionKey)
    {
        return CreateWindowPartitionKeyExpression(partitionKeys, partitionKey);
    }

    private ExpressionSyntax CreateRankingOrderKeyExpression(
        ExecutionWindowKeyArray orderKeys,
        IReadOnlyList<ExecutionWindowOrderKey> orderKeyExpressions)
    {
        return CreateWindowOrderKeyExpression(orderKeys, orderKeyExpressions);
    }

    private static TypeSyntax CreateWindowKeyElementTypeSyntax(ExecutionWindowKeyArray keyArray)
    {
        return HasGeneratedWindowKeyType(keyArray)
            ? SyntaxFactory.ParseTypeName(keyArray.Shape!.GeneratedElementTypeName!)
            : CreateTypeSyntax(GetArrayElementType(keyArray.Variable));
    }

    private static ArrayCreationExpressionSyntax CreateWindowKeyArrayCreation(
        ExecutionWindowKeyArray keyArray,
        ExpressionSyntax size)
    {
        return HasGeneratedWindowKeyType(keyArray)
            ? SyntaxFactory.ArrayCreationExpression(
                SyntaxFactory.ArrayType(CreateWindowKeyElementTypeSyntax(keyArray))
                    .WithRankSpecifiers(SyntaxFactory.SingletonList(
                        SyntaxFactory.ArrayRankSpecifier(SyntaxFactory.SingletonSeparatedList(size)))))
            : CreateWindowKeyArrayCreation(keyArray.Variable, size);
    }

    private static ExpressionStatementSyntax CreateWindowKeyArrayAssignment(
        ExecutionWindowKeyArray keyArray,
        string indexVariableName,
        ExpressionSyntax value)
    {
        if (!HasGeneratedWindowKeyType(keyArray))
            return CreateWindowKeyArrayAssignment(keyArray.Variable, indexVariableName, value);

        return SyntaxFactory.ExpressionStatement(
            SyntaxFactory.AssignmentExpression(
                SyntaxKind.SimpleAssignmentExpression,
                CreateElementAccess(
                    SyntaxFactory.IdentifierName(keyArray.Variable.Name),
                    SyntaxFactory.IdentifierName(indexVariableName)),
                value));
    }

    private IEnumerable<GeneratedWindowKeyStruct> CollectGeneratedWindowKeyStructs(ExecutionBlock block)
    {
        var structs = new Dictionary<string, GeneratedWindowKeyStruct>(StringComparer.Ordinal);

        foreach (var node in FlattenNodes(block))
        {
            foreach (var usage in ResolveGeneratedWindowKeyArrays(node))
            {
                var keyArray = usage.KeyArray;
                if (!HasGeneratedWindowKeyType(keyArray))
                    continue;

                var typeName = keyArray.Shape!.GeneratedElementTypeName!;
                if (!structs.TryGetValue(typeName, out var existing))
                {
                    structs.Add(typeName, CreateGeneratedWindowKeyStruct(keyArray) with
                    {
                        NeedsPeerEquals = usage.NeedsPeerEquals
                    });
                    continue;
                }

                if (usage.NeedsPeerEquals && !existing.NeedsPeerEquals)
                    structs[typeName] = existing with { NeedsPeerEquals = true };
            }
        }

        foreach (var key in structs.Values)
            yield return key;
    }

    private static IEnumerable<GeneratedWindowKeyArrayUsage> ResolveGeneratedWindowKeyArrays(ExecutionNode node)
    {
        switch (node)
        {
            case ExecutionComputeRankingWindow ranking:
                var rankingPartitionKeys = ResolveRankingPartitionKeyArray(ranking);
                if (CanUseFusedIntOrderRankingWindow(ranking))
                {
                    if (rankingPartitionKeys != null)
                        yield return new GeneratedWindowKeyArrayUsage(rankingPartitionKeys, false);

                    break;
                }

                if (rankingPartitionKeys != null)
                    yield return new GeneratedWindowKeyArrayUsage(rankingPartitionKeys, false);

                yield return new GeneratedWindowKeyArrayUsage(
                    ResolveRankingOrderKeyArray(ranking),
                    ranking.LibraryGet is not null && ranking.RequiresPeerMetadata ||
                    ranking.Function is not ExecutionRankingWindowFunction.RowNumber);
                break;

            case ExecutionComputeOffsetWindow offset:
                var offsetPartitionKeys = ResolveOffsetPartitionKeyArray(offset);
                if (offsetPartitionKeys != null)
                    yield return new GeneratedWindowKeyArrayUsage(offsetPartitionKeys, false);

                yield return new GeneratedWindowKeyArrayUsage(ResolveOffsetOrderKeyArray(offset), false);
                break;

            case ExecutionComputePluginWindow plugin when plugin.LibraryGet is not null:
                var pluginPartitionKeys = ResolvePluginPartitionKeyArray(plugin);
                if (pluginPartitionKeys != null)
                    yield return new GeneratedWindowKeyArrayUsage(pluginPartitionKeys, false);

                var pluginOrderKeys = ResolvePluginOrderKeyArray(plugin);
                if (pluginOrderKeys != null)
                    yield return new GeneratedWindowKeyArrayUsage(pluginOrderKeys, false);
                break;

            case ExecutionWindowAggregateKernel aggregate:
                var aggregatePartitionKeys = ResolveAggregatePartitionKeyArray(aggregate);
                if (aggregatePartitionKeys != null)
                    yield return new GeneratedWindowKeyArrayUsage(aggregatePartitionKeys, false);

                var aggregateOrderKeys = ResolveAggregateOrderKeyArray(aggregate);
                if (aggregateOrderKeys != null)
                    yield return new GeneratedWindowKeyArrayUsage(aggregateOrderKeys, false);
                break;
        }
    }

    private sealed record GeneratedWindowKeyArrayUsage(
        ExecutionWindowKeyArray KeyArray,
        bool NeedsPeerEquals);

    private sealed record GeneratedWindowKeyStruct(
        string TypeName,
        bool IsOrderKey,
        IReadOnlyList<ExecutionWindowGeneratedKeyPart> Parts)
    {
        public ExecutionTypeRef? RawValueType { get; init; }
        public bool NeedsPeerEquals { get; init; }
    }
}
