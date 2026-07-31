using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR;
using Musoq.Evaluator.IR.Logical.Nodes;
using Musoq.Evaluator.IR.Physical.Nodes;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.IR.Planning;

internal static partial class CteSidecarIndexPlanner
{
    private static bool TryCreateConsumerSpec(
        string cteName,
        OutputSchema cteOutputSchema,
        PhysicalHashJoinNode? hashJoin,
        PhysicalCteRefNode? cteRef,
        int indexSlot,
        out CteSidecarIndexSpec spec,
        out string reason)
    {
        spec = null!;

        if (hashJoin == null || cteRef == null)
        {
            reason = "Consumer is not an eligible direct hash-build CTE reference.";
            return false;
        }

        if (!TryResolveSimpleCteKeyColumns(hashJoin.BuildKeys, cteRef, cteOutputSchema, out var keyColumns, out reason))
            return false;

        var keyType = ResolveHashJoinKeyType(hashJoin);
        var kind = ResolveIndexKind(hashJoin, keyType);
        spec = new CteSidecarIndexSpec(cteName, kind, keyColumns, keyType, indexSlot);
        reason = string.Empty;
        return true;
    }

    private static CteSidecarIndexKind ResolveIndexKind(PhysicalHashJoinNode hashJoin, Type keyType)
    {
        return hashJoin is { Kind: JoinKind.LeftSemi or JoinKind.LeftAntiSemi, Residual: null } &&
               keyType != typeof(object)
            ? CteSidecarIndexKind.KeySet
            : CteSidecarIndexKind.Hash;
    }

    private static bool TryResolveSimpleCteKeyColumns(
        IReadOnlyList<IrExpression> keys,
        PhysicalCteRefNode cteRef,
        OutputSchema cteOutputSchema,
        out string[] keyColumns,
        out string reason)
    {
        var columns = new string[keys.Count];

        for (var index = 0; index < keys.Count; index++)
        {
            if (keys[index] is not ColumnRef column ||
                !string.Equals(column.Alias, cteRef.Alias, StringComparison.OrdinalIgnoreCase))
            {
                keyColumns = [];
                reason = "The hash-build key is not a simple CTE output column reference.";
                return false;
            }

            if (cteOutputSchema.FindByName(column.ColumnName) == null)
            {
                keyColumns = [];
                reason = $"The hash-build key column '{column.ColumnName}' is not present in the CTE output schema.";
                return false;
            }

            columns[index] = column.ColumnName;
        }

        keyColumns = columns;
        reason = string.Empty;
        return true;
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
            if (buildType != probeType || !CanUseTypedValueTupleHashJoinKeyPart(buildType))
                return false;

            types[index] = buildType;
        }

        keyTypes = types;
        return true;
    }

    private static bool CanUseTypedValueTupleHashJoinKeyPart(Type type)
    {
        return type != typeof(object) &&
               type is not NullNode.NullType;
    }

    private static Type ResolveCommonKeyType(Type buildType, Type probeType)
    {
        if (buildType == probeType)
            return buildType;

        var buildUnderlying = Nullable.GetUnderlyingType(buildType) ?? buildType;
        var probeUnderlying = Nullable.GetUnderlyingType(probeType) ?? probeType;

        if (buildUnderlying == probeUnderlying && buildUnderlying.IsValueType)
            return typeof(Nullable<>).MakeGenericType(buildUnderlying);

        return buildType;
    }

    private static PlanningDecision CreateSelectedDecision(
        CteSidecarIndexSpec spec,
        PhysicalHashJoinNode hashJoin,
        bool allocatedSlot)
    {
        var keyText = string.Join(", ", spec.KeyColumns);
        var slotText = spec.IndexSlot.ToString(CultureInfo.InvariantCulture);
        var allocationText = allocatedSlot ? "allocated" : "reused";

        return new PlanningDecision(
            PlanningDecisionCategory.CteSidecarIndexStrategy,
            "CteSidecarIndexStrategy",
            CreateConsumerTarget(spec.CteName, hashJoin),
            spec.Kind.ToString(),
            PlanningConfidence.High,
            $"Selected {spec.Kind} sidecar for CTE '{spec.CteName}' on key [{keyText}], slot {slotText} {allocationText}.");
    }

    private static PlanningDecision CreateSkippedDecision(string cteName, string target, string reason)
    {
        return new PlanningDecision(
            PlanningDecisionCategory.CteSidecarIndexStrategy,
            "CteSidecarIndexStrategy",
            target.StartsWith("cte:", StringComparison.Ordinal) ? target : $"cte:{cteName}:{target}",
            "Skipped",
            PlanningConfidence.Medium,
            reason);
    }

    private static string CreateConsumerTarget(string cteName, PhysicalHashJoinNode hashJoin)
    {
        return $"cte:{cteName}:hashJoin:{RuntimeHelpers.GetHashCode(hashJoin).ToString(CultureInfo.InvariantCulture)}";
    }
}
