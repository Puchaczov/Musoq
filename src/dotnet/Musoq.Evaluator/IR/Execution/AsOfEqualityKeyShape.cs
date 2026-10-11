using System.Collections.Generic;
using System.Linq;

namespace Musoq.Evaluator.IR.Execution;

public sealed record AsOfEqualityKeyShape
{
    public AsOfEqualityKeyShape(ExecutionTypeRef keyType, IReadOnlyList<ExecutionTypeRef> partTypes)
    {
        KeyType = keyType;
        PartTypes = ExecutionIrCollections.Freeze(partTypes);
    }
    public ExecutionTypeRef KeyType { get; }
    public IReadOnlyList<ExecutionTypeRef> PartTypes { get; }

    internal static AsOfEqualityKeyShape Select(IReadOnlyList<ExecutionAsOfEqualityKey> keys)
    {
        var parts = keys.Select(key =>
        {
            var left = Nullable.GetUnderlyingType(key.Left.ReturnType.ResolveClrType()) ?? key.Left.ReturnType.ResolveClrType();
            var right = Nullable.GetUnderlyingType(key.Right.ReturnType.ResolveClrType()) ?? key.Right.ReturnType.ResolveClrType();
            if (left != right)
                throw new InvalidOperationException("ASOF equality operands require the same concrete binding.");
            return left;
        }).ToArray();
        var keyType = parts.Length == 0 ? typeof(bool) : parts.Length == 1 ? parts[0] :
            ValueTupleTypeShape.TryCreate(parts, out var tupleType) ? tupleType :
                throw new InvalidOperationException("ASOF equality keys require a concrete typed carrier.");
        return new(ExecutionClrBindingFactory.FromClr(keyType),
            ExecutionIrCollections.Freeze(parts.Select(ExecutionClrBindingFactory.FromClr).ToArray()));
    }
}
