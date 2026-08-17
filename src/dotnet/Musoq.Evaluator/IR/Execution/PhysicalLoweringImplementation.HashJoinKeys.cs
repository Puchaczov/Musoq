using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Physical.Nodes;
using Musoq.Parser.Nodes;
using AliasRefExtractor = Musoq.Evaluator.IR.Expressions.AliasRefExtractor;

namespace Musoq.Evaluator.IR.Execution;

internal sealed partial class PhysicalLoweringImplementation
{
    private static bool TryResolveHashJoinSides(
        PhysicalHashJoinNode join,
        JoinSources sources,
        [NotNullWhen(true)] out HashJoinSides? hashSides)
    {
        var buildUsage = HashJoinAliasResolver.GetUsage(join.BuildKeys, sources.Left.Shape, sources.Right.Shape);
        var probeUsage = HashJoinAliasResolver.GetUsage(join.ProbeKeys, sources.Left.Shape, sources.Right.Shape);

        if (buildUsage.CanBindLeft && probeUsage.CanBindRight)
        {
            hashSides = new HashJoinSides(sources.Left, sources.Right);
            return true;
        }

        if (buildUsage.CanBindRight && probeUsage.CanBindLeft)
        {
            hashSides = new HashJoinSides(sources.Right, sources.Left);
            return true;
        }

        hashSides = null;
        return false;
    }

    private static bool ReferencesAlias(IrExpression expression, string alias)
    {
        return AliasRefExtractor.Extract(expression).Any(candidate =>
            string.Equals(candidate, alias, StringComparison.OrdinalIgnoreCase));
    }

    private static Type ResolveHashJoinKeyType(PhysicalHashJoinNode join)
    {
        if (join.BuildKeys.Length == 1)
            return ResolveCommonKeyType(join.BuildKeys[0].ReturnType, join.ProbeKeys[0].ReturnType);

        if (TryResolveValueTupleHashJoinKeyTypes(join, out var keyTypes) &&
            ValueTupleTypeShape.TryCreate(keyTypes, out var tupleType))
            return tupleType;

        return typeof(object);
    }

    private static Type ResolveRangeJoinKeyType(PhysicalSortMergeJoinNode join)
    {
        return ResolveCommonKeyType(join.RightKey.ReturnType, join.LeftKey.ReturnType);
    }

    private static Type? ResolveRangeJoinPartitionKeyType(PhysicalSortMergeJoinNode join)
    {
        if (join.LeftPartitionKeys.Length == 0)
            return null;

        if (join.LeftPartitionKeys.Length == 1)
            return ResolveCommonKeyType(
                join.RightPartitionKeys[0].ReturnType,
                join.LeftPartitionKeys[0].ReturnType);

        var keyTypes = new Type[join.LeftPartitionKeys.Length];
        if (keyTypes.Length > 7)
            return typeof(object);

        for (var index = 0; index < keyTypes.Length; index++)
        {
            var keyType = ResolveCommonKeyType(
                join.RightPartitionKeys[index].ReturnType,
                join.LeftPartitionKeys[index].ReturnType);
            if (!CanUseTypedValueTupleHashJoinKeyPart(keyType))
                return typeof(object);
            keyTypes[index] = keyType;
        }

        if (!ValueTupleTypeShape.TryCreate(keyTypes, out var tupleType))
            return typeof(object);

        return join.LeftPartitionKeys.Concat(join.RightPartitionKeys).Any(static key =>
            !key.ReturnType.IsValueType || Nullable.GetUnderlyingType(key.ReturnType) != null)
            ? typeof(Nullable<>).MakeGenericType(tupleType)
            : tupleType;
    }

    private static bool TryResolveValueTupleHashJoinKeyTypes(
        PhysicalHashJoinNode join,
        out Type[] keyTypes)
    {
        keyTypes = [];

        if (join.BuildKeys.Length < 2)
            return false;

        var types = new Type[join.BuildKeys.Length];
        for (var index = 0; index < join.BuildKeys.Length; index++)
        {
            var buildType = join.BuildKeys[index].ReturnType;
            var probeType = join.ProbeKeys[index].ReturnType;
            var buildUnderlying = Nullable.GetUnderlyingType(buildType) ?? buildType;
            var probeUnderlying = Nullable.GetUnderlyingType(probeType) ?? probeType;
            if (buildUnderlying != probeUnderlying)
                return false;

            var keyType = ResolveCommonKeyType(buildType, probeType);
            if (!CanUseTypedValueTupleHashJoinKeyPart(keyType))
                return false;

            types[index] = keyType;
        }

        keyTypes = types;
        return true;
    }

    private static bool CanUseTypedValueTupleHashJoinKeyPart(Type type)
    {
        return type != typeof(object) &&
               type is not NullNode.NullType;
    }

    private static bool IsValueTupleHashJoinKeyType(Type keyType)
    {
        return ValueTupleTypeShape.IsValueTuple(keyType);
    }

    private static ExecutionExpression CreateHashJoinKeyExpression(
        IrExpression[] keys,
        IReadOnlyDictionary<string, RowShape> sourceLookup,
        Type keyType)
    {
        if (keys.Length == 1)
            return ExecutionExpressionConverter.Convert(keys[0], sourceLookup);

        if (IsValueTupleHashJoinKeyType(keyType))
        {
            return new ExecutionValueTupleKey(
                keys.Select(key => ExecutionExpressionConverter.Convert(key, sourceLookup)).ToArray(),
                keyType);
        }

        return new ExecutionMethodCall(
            CreateNullableHashJoinKeyMethod,
            keys.Select(key => ExecutionExpressionConverter.Convert(key, sourceLookup)).ToArray(),
            null,
            typeof(object));
    }

    private static bool ReferencesExecutionAlias(ExecutionExpression expression, string alias)
    {
        return expression switch
        {
            ExecutionFieldRead fieldRead => string.Equals(fieldRead.Alias, alias, StringComparison.OrdinalIgnoreCase),
            ExecutionBinary binary => ReferencesExecutionAlias(binary.Left, alias) ||
                                      ReferencesExecutionAlias(binary.Right, alias),
            ExecutionUnary unary => ReferencesExecutionAlias(unary.Operand, alias),
            ExecutionStrictCast strictCast => ReferencesExecutionAlias(strictCast.Expression, alias),
            ExecutionMethodCall method => method.Arguments.Any(argument => ReferencesExecutionAlias(argument, alias)) ||
                                          (method.InjectedSource != null &&
                                           ReferencesExecutionAlias(method.InjectedSource, alias)),
            ExecutionIsNullCheck isNull => ReferencesExecutionAlias(isNull.Expression, alias),
            ExecutionRowPresence rowPresence => string.Equals(rowPresence.Alias, alias, StringComparison.OrdinalIgnoreCase) ||
                                                ReferencesExecutionAlias(rowPresence.PresenceSource, alias),
            ExecutionInCheck inCheck => ReferencesExecutionAlias(inCheck.Expression, alias) ||
                                        inCheck.Values.Any(value => ReferencesExecutionAlias(value, alias)),
            ExecutionPatternMatch patternMatch => ReferencesExecutionAlias(patternMatch.Expression, alias) ||
                                                  ReferencesExecutionAlias(patternMatch.Pattern, alias),
            ExecutionBetween between => ReferencesExecutionAlias(between.Expression, alias) ||
                                        ReferencesExecutionAlias(between.Low, alias) ||
                                        ReferencesExecutionAlias(between.High, alias),
            ExecutionCaseWhen caseWhen => caseWhen.Branches.Any(branch =>
                                                 ReferencesExecutionAlias(branch.Condition, alias) ||
                                                 ReferencesExecutionAlias(branch.Result, alias)) ||
                                             (caseWhen.ElseExpression != null &&
                                              ReferencesExecutionAlias(caseWhen.ElseExpression, alias)),
            ExecutionCoalesce coalesce => coalesce.Expressions.Any(expression => ReferencesExecutionAlias(expression, alias)),
            ExecutionCompositeKey compositeKey => compositeKey.Parts.Any(part => ReferencesExecutionAlias(part, alias)),
            ExecutionValueTupleKey valueTupleKey => valueTupleKey.Parts.Any(part => ReferencesExecutionAlias(part, alias)),
            ExecutionAggregateCall aggregateCall => aggregateCall.Arguments.Any(argument => ReferencesExecutionAlias(argument, alias)),
            _ => false
        };
    }
}
