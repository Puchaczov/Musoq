using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Expressions;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class PhysicalToExecutionPlanBuilder
{
    private static ExecutionSourceLoop CreateHashBuildLoop(HashJoinBuildContext context)
    {
        var hashAdd = new ExecutionHashAdd(
            context.Hash,
            CreateHashJoinKeyExpression(context.Join.BuildKeys, context.ConversionLookup, context.KeyType),
            context.Sides.Build.Variable,
            context.KeyType,
            context.Sides.Build.Variable.Type,
            context.Sides.Build.Variable.GeneratedRowTypeName);
        if (context.Sides.Build.FusedHashBuild is not { } fused)
        {
            return CreateSourceLoop(
                context.Sides.Build.Shape,
                context.Sides.Build.Rows,
                context.Sides.Build.Variable,
                new ExecutionBlock([hashAdd]));
        }

        return CreateSourceLoop(
            fused.ProducerShape,
            context.Sides.Build.Rows,
            fused.ProducerVariable,
            new ExecutionBlock(CreateFusedHashBuildNodes(context, fused, hashAdd)));
    }

    private static List<ExecutionNode> CreateFusedHashBuildNodes(
        HashJoinBuildContext context,
        FusedCteHashBuildSource fused,
        ExecutionHashAdd hashAdd)
    {
        if (context.Sides.Build.FusedHashPayload is { } payload)
        {
            var nodes = new List<ExecutionNode>();
            if (TryCreatePrePayloadHashKeyNodes(hashAdd, context.Sides.Build.Variable.Name, fused, nodes, out var keyVariable))
            {
                nodes.Add(new ExecutionCreateHashPayload(
                    context.Sides.Build.Variable,
                    payload.Shape,
                    payload.Values));
                nodes.Add(hashAdd with
                {
                    Key = new ExecutionVariableRead(keyVariable),
                    PrecomputedKey = keyVariable
                });
                return nodes;
            }

            return
            [
                new ExecutionCreateHashPayload(
                    context.Sides.Build.Variable,
                    payload.Shape,
                    payload.Values),
                hashAdd
            ];
        }

        return
        [
            new ExecutionCreateGeneratedRow(
                context.Sides.Build.Variable,
                fused.RowShape,
                fused.RowValues,
                fused.ContextValues,
                fused.ContextLayout),
            hashAdd
        ];
    }

    private static bool TryCreatePrePayloadHashKeyNodes(
        ExecutionHashAdd hashAdd,
        string payloadAlias,
        FusedCteHashBuildSource fused,
        List<ExecutionNode> nodes,
        out ExecutionVariable keyVariable)
    {
        keyVariable = null!;

        if (hashAdd.KeyType == typeof(object) ||
            !TryRewritePayloadKeyToProducerKey(hashAdd.Key, payloadAlias, fused.RowShape, fused.RowValues, out var producerKey))
        {
            return false;
        }

        if (producerKey is ExecutionValueTupleKey valueTupleKey)
        {
            return TryCreatePrePayloadValueTupleKeyNodes(valueTupleKey, hashAdd.KeyType, nodes, out keyVariable);
        }

        keyVariable = new ExecutionVariable("key", hashAdd.KeyType);
        nodes.Add(new ExecutionLet(keyVariable, producerKey));

        if (CanBeNullType(hashAdd.KeyType))
            nodes.Add(CreateContinueIfNull(new ExecutionVariableRead(keyVariable)));

        return true;
    }

    private static bool TryCreatePrePayloadValueTupleKeyNodes(
        ExecutionValueTupleKey valueTupleKey,
        Type keyType,
        List<ExecutionNode> nodes,
        out ExecutionVariable keyVariable)
    {
        keyVariable = new ExecutionVariable("key", keyType);
        var partVariables = new ExecutionVariable[valueTupleKey.Parts.Count];
        var nullablePartReads = new List<ExecutionExpression>();

        for (var index = 0; index < valueTupleKey.Parts.Count; index++)
        {
            var part = valueTupleKey.Parts[index];
            var partVariable = new ExecutionVariable($"key{index.ToString(System.Globalization.CultureInfo.InvariantCulture)}", part.ReturnType);
            partVariables[index] = partVariable;
            nodes.Add(new ExecutionLet(partVariable, part));

            if (CanBeNullType(part.ReturnType))
                nullablePartReads.Add(new ExecutionVariableRead(partVariable));
        }

        if (nullablePartReads.Count > 0)
            nodes.Add(CreateContinueIfAnyNull(nullablePartReads));

        nodes.Add(new ExecutionLet(
            keyVariable,
            new ExecutionValueTupleKey(
                partVariables.Select(static variable => new ExecutionVariableRead(variable)).ToArray(),
                keyType)));
        return true;
    }

    private static ExecutionContinueIf CreateContinueIfNull(ExecutionExpression expression)
    {
        return new ExecutionContinueIf(
            new ExecutionBinary(
                BinaryOpKind.Equal,
                expression,
                new ExecutionLiteral(null, expression.ReturnType),
                typeof(bool)));
    }

    private static ExecutionContinueIf CreateContinueIfAnyNull(IReadOnlyList<ExecutionExpression> expressions)
    {
        var condition = expressions
            .Select(static expression => new ExecutionBinary(
                BinaryOpKind.Equal,
                expression,
                new ExecutionLiteral(null, expression.ReturnType),
                typeof(bool)))
            .Aggregate<ExecutionExpression>(static (left, right) => new ExecutionBinary(
                BinaryOpKind.Or,
                left,
                right,
                typeof(bool)));

        return new ExecutionContinueIf(condition);
    }

    private static bool CanBeNullType(Type type)
    {
        return !type.IsValueType || Nullable.GetUnderlyingType(type) is not null;
    }
}
