using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.IR.Execution;

internal sealed partial class PhysicalLoweringImplementation
{
    private static Type ResolveAsOfComparisonKeyType(Type probeKeyType, Type candidateKeyType)
    {
        var keyType = ResolveCommonKeyType(probeKeyType, candidateKeyType);
        var probeUnderlying = Nullable.GetUnderlyingType(probeKeyType) ?? probeKeyType;
        var candidateUnderlying = Nullable.GetUnderlyingType(candidateKeyType) ?? candidateKeyType;
        if (probeUnderlying == candidateUnderlying && IsTypedAsOfComparisonKey(keyType))
            return keyType;
        throw new NotSupportedException("ASOF comparison keys require compatible concrete types.");
    }

    private static bool IsTypedAsOfComparisonKey(Type type)
    {
        return type != typeof(object) && type is not NullNode.NullType;
    }
}
